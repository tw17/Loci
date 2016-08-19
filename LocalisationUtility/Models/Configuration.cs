using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Loci.Models
{
    public class Configuration
    {
        public string VisualStudioSolutionPath { get; set; }

        [XmlIgnore]
        public string LocalisationProjectPath { get; set; }

        public CultureInfo NeutralLanguage { get; set; }

        public List<CultureInfo> SupportedLanguages { get; set; }

        public List<string> KeyExcludePatterns { get; set; }

        public List<string> ValueExcludePatterns { get; set; }

        public Configuration()
        {           
            SupportedLanguages = new List<CultureInfo>();
            KeyExcludePatterns = new List<string>();
            ValueExcludePatterns = new List<string>();
        }

        public Regex KeyExcludeRegex
        {
            get
            {
                if (!KeyExcludePatterns.Any()) return null;

                var stringPattern = string.Join("|", KeyExcludePatterns);
                var finalPattern = $"^({stringPattern.Replace(@".", @"\.").Replace(@"*", @"\S*")})$";
                return new Regex(finalPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
        }

        public Regex ValueExcludeRegex
        {
            get
            {
                if (!ValueExcludePatterns.Any()) return null;

                var stringPattern = string.Join("|", ValueExcludePatterns);
                var finalPattern = $"^({stringPattern.Replace(@".", @"\.").Replace(@"*", @"\S*")})$";
                return new Regex(finalPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
        }
    }
}