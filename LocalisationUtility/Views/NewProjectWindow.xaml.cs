using System.Windows;
using LocalisationUtility.ViewModels;

namespace LocalisationUtility.Views
{
    /// <summary>
    /// Interaction logic for NewProjectWindow.xaml
    /// </summary>
    public partial class NewProjectWindow
    {
        private readonly NewProjectViewModel _viewModel;

        public NewProjectWindow(NewProjectViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
            InitializeComponent();
        }

        private void Wizard_OnFinish(object sender, RoutedEventArgs e)
        {
            _viewModel.FinishedCommand.Execute(null);
        }
    }
}
