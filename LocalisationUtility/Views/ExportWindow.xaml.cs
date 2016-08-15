using System.Windows;
using LocalisationUtility.Models;
using LocalisationUtility.ViewModels;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Core;

namespace LocalisationUtility.Views
{
    /// <summary>
    /// Interaction logic for ExportWindow.xaml
    /// </summary>
    public partial class ExportWindow : Window
    {
        private readonly ExportViewModel _viewModel;

        public ExportWindow(ExportViewModel viewModel)
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
            if (_viewModel.IsExporting)
                _viewModel.CancelExport();
        }

        private void Wizard_OnNext(object sender, CancelRoutedEventArgs e)
        {
            var wizard = sender as Wizard;
            if (wizard != null && wizard.CurrentPage.Name == LocationAndFormat.Name)
            {
                _viewModel.ExportCommand.Execute(null);
            }
        }
    }
}