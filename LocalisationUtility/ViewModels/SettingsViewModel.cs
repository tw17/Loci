using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Loci.Events;
using Loci.Models;

namespace Loci.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        #region Private Fields

        private readonly Configuration _configuration;
        private readonly IMessenger _messenger;
        private string _keyExcludePatternsString;
        private string _valueExcludePatternsString;

        #endregion

        #region Commands

        public ICommand OkCommand { get; private set; }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the key exclude patterns string.
        /// </summary>
        /// <value>
        /// The key exclude patterns string.
        /// </value>
        public string KeyExcludePatternsString
        {
            get { return _keyExcludePatternsString; }
            set
            {
                if (_keyExcludePatternsString == value) return;
                _keyExcludePatternsString = value;
                OnPropertyChanged("KeyExcludePatternsString");
            }
        }

        /// <summary>
        /// Gets or sets the value exclude patterns string.
        /// </summary>
        /// <value>
        /// The value exclude patterns string.
        /// </value>
        public string ValueExcludePatternsString
        {
            get { return _valueExcludePatternsString; }
            set
            {
                if (_valueExcludePatternsString == value) return;
                _valueExcludePatternsString = value;
                OnPropertyChanged("ValueExcludePatternsString");
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
            KeyExcludePatternsString = string.Join("\r\n", _configuration.KeyExcludePatterns);
            ValueExcludePatternsString = string.Join("\r\n", _configuration.ValueExcludePatterns);
            OkCommand = new RelayCommand<Window>(OKCommandHandler);
        }

        private void OKCommandHandler(Window window)
        {
            _configuration.KeyExcludePatterns.Clear();
            _configuration.ValueExcludePatterns.Clear();
            if (!string.IsNullOrWhiteSpace(KeyExcludePatternsString))
            {
                _configuration.KeyExcludePatterns.AddRange(KeyExcludePatternsString.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));   
            }
            if (!string.IsNullOrWhiteSpace(ValueExcludePatternsString))
            {
                _configuration.ValueExcludePatterns.AddRange(ValueExcludePatternsString.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
            }
            _configuration.NeutralLanguage = NeutralLanguage;
            _configuration.SupportedLanguages.Clear();
            _configuration.SupportedLanguages.AddRange(SelectedLanguages.ToList());
            _messenger.Send<SettingsChangedEvent>(null);
            window.Close();
        }
    }
}
