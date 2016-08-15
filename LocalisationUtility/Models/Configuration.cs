using System.Collections.Generic;
using System.Globalization;
using System.Xml.Serialization;

namespace LocalisationUtility.Models
{
    public class Configuration
    {
        public string VisualStudioSolutionPath { get; set; }

        [XmlIgnore]
        public string LocalisationProjectPath { get; set; }

        public CultureInfo NeutralLanguage { get; set; }

        public List<CultureInfo> SupportedLanguages { get; set; }

        public List<string> ExcludePatterns { get; set; }

        public Configuration()
        {           
            SupportedLanguages = new List<CultureInfo>();
            ExcludePatterns = new List<string>();
        }
    }
}