using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LocalisationUtility.Events
{
    public class NewProjectCreatedEventArgs
    {
        public string LocalisationProjectPath { get; private set; }

        public string VisualStudioSolutionPath { get; private set; }

        public CultureInfo NeutralLanguage { get; private set; }

        public List<CultureInfo> SupportedLanguages { get; private set; }

        public NewProjectCreatedEventArgs(string localisationProjectPath, string visualStudioSolutionPath, CultureInfo neutralLanguage, List<CultureInfo> supportedLanguages)
        {
            LocalisationProjectPath = localisationProjectPath;
            VisualStudioSolutionPath = visualStudioSolutionPath;
            NeutralLanguage = neutralLanguage;
            SupportedLanguages = supportedLanguages.ToList();
        }
    }
}
