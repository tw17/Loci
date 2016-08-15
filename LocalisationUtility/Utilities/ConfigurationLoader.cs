using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Loci.Interfaces;
using Loci.Models;

namespace Loci.Utilities
{
    public class ConfigurationLoader : IConfigurationLoader
    {
        //Cannot serialize CultureInfo so we will use an internal class for serialization
        public class SerializableConfiguration
        {
            public string VisualStudioSolutionPath { get; set; }

            public string NeutralLanguage { get; set; }

            public List<string> SupportedLanguages { get; set; }

            public List<string> ExcludePatterns { get; set; }
        }

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="configFile">The configuration file.</param>
        public void SaveConfiguration(Configuration configuration, string configFile)
        {
            var serializableConfiguration = new SerializableConfiguration
            {
                ExcludePatterns = configuration.ExcludePatterns.ToList(),
                NeutralLanguage = configuration.NeutralLanguage.Name,
                SupportedLanguages = configuration.SupportedLanguages.Select(l => l.Name).ToList(),
                VisualStudioSolutionPath = configuration.VisualStudioSolutionPath
            };

            var serializer = new XmlSerializer(typeof(SerializableConfiguration));
            using (TextWriter writer = new StreamWriter(configFile))
            {
                serializer.Serialize(writer, serializableConfiguration);
            }
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        /// <param name="configFile">The configuration file.</param>
        /// <returns></returns>
        public Configuration LoadConfiguration(string configFile)
        {
            var deserializer = new XmlSerializer(typeof(SerializableConfiguration));
            using (TextReader reader = new StreamReader(configFile))
            {
                var obj = (SerializableConfiguration)deserializer.Deserialize(reader);
              
                return new Configuration()
                {
                    LocalisationProjectPath = configFile,
                    VisualStudioSolutionPath = obj.VisualStudioSolutionPath,
                    NeutralLanguage = CultureInfo.GetCultureInfo(obj.NeutralLanguage),
                    SupportedLanguages = obj.SupportedLanguages.Select(CultureInfo.GetCultureInfo).ToList(),
                    ExcludePatterns = obj.ExcludePatterns.ToList()
                };
            }
        }
    }
}
