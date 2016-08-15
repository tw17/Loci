using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Loci.Events;

namespace Loci.ViewModels
{
    public class NewProjectViewModel : BaseViewModel
    {
        #region Private Fields
        private string _saveLocation;
        private string _solutionLocation;
        private readonly IMessenger _messenger;
        #endregion

        #region Public Properties
        public string SaveLocation
        {
            get { return _saveLocation; }
            set
            {
                if (_saveLocation == value) return;
                _saveLocation = value;
                OnPropertyChanged("SaveLocation");
            }
        }

        public string SolutionLocation
        {
            get { return _solutionLocation; }
            set
            {
                if (_solutionLocation == value) return;
                _solutionLocation = value;
                OnPropertyChanged("SolutionLocation");
            }
        }

        public CultureInfo NeutralLanguage { get; set; }

        public ObservableCollection<CultureInfo> AvailableLanguages { get; private set; }

        public ObservableCollection<CultureInfo> SelectedLanguages { get; private set; }

        #endregion

        #region Commands

        public ICommand SelectSaveLocationCommand { get; private set; }

        public ICommand SelectSolutionLocationCommand { get; private set; }

        public ICommand FinishedCommand { get; private set; }

        #endregion

        public NewProjectViewModel(IMessenger messenger)
        {
            _messenger = messenger;
            NeutralLanguage = CultureInfo.GetCultureInfo("en");
            AvailableLanguages = new ObservableCollection<CultureInfo>(CultureInfo.GetCultures(CultureTypes.AllCultures).Except(new[] { CultureInfo.InvariantCulture }));
            SelectedLanguages = new ObservableCollection<CultureInfo>();
            SelectSaveLocationCommand = new RelayCommand(SelectSaveLocationCommandHandler);
            SelectSolutionLocationCommand = new RelayCommand(SelectSolutionLocationCommandHandler);
            FinishedCommand = new RelayCommand(FinishedCommandHandler);
        }

        private void FinishedCommandHandler()
        {
            _messenger.Send(new NewProjectCreatedEventArgs(SaveLocation, SolutionLocation, NeutralLanguage, SelectedLanguages.ToList()));
        }

        private void SelectSolutionLocationCommandHandler()
        {
            // Create OpenFileDialog 
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".sln",
                Filter = "Visual Studio Sln Files (*.sln)|*.sln"
            };

            // Set filter for file extension and default file extension 

            // Display OpenFileDialog by calling ShowDialog method 
            var result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                SolutionLocation = dlg.FileName;
            }
        }

        private void SelectSaveLocationCommandHandler()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "LocalisationProject",
                DefaultExt = ".loci",
                Filter = "Localisation Project (.loci)|*.loci"
            };

            // Show save file dialog box
            var result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                SaveLocation = dlg.FileName;
            }
        }
    }
}
