using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;

namespace Loci.Controls
{
    public class CultureInfoToStringConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var typedValue = value as CultureInfo;
            if (typedValue != null)
            {
                return typedValue.EnglishName.Equals(CultureInfo.InvariantCulture.EnglishName) ? typedValue.EnglishName :
                    $"{typedValue.EnglishName} - {typedValue.Name}";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Interaction logic for LanguageSelector.xaml
    /// </summary>
    public partial class LanguageSelector : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        #region Private Fields

        private CultureInfo _selectedAvailableLanguage;
        private CultureInfo _selectedLanguage;

        #endregion

        #region Commands

        public ICommand AddCommand { get; private set; }
        public ICommand UpCommand { get; private set; }
        public ICommand DownCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }

        #endregion

        public ObservableCollection<CultureInfo> AvailableLanguages
        {
            get { return (ObservableCollection<CultureInfo>)GetValue(AvailableLanguagesProperty); }
            set { SetValue(AvailableLanguagesProperty, value); }
        }
        public static readonly DependencyProperty AvailableLanguagesProperty = DependencyProperty.Register(
          "AvailableLanguages", typeof(ObservableCollection<CultureInfo>), typeof(LanguageSelector), new UIPropertyMetadata(new ObservableCollection<CultureInfo>(), AvailableLanguagesPropertyChangedCallback));

        private static void AvailableLanguagesPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
        }

        public ObservableCollection<CultureInfo> SelectedLanguages
        {
            get { return (ObservableCollection<CultureInfo>)GetValue(SelectedLanguagesProperty); }
            set { SetValue(SelectedLanguagesProperty, value); }
        }
        public static readonly DependencyProperty SelectedLanguagesProperty = DependencyProperty.Register(
          "SelectedLanguages", typeof(ObservableCollection<CultureInfo>), typeof(LanguageSelector), new UIPropertyMetadata(new ObservableCollection<CultureInfo>(), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            //throw new NotImplementedException();
            var typedObject = dependencyPropertyChangedEventArgs.NewValue as ObservableCollection<CultureInfo>;
            if (typedObject == null) return;
            foreach (var cultureInfo in typedObject)
            {
                var languageSelector = dependencyObject as LanguageSelector;
                languageSelector?.AvailableLanguages.Remove(cultureInfo);
            }
        }

        public CultureInfo SelectedAvailableLanguage
        {
            get { return _selectedAvailableLanguage; }
            set
            {
                if (Equals(_selectedAvailableLanguage, value)) return;
                _selectedAvailableLanguage = value;
                OnPropertyChanged("SelectedAvailableLanguage");
            }
        }

        public CultureInfo SelectedLanguage
        {
            get { return _selectedLanguage; }
            set
            {
                if (Equals(_selectedLanguage, value)) return;
                _selectedLanguage = value;
                OnPropertyChanged("SelectedLanguage");
            }
        }

        public LanguageSelector()
        {
            DataContext = this;
            AddCommand = new RelayCommand(AddCommandHandler, AddCommandCanExecute);
            UpCommand = new RelayCommand(UpCommandHandler, UpCommandCanExecute);
            DownCommand = new RelayCommand(DownCommandHandler, DownCommandCanExecute);
            DeleteCommand = new RelayCommand(DeleteCommandHandler, DeleteCommandCanExecute);
            InitializeComponent();
        }

        private void AddCommandHandler()
        {
            SelectedLanguages.Add(SelectedAvailableLanguage);
            AvailableLanguages.Remove(SelectedAvailableLanguage);
            if (AvailableLanguages.Count > 0)
            {
                SelectedAvailableLanguage = AvailableLanguages.FirstOrDefault();
            }
            OnPropertyChanged("SelectedLanguages");
        }

        public void DownCommandHandler()
        {
            var index = SelectedLanguages.IndexOf(SelectedLanguage);
            SelectedLanguages.Move(index, index + 1);
        }

        public void UpCommandHandler()
        {
            var index = SelectedLanguages.IndexOf(SelectedLanguage);
            SelectedLanguages.Move(index, index - 1);
        }

        public void DeleteCommandHandler()
        {
            AvailableLanguages.Add(SelectedLanguage);
            var sorted = AvailableLanguages.OrderBy(s => s.Name).ToArray();
            AvailableLanguages.Clear();
            foreach (var cultureInfo in sorted)
            {
                AvailableLanguages.Add(cultureInfo);
            }
            SelectedLanguages.Remove(SelectedLanguage);
            OnPropertyChanged("SelectedLanguages");
        }

        private bool DeleteCommandCanExecute()
        {
            return SelectedLanguage != null;
        }

        private bool UpCommandCanExecute()
        {
            return SelectedLanguage != null && SelectedLanguages.IndexOf(SelectedLanguage) != 0;
        }

        private bool DownCommandCanExecute()
        {
            return SelectedLanguage != null &&
                   SelectedLanguages.IndexOf(SelectedLanguage) != SelectedLanguages.Count - 1;
        }

        private bool AddCommandCanExecute()
        {
            return SelectedAvailableLanguage != null && AvailableLanguages.Count > 0;
        }
    }
}
