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

        private BackgroundWorker mExportBackgroundWorker;
        private IMessenger _messenger;
        private Configuration mConfiguration;
        private BaseNode mRootNode;
        private CultureInfo mSelectedAvailableLanguage;
        private CultureInfo mSelectedLanguage;
        private string mSaveLocation;
        private ExcelFormat mExportExcelFormat;
        private double mExportProgress;
        private string mExportStatusString;
        private bool mIsExporting;
        private Regex mExcludePatterns;

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
            get { return mExportExcelFormat; }
            set
            {
                if (mExportExcelFormat != value)
                {
                    mExportExcelFormat = value;
                    OnPropertyChanged("ExportExcelFormat");
                }
            }
        }

        public bool IsExporting
        {
            get { return mIsExporting; }
            set
            {
                if (mIsExporting != value)
                {
                    mIsExporting = value;
                    OnPropertyChanged("IsExporting");
                }
            }
        }

        public double ExportProgress
        {
            get { return mExportProgress; }
            private set
            {
                if (mExportProgress != value)
                {
                    mExportProgress = value;
                    OnPropertyChanged("ExportProgress");
                }
            }
        }

        public string ExportStatusString
        {
            get { return mExportStatusString; }
            private set
            {
                if (mExportStatusString != value)
                {
                    mExportStatusString = value;
                    OnPropertyChanged("ExportStatusString");
                }
            }
        }

        public string SaveLocation
        {
            get { return mSaveLocation; }
            set
            {
                if (mSaveLocation != value)
                {
                    mSaveLocation = value;
                    OnPropertyChanged("SaveLocation");
                }
            }
        }

        #endregion

        #region Ctor

        public ExportViewModel(IMessenger messenger, Configuration configuration)
        {
            _messenger = messenger;
            mConfiguration = configuration;

            if (mConfiguration.ExcludePatterns.Count > 0)
            {
                var stringPattern = String.Join("|", mConfiguration.ExcludePatterns);
                var finalPattern = String.Format("^({0})$", stringPattern.Replace(@".", @"\.").Replace(@"*", @"\S*"));
                mExcludePatterns = new Regex(finalPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            else
            {
                mExcludePatterns = null;
            }

            AvailableLanguages = new ObservableCollection<CultureInfo>(mConfiguration.SupportedLanguages.ToArray());
            SelectedLanguages = new ObservableCollection<CultureInfo>();
            SelectedLanguages.CollectionChanged += (sender, args) => OnPropertyChanged("SelectedLanguages");
            SelectSaveLocationCommand = new RelayCommand(SelectSaveLocationCommandHandler);
            ExportCommand = new RelayCommand(ExportCommandHandler);

            mExportBackgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            mExportBackgroundWorker.DoWork += ExportBackgroundWorkerOnDoWork;
            mExportBackgroundWorker.RunWorkerCompleted += ExportBackgroundWorkerOnRunWorkerCompleted;
            mExportBackgroundWorker.ProgressChanged += ExportBackgroundWorkerOnProgressChanged;
        }

        private void ExportCommandHandler()
        {
            ExportToExcel();
        }

        #endregion

        #region Public Methods

        public void SetRootNode(BaseNode rootNode)
        {
            mRootNode = rootNode;
        }

        public void CancelExport()
        {
            mExportBackgroundWorker.CancelAsync();
        }

        #endregion

        #region Command Handlers

        private void SelectSaveLocationCommandHandler()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = mRootNode.Name,
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

        private void ExportBackgroundWorkerOnProgressChanged(object sender,
            ProgressChangedEventArgs progressChangedEventArgs)
        {
            ExportProgress = progressChangedEventArgs.ProgressPercentage;
            ExportStatusString = progressChangedEventArgs.UserState as string;
        }

        private void ExportBackgroundWorkerOnRunWorkerCompleted(object sender,
            RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
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
                var allResources = SolutionLoader.GetAllResourcesUnderNode(mRootNode).ToList();

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
                        //if (mExcludePatterns != null && mExcludePatterns.IsMatch(pNode.Name)) continue; //regex here


                        var resXnodeValue = SolutionLoader.GetResXNodeValue(pNode);
                        if (String.IsNullOrWhiteSpace(resXnodeValue.ToString())) continue;
                        if (mExcludePatterns != null && mExcludePatterns.IsMatch((string)resXnodeValue)) continue; //regex here
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
                            if (IsUntranslated((string) obj))
                            {
                                untranslated++;
                                allTranslated = false;
                            }
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

        private bool IsUntranslated(string value)
        {
            return String.IsNullOrWhiteSpace(value);
        }

        private void ExportToExcel()
        {
            IsExporting = true;
            mExportBackgroundWorker.RunWorkerAsync(ExportExcelFormat);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            mExportBackgroundWorker.DoWork -= ExportBackgroundWorkerOnDoWork;
            mExportBackgroundWorker.RunWorkerCompleted -= ExportBackgroundWorkerOnRunWorkerCompleted;
            mExportBackgroundWorker.ProgressChanged -= ExportBackgroundWorkerOnProgressChanged;
        }

        #endregion
    }
}