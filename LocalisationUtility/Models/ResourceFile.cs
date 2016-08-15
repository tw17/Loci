using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace LocalisationUtility.Models
{
    public class ResourceFile
    {
        public string FileName { get; }
        public List<TextResource> Resources { get; set; }

        private readonly string _location;

        public string FullPath => _location + FileName;

        public ResourceFile(string filename, string location)
        {
            _location = location;
            FileName = filename;
            Resources = new List<TextResource>();
        }

        public bool Contains(TextResource resource)
        {
            return Resources.Any(s => s.Name == resource.Name);
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceFile && ((ResourceFile) obj).FileName == FileName;
        }

        public override int GetHashCode()
        {
            return FileName.GetHashCode();
        }

        private void ReadResxFile()
        {
            //XDocument doc = XDocument.Load(Path.Combine(mLocation, FileName)); // Or whatever
            var doc = XDocument.Load(_location + FileName); // Or whatever
            //var allElements = doc.Descendants();
            var elements = doc.Descendants().Where(x => x.Name.LocalName == "data").ToList();
            foreach (
                var element in
                    elements.Where(element => !element.Attribute("name").Value.ToLowerInvariant().EndsWith("size") &&
                                              !element.Attribute("name").Value.ToLowerInvariant().EndsWith("location") &&
                                              !element.Attribute("name").Value.ToLowerInvariant().EndsWith("color") &&
                                              !element.Attribute("name").Value.ToLowerInvariant().EndsWith("enabled") &&
                                              element.Attribute("name").Value.ToLowerInvariant().Contains("text")))
            {
                Resources.Add(new TextResource()
                {
                    Name = element.Attribute("name").Value,
                    NutrualValue = element.Value.Trim()
                });
            }
        }
    }
}