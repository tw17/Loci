using System;
using System.Reflection;
using System.Windows;

namespace Loci.Views
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow
    {
        public AboutWindow()
        {
            InitializeComponent();
            var fullVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var shortVersion = fullVersion.Substring(0, fullVersion.LastIndexOf(".", StringComparison.Ordinal));
            VersionTextBlock.Text = shortVersion;
            //var contributors = Properties.Resources.contributors.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);            
        }
    }
}
