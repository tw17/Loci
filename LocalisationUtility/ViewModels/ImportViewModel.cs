using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using LocalisationUtility.Models;

using OfficeOpenXml;

namespace LocalisationUtility.ViewModels
{
    public class ImportViewModel : BaseViewModel
    {
        #region Private Fields

        private BaseNode mRootNode;
        private BackgroundWorker mImportBackgroundWorker;
        private string mExcelFileLocation;
        private bool mIsImporting;
        private double mImportProgress;
        private string mImportStatusString;

        #endregion

        #region Commands

        public ICommand SelectFileLocationCommand { get; private set; }

        public ICommand ImportCommand { get; private set; }

        #endregion

        #region Public Properties

        public bool IsImporting
        {
            get { return mIsImporting; }
            set
            {
                if (mIsImporting != value)
                {
                    mIsImporting = value;
                    OnPropertyChanged("IsImporting");
                }
            }
        }

        public double ImportProgress
        {
            get { return mImportProgress; }
            private set
            {
                if (mImportProgress != value)
                {
                    mImportProgress = value;
                    OnPropertyChanged("ImportProgress");
                }
            }
        }

        public string ImportStatusString
        {
            get { return mImportStatusString; }
            private set
            {
                if (mImportStatusString != value)
                {
                    mImportStatusString = value;
                    OnPropertyChanged("ImportStatusString");
                }
            }
        }

        /// <summary>
        /// Gets or sets the excel file location.
        /// </summary>
        /// <value>
        /// The excel file location.
        /// </value>
        public string ExcelFileLocation
        {
            get { return mExcelFileLocation; }
            set
            {
                if (mExcelFileLocation != value)
                {
                    mExcelFileLocation = value;
                    OnPropertyChanged("ExcelFileLocation");
                }
            }
        }

        #endregion

        #region Public Methods

        public void CancelImport()
        {
            mImportBackgroundWorker.CancelAsync();
        }

        public void SetRootNode(BaseNode rootNode)
        {
            mRootNode = rootNode;
        }

        #endregion

        #region Ctor

        public ImportViewModel()
        {
            SelectFileLocationCommand = new RelayCommand(SelectFileLocationCommandHandler);
            ImportCommand = new RelayCommand(ImportCommandHandler);

            mImportBackgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            mImportBackgroundWorker.DoWork += ImportBackgroundWorkerOnDoWork;
            mImportBackgroundWorker.RunWorkerCompleted += ImportBackgroundWorkerOnRunWorkerCompleted;
            mImportBackgroundWorker.ProgressChanged += ImportBackgroundWorkerOnProgressChanged;
        }

        #endregion

        #region Command Handlers

        private void ImportCommandHandler()
        {
            ImportExcel();
        }

        private void SelectFileLocationCommandHandler()
        {
            // Create OpenFileDialog 
            var dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".xlsx";
            dlg.Filter = "Excel document (.xlsx)|*.xlsx";

            // Display OpenFileDialog by calling ShowDialog method 
            var result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                ExcelFileLocation = dlg.FileName;
            }
        }

        #endregion

        #region Private Methods

        private void ImportExcel()
        {
            IsImporting = true;
            mImportBackgroundWorker.RunWorkerAsync();
        }

        #endregion

        #region Background Worker
        private void ImportBackgroundWorkerOnProgressChanged(object sender, ProgressChangedEventArgs progressChangedEventArgs)
        {
            ImportProgress = progressChangedEventArgs.ProgressPercentage;
            ImportStatusString = progressChangedEventArgs.UserState as string;
        }

        private void ImportBackgroundWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            IsImporting = false;
        }

        private void ImportBackgroundWorkerOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            List<string> errorFiles = new List<string>();
            var newFile = new FileInfo(ExcelFileLocation);
            int notexist = 0;
            using (var package = new ExcelPackage(newFile))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                var t = worksheet.Dimension;

                for (var column = 4; column <= worksheet.Dimension.Columns; column++)
                {
                    Dictionary<string, List<Tuple<string, string>>> resources = new Dictionary<string, List<Tuple<string, string>>>();

                    var stringLanguage = worksheet.Cells[1, column].Value.ToString();
                    var cultureInfo = CultureInfo.GetCultureInfo(stringLanguage);
                    for (var row = 2; row < worksheet.Dimension.Rows; row++)
                    {
                        if(worksheet.Cells[row, column].Value == null)
                            continue;
                        //var filename = SolutionLoader.GetCultureResXFileName(worksheet.Cells[row, 1].Value.ToString(), cultureInfo);
                        var filename = worksheet.Cells[row, 1].Value.ToString();
                        if (resources.ContainsKey(filename))
                        {
                            resources[filename].Add(new Tuple<string, string>(worksheet.Cells[row, 2].Value.ToString(), worksheet.Cells[row, column].Value.ToString()));
                        }
                        else
                        {
                            resources.Add(filename, new List<Tuple<string, string>>() { new Tuple<string, string>(worksheet.Cells[row, 2].Value.ToString(), worksheet.Cells[row, column].Value.ToString()) });
                        }
                        
                        //ImportResource(worksheet.Cells[row, 1].Value.ToString(), worksheet.Cells[row, 2].Value.ToString(), worksheet.Cells[row, column].Value, cultureInfo);
                    }
                    foreach (var keyValuePair in resources)
                    {
                        SaveResources(keyValuePair.Key, keyValuePair.Value, cultureInfo);
                    }
                }
            }
            
        }

        private void SaveResourcesold(string resourceFileLocation, List<Tuple<string, string>> newValues)
        {
            var existing = SolutionLoader.GetResXNodes(resourceFileLocation);
            foreach (var newValue in newValues)
            {
                var res = existing.FirstOrDefault(r => r.Name == newValue.Item1);
                if (res != null)
                {
                }
            }
        }

        public static void SaveResources(string path, List<Tuple<string, string>> newValues, CultureInfo culture)
        {
            string cultureResXfileName = SolutionLoader.GetCultureResXFileName(path, culture);
            List<ResXDataNode> resXnodes1 = SolutionLoader.GetResXNodes(path);
            List<ResXDataNode> resXnodes2 = SolutionLoader.GetResXNodes(cultureResXfileName);
            using (ResXResourceWriter resXresourceWriter = new ResXResourceWriter(cultureResXfileName))
            {
                foreach (ResXDataNode resXdataNode in resXnodes2)
                {
                    if (!SolutionLoader.IsResXNodeTranslatable(resXdataNode) && SolutionLoader.FindResXNode(resXnodes1, resXdataNode.Name) != null)
                        resXresourceWriter.AddResource(resXdataNode);
                }

                foreach (ResXDataNode resXdataNode in resXnodes2)
                {
                    if (SolutionLoader.IsResXNodeTranslatable(resXdataNode))
                    {
                        var newValue = newValues.FirstOrDefault(p => p.Item1 == resXdataNode.Name);
                        if (newValue != null)
                        {
                            resXresourceWriter.AddResource(new ResXDataNode(newValue.Item1, newValue.Item2)); 
                        }
                        else
                        {
                            resXresourceWriter.AddResource(resXdataNode);      
                        }
                        newValues.Remove(newValue);
                    }
                }
                foreach (var newValue in newValues)
                {
                    resXresourceWriter.AddResource(new ResXDataNode(newValue.Item1, newValue.Item2)); 
                }

                //foreach (var pair in newValues)
                //{
                //    var existing = resXnodes2.FirstOrDefault(i => i.Name == pair.First);
                //    if (existing != null)
                //    {
                //        existing
                //    }
                //    else
                //    {
                        
                //    }
                //    resXresourceWriter.AddResource(new ResXDataNode(pair.First, (object)pair.Second));
                //}
            }
        }

        //Hashtable resourceEntries = new Hashtable();

            //if (File.Exists(path))
            //{
            //    //Get existing resources
            //    ResXResourceReader reader = new ResXResourceReader(path);
            //    if (reader != null)
            //    {
            //        IDictionaryEnumerator id = reader.GetEnumerator();
            //        foreach (DictionaryEntry d in reader)
            //        {
            //            if (d.Value == null)
            //                resourceEntries.Add(d.Key.ToString(), "");
            //            else
            //                resourceEntries.Add(d.Key.ToString(), d.Value.ToString());
            //        }
            //        reader.Close();
            //    }
            //}

            ////Modify resources here...
            //foreach (var pair in newValues)
            //{
            //    if (!resourceEntries.ContainsKey(pair.First))
            //    {
            //        resourceEntries.Add(pair.First, pair.Second);
            //    }
            //    else
            //    {
            //        if (!resourceEntries[pair.First].ToString().ToLowerInvariant().Equals(pair.Second.ToLowerInvariant()))
            //            resourceEntries[pair.First] = pair.Second;
            //    }
            //}

            ////Write the combined resource file
            //ResXResourceWriter resourceWriter = new ResXResourceWriter(path);

            //foreach (String key in resourceEntries.Keys)
            //{
            //    resourceWriter.AddResource(key, resourceEntries[key]);
            //}
            //resourceWriter.Generate();
            //resourceWriter.Close();

        #endregion

    }
}
