using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using OfficeOpenXml;
using Loci.Models;

namespace Loci.ViewModels
{

    #region Enums

    public enum ExcelFormat
    {
        ExportOnlyUntranslatedResources,
        ExportAllResources,
    }

    #endregion

    public class ExportViewModel : BaseViewModel, IDisposable
    {
        #region Private Fields

        private readonly BackgroundWorker _exportBackgroundWorker;
        private IMessenger _messenger;
        private readonly Configuration _configuration;
        private BaseNode _rootNode;
        private string _saveLocation;
        private ExcelFormat _exportExcelFormat;
        private double _exportProgress;
        private string _exportStatusString;
        private bool _isExporting;
        private readonly Regex _excludePatterns;

        #endregion

        #region Commands

        public ICommand SelectSaveLocationCommand { get; private set; }

        public ICommand ExportCommand { get; private set; }

        #endregion

        #region Public Properties

        public ObservableCollection<CultureInfo> AvailableLanguages { get; private set; }

        public ObservableCollection<CultureInfo> SelectedLanguages { get; private set; }

        public ExcelFormat ExportExcelFormat
        {
            get { return _exportExcelFormat; }
            set
            {
                if (_exportExcelFormat == value) return;
                _exportExcelFormat = value;
                OnPropertyChanged("ExportExcelFormat");
            }
        }

        public bool IsExporting
        {
            get { return _isExporting; }
            set
            {
                if (_isExporting == value) return;
                _isExporting = value;
                OnPropertyChanged("IsExporting");
            }
        }

        public double ExportProgress
        {
            get { return _exportProgress; }
            private set
            {
                if (_exportProgress == value) return;
                _exportProgress = value;
                OnPropertyChanged("ExportProgress");
            }
        }

        public string ExportStatusString
        {
            get { return _exportStatusString; }
            private set
            {
                if (_exportStatusString == value) return;
                _exportStatusString = value;
                OnPropertyChanged("ExportStatusString");
            }
        }

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

        #endregion

        #region Ctor

        public ExportViewModel(IMessenger messenger, Configuration configuration)
        {
            _messenger = messenger;
            _configuration = configuration;

            if (_configuration.ExcludePatterns.Count > 0)
            {
                var stringPattern = string.Join("|", _configuration.ExcludePatterns);
                var finalPattern = $"^({stringPattern.Replace(@".", @"\.").Replace(@"*", @"\S*")})$";
                _excludePatterns = new Regex(finalPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            else
            {
                _excludePatterns = null;
            }

            AvailableLanguages = new ObservableCollection<CultureInfo>(_configuration.SupportedLanguages.ToArray());
            SelectedLanguages = new ObservableCollection<CultureInfo>();
            SelectedLanguages.CollectionChanged += (sender, args) => OnPropertyChanged("SelectedLanguages");
            SelectSaveLocationCommand = new RelayCommand(SelectSaveLocationCommandHandler);
            ExportCommand = new RelayCommand(ExportCommandHandler);

            _exportBackgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _exportBackgroundWorker.DoWork += ExportBackgroundWorkerOnDoWork;
            _exportBackgroundWorker.RunWorkerCompleted += ExportBackgroundWorkerOnRunWorkerCompleted;
            _exportBackgroundWorker.ProgressChanged += ExportBackgroundWorkerOnProgressChanged;
        }

        private void ExportCommandHandler()
        {
            ExportToExcel();
        }

        #endregion

        #region Public Methods

        public void SetRootNode(BaseNode rootNode)
        {
            _rootNode = rootNode;
        }

        public void CancelExport()
        {
            _exportBackgroundWorker.CancelAsync();
        }

        #endregion

        #region Command Handlers

        private void SelectSaveLocationCommandHandler()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = _rootNode.Name,
                DefaultExt = ".xlsx",
                Filter = "Excel document (.xlsx)|*.xlsx"
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

        #endregion

        #region Background worker

        private void ExportBackgroundWorkerOnProgressChanged(object sender, ProgressChangedEventArgs progressChangedEventArgs)
        {
            ExportProgress = progressChangedEventArgs.ProgressPercentage;
            ExportStatusString = progressChangedEventArgs.UserState as string;
        }

        private void ExportBackgroundWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            IsExporting = false;
        }

        private void ExportBackgroundWorkerOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            var exportFormat = (ExcelFormat)doWorkEventArgs.Argument;
            var worker = sender as BackgroundWorker;

            var newFile = new FileInfo(SaveLocation);
            if (newFile.Exists)
            {
                newFile.Delete(); // ensures we create a new workbook
                newFile = new FileInfo(SaveLocation);
            }
            using (var package = new ExcelPackage(newFile))
            {
                var worksheet = package.Workbook.Worksheets.Add(Path.GetFileNameWithoutExtension(SaveLocation));

                worksheet.Column(1).Width = 50;
                worksheet.Column(1).Hidden = true;
                worksheet.Cells[1, 1].Value = "Resource File";
                worksheet.Column(2).Width = 50;
                worksheet.Cells[1, 2].Value = "Key";
                worksheet.Column(3).Width = 50;
                worksheet.Cells[1, 3].Value = "Neutral";

                var headerRowCount = 4;
                foreach (var language in SelectedLanguages)
                {
                    worksheet.Cells[1, headerRowCount].Value = language.EnglishName;
                    worksheet.Column(headerRowCount).Width = 50;
                    headerRowCount++;
                }

                var rowCount = 2;
                var allResources = SolutionLoader.GetAllResourcesUnderNode(_rootNode).ToList();

                var total = allResources.Count();
                var resourceCount = 1;
                var resourceCount2 = 1;
                var untranslated = 0;
                foreach (var resource in allResources.TakeWhile(resource => !worker.CancellationPending))
                {
                    worker.ReportProgress((int) ((double) resourceCount/(double) total*100.0), resource.Location);
                    foreach (
                        var pNode in
                            SolutionLoader.GetResXNodes(resource.Location).TakeWhile(n => !worker.CancellationPending))
                    {
                        if (!SolutionLoader.IsResXNodeTranslatable(pNode)) continue;
                        if (pNode.Name.ToLowerInvariant().Contains("tooltip")) continue;
                        //if (_excludePatterns != null && _excludePatterns.IsMatch(pNode.Name)) continue; //regex here


                        var resXnodeValue = SolutionLoader.GetResXNodeValue(pNode);
                        if (string.IsNullOrWhiteSpace(resXnodeValue.ToString())) continue;
                        if (resXnodeValue.ToString().Contains("CustomizationFormText"))
                        {
                            var t = 0;
                        }
                        if (_excludePatterns != null && (_excludePatterns.IsMatch((string)resXnodeValue) || _excludePatterns.IsMatch(pNode.Name))) continue; //regex here
                        worksheet.Cells[rowCount, 1].Value = resource.Location;
                        worksheet.Cells[rowCount, 2].Value = pNode.Name;
                        worksheet.Cells[rowCount, 3].Value = resXnodeValue;

                        var allTranslated = true;
                        for (var column = 0; column < SelectedLanguages.Count; column++)
                        {
                            var cultureResXnodes = SolutionLoader.GetCultureResXNodes(resource.Location,
                                SelectedLanguages.ElementAt(column));
                            object obj = null;
                            var resXnode = SolutionLoader.FindResXNode(cultureResXnodes, pNode.Name);
                            if (resXnode != null)
                                obj = SolutionLoader.GetResXNodeValue(resXnode);
                            worksheet.Cells[rowCount, column + 4].Value = obj;
                            if (!IsUntranslated((string) obj)) continue;
                            untranslated++;
                            allTranslated = false;
                        }
                        rowCount++;
                        if (allTranslated && exportFormat == ExcelFormat.ExportOnlyUntranslatedResources)
                            rowCount--;
                        resourceCount2++;
                    }
                    resourceCount++;
                }

                package.Save();
            }
        }

        #endregion

        #region Private methods

        private static bool IsUntranslated(string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        private void ExportToExcel()
        {
            IsExporting = true;
            _exportBackgroundWorker.RunWorkerAsync(ExportExcelFormat);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _exportBackgroundWorker.DoWork -= ExportBackgroundWorkerOnDoWork;
            _exportBackgroundWorker.RunWorkerCompleted -= ExportBackgroundWorkerOnRunWorkerCompleted;
            _exportBackgroundWorker.ProgressChanged -= ExportBackgroundWorkerOnProgressChanged;
        }

        #endregion
    }
}