using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using Autofac;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Loci.Enums;
using Loci.Events;
using Loci.Interfaces;
using Loci.Models;
using Loci.Views;

namespace Loci.ViewModels
{
    public class MainViewModel : BaseViewModel, IDisposable
    {
        #region Private Fields
        private Configuration mConfiguration;
        private IConfigurationLoader mConfigurationLoader;
        private IMessenger _messenger;
        private ReadOnlyCollection<TreeNodeViewModel> mFirstGeneration;
        private TreeNodeViewModel mRootNode;
        private UserControl mSelectedNodeDetailsView;
        private DataTable mDataTable = new DataTable();
        private BaseNode mSelectedNode;
        private Regex mExcludePatterns;
        private bool mIsLoaded;

        public DataView DefaultView
        {
            get { return mDataTable.AsDataView(); }
        }

        #endregion

        #region Commands

        public ICommand NewProjectCommand { get; private set; }
        public ICommand LoadProjectCommand { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public ICommand ExportCommand { get; private set; }
        public ICommand ImportCommand { get; private set; }
        public ICommand DiffCommand { get; private set; }
        public ICommand PreFillCommand { get; private set; }
        public ICommand AboutCommand { get; private set; }

        #endregion

        #region Public Properties

        public UserControl SelectedNodeDetailsView
        {
            get { return mSelectedNodeDetailsView; }
            private set
            {
                if (mSelectedNodeDetailsView != value)
                {
                    mSelectedNodeDetailsView = value;
                    OnPropertyChanged("SelectedNodeDetailsView");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a solution is loaded.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is loaded; otherwise, <c>false</c>.
        /// </value>
        public bool IsLoaded
        {
            get { return mIsLoaded; }
            set
            {
                if (value != mIsLoaded)
                {
                    mIsLoaded = value;
                    OnPropertyChanged("IsLoaded");
                }
            }
        }

        public bool PendingChanges
        {
            get { return mDataTable.GetChanges() != null; }
        }

        #endregion

        public MainViewModel(IMessenger messenger, IConfigurationLoader configurationLoader, Configuration configuration)
        {
            _messenger = messenger;
            mConfigurationLoader = configurationLoader;
            _messenger.Register<SettingsChangedEvent>(this, SettingsChangedEventHandler);
            _messenger.Register<NewProjectCreatedEventArgs>(this, NewProjectCreatedEventHandler);
            mConfiguration = configuration;

            NewProjectCommand = new RelayCommand(NewProjectCommandHandler);
            LoadProjectCommand = new RelayCommand(LoadProjectCommandHandler);
            ExportCommand = new RelayCommand(ExportCommandHandler);
            ImportCommand = new RelayCommand(ImportCommandHandler);
            AboutCommand = new RelayCommand(AboutCommandHandler);
            SettingsCommand = new RelayCommand(SettingsCommandHandler);
            DiffCommand = new RelayCommand(DiffCommandHandler);
            PreFillCommand = new RelayCommand(PreFillCommandHandler);
        }

        private void NewProjectCreatedEventHandler(NewProjectCreatedEventArgs args)
        {
            mConfiguration.ExcludePatterns.Clear();

            mConfiguration.VisualStudioSolutionPath = args.VisualStudioSolutionPath;

            mConfiguration.LocalisationProjectPath = args.LocalisationProjectPath;

            mConfiguration.NeutralLanguage = args.NeutralLanguage;

            mConfiguration.SupportedLanguages.Clear();
            mConfiguration.SupportedLanguages.AddRange(args.SupportedLanguages);

            mConfigurationLoader.SaveConfiguration(mConfiguration, args.LocalisationProjectPath);

            LoadLocalisationProject(args.LocalisationProjectPath);
        }

        private void NewProjectCommandHandler()
        {
            App.Container.Resolve<NewProjectWindow>().ShowDialog();
        }

        private void SettingsChangedEventHandler(SettingsChangedEvent args)
        {
            mConfigurationLoader.SaveConfiguration(mConfiguration, mConfiguration.LocalisationProjectPath);
            if (mConfiguration.ExcludePatterns.Count > 0)
            {
                var stringPattern = string.Join("|", mConfiguration.ExcludePatterns);
                var finalPattern = $"^({stringPattern.Replace(@".", @"\.").Replace(@"*", @"\S*")})$";
                mExcludePatterns = new Regex(finalPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);   
            }
            else
            {
                mExcludePatterns = null;
            }
            if (mSelectedNode != null)
                SetSelectedNode(mSelectedNode);
        }

        private void SettingsCommandHandler()
        {
            App.Container.Resolve<SettingsWindow>().ShowDialog();
        }

        private void AboutCommandHandler()
        {
            new AboutWindow().ShowDialog();
        }

        private void ExportCommandHandler()
        {
            var exportWindow = App.Container.Resolve<ExportWindow>();
            exportWindow.Initialize(mRootNode.Node);
            exportWindow.ShowDialog();
        }

        private void ImportCommandHandler()
        {
            var importWindow = App.Container.Resolve<ImportWindow>();
            importWindow.Initialize(mRootNode.Node);
            importWindow.ShowDialog();
        }

        private void LoadLocalisationProject(string projectPath)
        {
            var config = mConfigurationLoader.LoadConfiguration(projectPath);

            mConfiguration.SupportedLanguages.Clear();
            mConfiguration.SupportedLanguages.AddRange(config.SupportedLanguages.ToArray());

            mConfiguration.ExcludePatterns.Clear();
            mConfiguration.ExcludePatterns.AddRange(config.ExcludePatterns.ToArray());

            mConfiguration.NeutralLanguage = config.NeutralLanguage;
            mConfiguration.VisualStudioSolutionPath = config.VisualStudioSolutionPath;
            mConfiguration.LocalisationProjectPath = config.LocalisationProjectPath;

            LoadSolution(mConfiguration.VisualStudioSolutionPath);
            IsLoaded = true;
        }

        private void LoadSolution(string solutionPath)
        {
            var solution = SolutionLoader.LoadSolution(solutionPath);

            LoadSolutionTree(solution);            
        }

        #region Command Handlers

        private void DiffCommandHandler()
        {
            //var diffWindow = mUnityContainer.Resolve<DiffWindow>();
            //diffWindow.Initialize(mRootNode.Node, mConfiguration.VisualStudioSolutionPath);
            //diffWindow.ShowDialog();
        }

        private void PreFillCommandHandler()
        {
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

            Dictionary<string, string> lookUpDictionary = new Dictionary<string, string>();

            List<Translation> untranslated = new List<Translation>();
            List<Translation> translated = new List<Translation>();


            var allResources = SolutionLoader.GetAllResourcesUnderNode(mRootNode.Node).ToList();

            var total = allResources.Count();
            var resourceCount = 1;
            var untranslatedCount = 0;
            var culture = CultureInfo.GetCultureInfo("fi");
            //foreach (var resource in allResources.TakeWhile(resource => !worker.CancellationPending))
            foreach (var resource in allResources)
            {
                //worker.ReportProgress((int)((double)resourceCount / (double)total * 100.0), resource.Location);
                //foreach (var pNode in SolutionLoader.GetResXNodes(resource.Location).TakeWhile(n => !worker.CancellationPending))
                foreach (var pNode in SolutionLoader.GetResXNodes(resource.Location))
                {
                    if (!SolutionLoader.IsResXNodeTranslatable(pNode)) continue;
                    //if (mExcludePatterns != null && mExcludePatterns.IsMatch(pNode.Name)) continue; //regex here


                    var resXnodeValue = SolutionLoader.GetResXNodeValue(pNode);
                    if (String.IsNullOrWhiteSpace(resXnodeValue.ToString())) continue;
                    if (mExcludePatterns != null && mExcludePatterns.IsMatch((string)resXnodeValue)) continue; //regex here
                    //worksheet.Cells[rowCount, 1].Value = resource.Location;
                    //worksheet.Cells[rowCount, 2].Value = pNode.Name;
                    //worksheet.Cells[rowCount, 3].Value = resXnodeValue;

                    var cultureResXnodes = SolutionLoader.GetCultureResXNodes(resource.Location,culture);
                    object obj = null;
                    var resXnode = SolutionLoader.FindResXNode(cultureResXnodes, pNode.Name);
                    if (resXnode != null)
                        obj = SolutionLoader.GetResXNodeValue(resXnode);
                    if (IsUntranslated((string) obj))
                    {
                        untranslatedCount++;
                        untranslated.Add(new Translation()
                        {
                            Culture = culture,
                            Key = pNode.Name,
                            Location = resource.Location,
                            NeutralValue = resXnodeValue.ToString()
                        });
                    }
                    else
                    {
                        if (!lookUpDictionary.ContainsKey(resXnodeValue.ToString()))
                            lookUpDictionary.Add(resXnodeValue.ToString(), (string)obj);
                    }
                    //worksheet.Cells[rowCount, column + 4].Value = obj;
                    resourceCount++;
                }
            }

            var count = 0;
            foreach (var item in untranslated)
            {
                if (lookUpDictionary.ContainsKey(item.NeutralValue))
                {
                    item.TranslatedValue = lookUpDictionary[item.NeutralValue];
                    translated.Add(item);
                    continue;
                }
                if (lookUpDictionary.ContainsKey(item.NeutralValue.ToLowerInvariant()))
                {
                    item.TranslatedValue = lookUpDictionary[item.NeutralValue];
                    translated.Add(item);
                }
            }

            foreach (var translation in translated)
            {
                SaveResources(translation);
            }
        }

        public void SaveResources(Translation translation)
        {
            string cultureResXfileName = SolutionLoader.GetCultureResXFileName(translation.Location, translation.Culture);
            List<ResXDataNode> resXnodes1 = SolutionLoader.GetResXNodes(translation.Location);
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
                        if (resXdataNode.Name == translation.Key)
                        {
                            resXresourceWriter.AddResource(new ResXDataNode(translation.Key, translation.TranslatedValue));
                            return;
                        }
                        else
                        {
                            resXresourceWriter.AddResource(resXdataNode);
                        }
                    }
                }
                resXresourceWriter.AddResource(new ResXDataNode(translation.Key, translation.TranslatedValue));
            }
        }

        public class Translation
        {
            public CultureInfo Culture { get; set; }

            public string Location { get; set; }

            public string Key { get; set; }

            public string NeutralValue { get; set; }

            public string TranslatedValue { get; set; }
        }

        private bool IsUntranslated(string value)
        {
            return String.IsNullOrWhiteSpace(value);
        }

        private void LoadProjectCommandHandler()
        {
            // Create OpenFileDialog 
            var dlg = new Microsoft.Win32.OpenFileDialog
            {               
                DefaultExt = ".loci",
                Filter = "Localisation Project (.loci)|*.loci"
            };

            // Set filter for file extension and default file extension 

            // Display OpenFileDialog by calling ShowDialog method 
            var result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                LoadLocalisationProject(dlg.FileName);
            }
        }

        #endregion

        public void SetSelectedNode(BaseNode node)
        {
            mSelectedNode = node;
            if (node.NodeType == TreeNodeType.Resource)
            {
                LoadResources(node as Resource);
                SelectedNodeDetailsView = new ResourceView();
            }
        }

        private void LoadResources(Resource resource)
        {
            mDataTable.Clear();
            mDataTable.Columns.Clear();
            
            var resXnodes = SolutionLoader.GetResXNodes(resource.Location);

            //Create Columns
            mDataTable.Columns.Add(new DataColumn("Key") {ReadOnly = true});
            mDataTable.Columns.Add(new DataColumn("Neutral"));

            //Create Language Headers
            foreach (var language in mConfiguration.SupportedLanguages)
            {
                mDataTable.Columns.Add(new DataColumn(language.EnglishName));
            }

            foreach (var pNode in resXnodes)
            {
                var resXnodeValue = SolutionLoader.GetResXNodeValue(pNode);
                if (SolutionLoader.IsResXNodeTranslatable(pNode) && (mExcludePatterns == null || !mExcludePatterns.IsMatch(pNode.Name))) //regex here
                {
                    var dataRow = mDataTable.NewRow();
                    dataRow[0] = pNode.Name;
                    dataRow[1] = resXnodeValue;

                    for (var i = 2; i < mConfiguration.SupportedLanguages.Count + 2; i++)
                    {
                        var cultureResXnodes = SolutionLoader.GetCultureResXNodes(resource.Location,
                            mConfiguration.SupportedLanguages.ElementAt(i - 2));
                        object obj = null;
                        var resXnode = SolutionLoader.FindResXNode(cultureResXnodes, pNode.Name);
                        if (resXnode != null)
                            obj = SolutionLoader.GetResXNodeValue(resXnode);
                        dataRow[i] = obj;
                    }

                    mDataTable.Rows.Add(dataRow);
                }
            }
            mDataTable.AcceptChanges();
            OnPropertyChanged("DefaultView");
        }

        public void LoadSolutionTree(BaseNode rootNode)
        {
            mRootNode = new TreeNodeViewModel(rootNode) {IsExpanded = true};
            mFirstGeneration = new ReadOnlyCollection<TreeNodeViewModel>(new[] {mRootNode});
            OnPropertyChanged("FirstGeneration");
        }

        #region Properties

        #region FirstGeneration

        /// <summary>
        /// Returns a read-only collection containing the first person 
        /// in the family tree, to which the TreeView can bind.
        /// </summary>
        public ReadOnlyCollection<TreeNodeViewModel> FirstGeneration
        {
            get { return mFirstGeneration; }
        }

        #endregion // FirstGeneration

        #endregion // Properties

        public void Dispose()
        {
            _messenger.Unregister<SettingsChangedEvent>(this, SettingsChangedEventHandler);
            _messenger.Unregister<NewProjectCreatedEventArgs>(this, NewProjectCreatedEventHandler);
        }
    }
}