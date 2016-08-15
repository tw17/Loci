using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Xml.Linq;
using LocalisationUtility.Enums;
using LocalisationUtility.Models;

namespace LocalisationUtility
{
    /// <summary>
    /// Class for loading .sln solution files
    /// </summary>
    public static class SolutionLoader
    {
        //internal class SolutionParser
        //Name: Microsoft.Build.Construction.SolutionParser
        //Assembly: Microsoft.Build, Version=4.0.0.0

        #region Read only fields

        private static readonly Type s_SolutionParser;
        private static readonly PropertyInfo s_SolutionParser_solutionReader;
        private static readonly MethodInfo s_SolutionParser_parseSolution;
        private static readonly PropertyInfo s_SolutionParser_projects;

        private static readonly Type s_ProjectInSolution;
        private static readonly PropertyInfo s_ProjectInSolution_ProjectName;
        private static readonly PropertyInfo s_ProjectInSolution_RelativePath;
        private static readonly PropertyInfo s_ProjectInSolution_ProjectType;
        private static readonly PropertyInfo s_ProjectInSolution_ProjectGuid;

        #endregion

        /// <summary>
        /// Initializes the <see cref="SolutionLoader"/> class.
        /// </summary>
        static SolutionLoader()
        {
            s_SolutionParser =
                Type.GetType(
                    "Microsoft.Build.Construction.SolutionParser, Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                    false, false);
            if (s_SolutionParser != null)
            {
                s_SolutionParser_solutionReader = s_SolutionParser.GetProperty("SolutionReader",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                s_SolutionParser_projects = s_SolutionParser.GetProperty("Projects",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                s_SolutionParser_parseSolution = s_SolutionParser.GetMethod("ParseSolution",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }

            s_ProjectInSolution =
                Type.GetType(
                    "Microsoft.Build.Construction.ProjectInSolution, Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                    false, false);
            if (s_ProjectInSolution == null) return;
            s_ProjectInSolution_ProjectName = s_ProjectInSolution.GetProperty("ProjectName",
                BindingFlags.NonPublic | BindingFlags.Instance);
            s_ProjectInSolution_RelativePath = s_ProjectInSolution.GetProperty("RelativePath",
                BindingFlags.NonPublic | BindingFlags.Instance);
            s_ProjectInSolution_ProjectType = s_ProjectInSolution.GetProperty("ProjectType",
                BindingFlags.NonPublic | BindingFlags.Instance);
            s_ProjectInSolution_ProjectGuid = s_ProjectInSolution.GetProperty("ProjectGuid",
                BindingFlags.NonPublic | BindingFlags.Instance);
        }

        /// <summary>
        /// Loads the solution.
        /// </summary>
        /// <param name="solutionFileName">Name of the solution file.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Can not find type 'Microsoft.Build.Construction.SolutionParser' are you missing a assembly reference to 'Microsoft.Build.dll'?</exception>
        public static Solution LoadSolution(string solutionFileName)
        {
            if (s_SolutionParser == null)
            {
                throw new InvalidOperationException(
                    "Can not find type 'Microsoft.Build.Construction.SolutionParser' are you missing a assembly reference to 'Microsoft.Build.dll'?");
            }
            var solutionParser =
                s_SolutionParser.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).First().Invoke(null);
            using (var streamReader = new StreamReader(solutionFileName))
            {
                s_SolutionParser_solutionReader.SetValue(solutionParser, streamReader, null);
                s_SolutionParser_parseSolution.Invoke(solutionParser, null);
            }

            var solution = new Solution {Name = Path.GetFileNameWithoutExtension(solutionFileName)};

            var array = (Array) s_SolutionParser_projects.GetValue(solutionParser, null);
            for (var i = 0; i < array.Length; i++)
            {
                var projectType = s_ProjectInSolution_ProjectType.GetValue(array.GetValue(i), null);
                var underlyingValue =
                    Convert.ChangeType(projectType, Enum.GetUnderlyingType(projectType.GetType())) as int?;
                if (underlyingValue == 2) continue;

                var projectName = s_ProjectInSolution_ProjectName.GetValue(array.GetValue(i), null) as string;
                var relativePath = s_ProjectInSolution_RelativePath.GetValue(array.GetValue(i), null) as string;
                var projectGuid = s_ProjectInSolution_ProjectGuid.GetValue(array.GetValue(i), null) as string;

                solution.AddProject(GetProject(projectName, projectGuid, relativePath,
                    Path.Combine(Path.GetDirectoryName(solutionFileName), relativePath)));
            }
            return solution;
        }


        public static IEnumerable<Resource> GetAllResourcesUnderNode(BaseNode node)
        {
            var resources = node.Children.Where(c => c.NodeType == TreeNodeType.Resource).Cast<Resource>().ToList();

            foreach (var child in node.Children.Where(c => c.Children.Any()))
            {
                resources.AddRange(GetAllResourcesUnderNode(child));
            }

            return resources;
        }

        private static List<TextResource> LoadTextResources(string file, List<string> excludePatterns)
        {
            var resources = new List<TextResource>();
            var doc = XDocument.Load(file);

            var elements = doc.Descendants().Where(x => x.Name.LocalName == "data").ToList();
            foreach (var element in elements)
            {
                if (element.Attribute("name").Value.ToLowerInvariant().EndsWith("size") ||
                    element.Attribute("name").Value.ToLowerInvariant().EndsWith("location") ||
                    element.Attribute("name").Value.ToLowerInvariant().EndsWith("color") ||
                    element.Attribute("name").Value.ToLowerInvariant().EndsWith("enabled") ||
                    element.Attribute("name").Value.ToLowerInvariant().StartsWith(">>") ||
                    !element.Attribute("name").Value.ToLowerInvariant().Contains("text")) continue;
                var value = element.Descendants().FirstOrDefault(d => d.Name.LocalName == "value");
                if (value != null)
                {
                    resources.Add(new TextResource()
                    {
                        Name = element.Attribute("name").Value,
                        NutrualValue = value.Value
                    });
                }
            }

            return resources;
        }

        /// <summary>
        /// Gets the project.
        /// </summary>
        /// <param name="projectName">Name of the project.</param>
        /// <param name="projectGuid">The project unique identifier.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <param name="absolutePath">The absolute path.</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        private static Project GetProject(string projectName, string projectGuid, string relativePath,
            string absolutePath)
        {
            if (!File.Exists(absolutePath))
                throw new FileNotFoundException($"{projectName} could not be found. {absolutePath}");

            var project = new Project()
            {
                Name = projectName,
                Guid = projectGuid,
                AbsolutePath = absolutePath,
                RelativePath = relativePath
            };

            var doc = XDocument.Load(absolutePath); // Or whatever
            //var allElements = doc.Descendants();
            var elements =
                doc.Descendants()
                    .Where(
                        x =>
                            x.Name.LocalName == "EmbeddedResource" && x.Attribute("Include") != null &&
                            x.Attribute("Include").Value.EndsWith(".resx"))
                    .ToList();

            //Get the nuturals
            foreach (var element in elements)
            {
                var includeAttributeValue = element.Attribute("Include").Value;

                var resourceName =
                    Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(includeAttributeValue));
                var resourceLocation = Path.Combine(Path.GetDirectoryName(absolutePath), includeAttributeValue);
                if (IsCultureFile(resourceLocation)) continue;
                var resource = new Resource(resourceName, resourceLocation);
                var folders =
                    includeAttributeValue.Split('\\').Where(s => !s.ToLowerInvariant().EndsWith(".resx")).ToArray();
                project.AddResource(folders, resource);
            }

            return project;
        }

        public static bool IsCultureFile(string strFileName)
        {
            var strArray = Path.GetFileName(strFileName).Split('.');
            if (strArray.Length <= 2) return false;
            var name = strArray[strArray.Length - 2];
            try
            {
                CultureInfo.GetCultureInfo(name);
                return true;
            }
            catch (ArgumentException ex)
            {
            }
            return false;
        }

        public static List<ResXDataNode> GetResXNodes(string strFileName)
        {
            var list = new List<ResXDataNode>();
            if (!File.Exists(strFileName)) return list;
            try
            {
                using (var resXresourceReader = new ResXResourceReader(strFileName))
                {
                    resXresourceReader.UseResXDataNodes = true;
                    list.AddRange(from DictionaryEntry dictionaryEntry in resXresourceReader select (ResXDataNode) dictionaryEntry.Value);
                }
            }
            catch (FileNotFoundException ex)
            {
            }
            return list;
        }

        public static void WriteResourceNodes(string strFileName, List<ResXDataNode> resources)
        {
            using (var resXResourceWriter = new ResXResourceWriter(strFileName))
            {
                foreach (var resource in resources)
                {
                    resXResourceWriter.AddResource(resource);
                }
                resXResourceWriter.Generate();
                resXResourceWriter.Close();
            }
        }

        public static string GetCultureResXFileName(string strNeutralFileName, CultureInfo pCulture)
        {
            return pCulture == null
                ? strNeutralFileName
                : Path.Combine(Path.GetDirectoryName(strNeutralFileName),
                    string.Concat(Path.GetFileNameWithoutExtension(strNeutralFileName), '.', pCulture.ToString(),
                        Path.GetExtension(strNeutralFileName)));
        }

        public static List<ResXDataNode> GetCultureResXNodes(string strNeutralFileName, CultureInfo pCulture)
        {
            return GetResXNodes(GetCultureResXFileName(strNeutralFileName, pCulture));
        }

        public static object GetResXNodeValue(ResXDataNode pNode)
        {
            if (pNode.FileRef != null)
                return null;

            AssemblyName[] names = {new AssemblyName(typeof (string).Assembly.FullName)};
            try
            {
                return pNode.GetValue(names);
            }
            catch
            {
                return null;
            }
        }

        public static bool IsResXNodeTranslatable(ResXDataNode pNode)
        {
            var resXnodeValue = GetResXNodeValue(pNode);
            return resXnodeValue is string && !pNode.Name.StartsWith(">>");
        }

        public static ResXDataNode FindResXNode(List<ResXDataNode> pNodes, string strKey)
        {
            return pNodes.FirstOrDefault(node => node.Name == strKey);
        }
    }
}