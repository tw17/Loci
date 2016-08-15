using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using LocalisationUtility.Events;
using LocalisationUtility.Models;

namespace LocalisationUtility.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        #region Private Fields

        private readonly Configuration _configuration;
        private readonly IMessenger _messenger;
        private string _excludePatternsString;

        #endregion

        #region Commands

        public ICommand OKCommand { get; private set; }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the exclude patterns string.
        /// </summary>
        /// <value>
        /// The exclude patterns string.
        /// </value>
        public string ExcludePatternsString
        {
            get { return _excludePatternsString; }
            set
            {
                if (_excludePatternsString != value)
                {
                    _excludePatternsString = value;
                    OnPropertyChanged("ExcludePatternsString");
                }
            }
        }

        public ObservableCollection<CultureInfo> SelectedLanguages { get; private set; }
        public ObservableCollection<CultureInfo> AvailableLanguages { get; private set; }
        public CultureInfo NeutralLanguage { get; set; }

        #endregion

        public SettingsViewModel(IMessenger messenger, Configuration configuration)
        {
            _configuration = configuration;
            _messenger = messenger;
            NeutralLanguage = _configuration.NeutralLanguage;
            AvailableLanguages = new ObservableCollection<CultureInfo>(CultureInfo.GetCultures(CultureTypes.AllCultures).Except(new [] {CultureInfo.InvariantCulture}));
            SelectedLanguages = new ObservableCollection<CultureInfo>(_configuration.SupportedLanguages.ToArray());
            ExcludePatternsString = String.Join("\r\n", _configuration.ExcludePatterns);
            OKCommand = new RelayCommand<Window>(OKCommandHandler);
        }

        private void OKCommandHandler(Window window)
        {
            _configuration.ExcludePatterns.Clear();
            if (!String.IsNullOrWhiteSpace(ExcludePatternsString))
            {
                _configuration.ExcludePatterns.AddRange(ExcludePatternsString.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));   
            }
            _configuration.NeutralLanguage = NeutralLanguage;
            _configuration.SupportedLanguages.Clear();
            _configuration.SupportedLanguages.AddRange(SelectedLanguages.ToList());
            _messenger.Send<SettingsChangedEvent>(null);
            window.Close();
        }
    }
}
