using System.Windows;
using LocalisationUtility.Models;
using LocalisationUtility.ViewModels;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Core;

namespace LocalisationUtility.Views
{
    /// <summary>
    /// Interaction logic for ImportWindow.xaml
    /// </summary>
    public partial class ImportWindow
    {
        private readonly ImportViewModel _viewModel;

        public ImportWindow(ImportViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
            InitializeComponent();
        }

        public void Initialize(BaseNode rootNode)
        {
            _viewModel.SetRootNode(rootNode);
        }

        private void Wizard_OnCancel(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsImporting)
                _viewModel.CancelImport();
        }

        private void Wizard_OnNext(object sender, CancelRoutedEventArgs e)
        {
            var wizard = sender as Wizard;
            if (wizard != null && wizard.CurrentPage.Name == ExcelLocation.Name)
            {
                _viewModel.ImportCommand.Execute(null);
            }
        }
    }
}
