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
        private readonly Configuration _configuration;
        private readonly IConfigurationLoader _configurationLoader;
        private readonly IMessenger _messenger;
        private ReadOnlyCollection<TreeNodeViewModel> _firstGeneration;
        private TreeNodeViewModel _rootNode;
        private UserControl _selectedNodeDetailsView;
        private readonly DataTable _dataTable = new DataTable();
        private BaseNode _selectedNode;
        private Regex _excludePatterns;
        private bool _isLoaded;

        public DataView DefaultView => _dataTable.AsDataView();

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
            get { return _selectedNodeDetailsView; }
            private set
            {
                if (Equals(_selectedNodeDetailsView, value)) return;
                _selectedNodeDetailsView = value;
                OnPropertyChanged("SelectedNodeDetailsView");
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
            get { return _isLoaded; }
            set
            {
                if (value == _isLoaded) return;
                _isLoaded = value;
                OnPropertyChanged("IsLoaded");
            }
        }

        public bool PendingChanges => _dataTable.GetChanges() != null;

        #endregion

        public MainViewModel(IMessenger messenger, IConfigurationLoader configurationLoader, Configuration configuration)
        {
            _messenger = messenger;
            _configurationLoader = configurationLoader;
            _messenger.Register<SettingsChangedEvent>(this, SettingsChangedEventHandler);
            _messenger.Register<NewProjectCreatedEventArgs>(this, NewProjectCreatedEventHandler);
            _configuration = configuration;

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
            _configuration.ExcludePatterns.Clear();

            _configuration.VisualStudioSolutionPath = args.VisualStudioSolutionPath;

            _configuration.LocalisationProjectPath = args.LocalisationProjectPath;

            _configuration.NeutralLanguage = args.NeutralLanguage;

            _configuration.SupportedLanguages.Clear();
            _configuration.SupportedLanguages.AddRange(args.SupportedLanguages);

            _configurationLoader.SaveConfiguration(_configuration, args.LocalisationProjectPath);

            LoadLocalisationProject(args.LocalisationProjectPath);
        }

        private void NewProjectCommandHandler()
        {
            App.Container.Resolve<NewProjectWindow>().ShowDialog();
        }

        private void SettingsChangedEventHandler(SettingsChangedEvent args)
        {
            _configurationLoader.SaveConfiguration(_configuration, _configuration.LocalisationProjectPath);
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
            if (_selectedNode != null)
                SetSelectedNode(_selectedNode);
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
            exportWindow.Initialize(_rootNode.Node);
            exportWindow.ShowDialog();
        }

        private void ImportCommandHandler()
        {
            var importWindow = App.Container.Resolve<ImportWindow>();
            importWindow.Initialize(_rootNode.Node);
            importWindow.ShowDialog();
        }

        private void LoadLocalisationProject(string projectPath)
        {
            var config = _configurationLoader.LoadConfiguration(projectPath);

            _configuration.SupportedLanguages.Clear();
            _configuration.SupportedLanguages.AddRange(config.SupportedLanguages.ToArray());

            _configuration.ExcludePatterns.Clear();
            _configuration.ExcludePatterns.AddRange(config.ExcludePatterns.ToArray());

            _configuration.NeutralLanguage = config.NeutralLanguage;
            _configuration.VisualStudioSolutionPath = config.VisualStudioSolutionPath;
            _configuration.LocalisationProjectPath = config.LocalisationProjectPath;

            LoadSolution(_configuration.VisualStudioSolutionPath);
            IsLoaded = true;
        }

        private void LoadSolution(string solutionPath)
        {
            var solution = SolutionLoader.LoadSolution(solutionPath);

            LoadSolutionTree(solution);            
        }

        #region Command Handlers

        private static void DiffCommandHandler()
        {
            //var diffWindow = mUnityContainer.Resolve<DiffWindow>();
            //diffWindow.Initialize(_rootNode.Node, _configuration.VisualStudioSolutionPath);
            //diffWindow.ShowDialog();
        }

        private void PreFillCommandHandler()
        {
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

            var lookUpDictionary = new Dictionary<string, string>();

            var untranslated = new List<Translation>();
            var translated = new List<Translation>();


            var allResources = SolutionLoader.GetAllResourcesUnderNode(_rootNode.Node).ToList();

            //var resourceCount = 1;
            //var untranslatedCount = 0;
            var culture = CultureInfo.GetCultureInfo("fi");
            //foreach (var resource in allResources.TakeWhile(resource => !worker.CancellationPending))
            foreach (var resource in allResources)
            {
                //worker.ReportProgress((int)((double)resourceCount / (double)total * 100.0), resource.Location);
                //foreach (var pNode in SolutionLoader.GetResXNodes(resource.Location).TakeWhile(n => !worker.CancellationPending))
                foreach (var pNode in SolutionLoader.GetResXNodes(resource.Location))
                {
                    if (!SolutionLoader.IsResXNodeTranslatable(pNode)) continue;
                    //if (_excludePatterns != null && _excludePatterns.IsMatch(pNode.Name)) continue; //regex here


                    var resXnodeValue = SolutionLoader.GetResXNodeValue(pNode);
                    if (String.IsNullOrWhiteSpace(resXnodeValue.ToString())) continue;
                    if (_excludePatterns != null && _excludePatterns.IsMatch((string)resXnodeValue)) continue; //regex here
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
                        //untranslatedCount++;
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
                    //resourceCount++;
                }
            }

            //var count = 0;
            foreach (var item in untranslated)
            {
                if (lookUpDictionary.ContainsKey(item.NeutralValue))
                {
                    item.TranslatedValue = lookUpDictionary[item.NeutralValue];
                    translated.Add(item);
                    continue;
                }
                if (!lookUpDictionary.ContainsKey(item.NeutralValue.ToLowerInvariant())) continue;
                item.TranslatedValue = lookUpDictionary[item.NeutralValue];
                translated.Add(item);
            }

            foreach (var translation in translated)
            {
                SaveResources(translation);
            }
        }

        public void SaveResources(Translation translation)
        {
            var cultureResXfileName = SolutionLoader.GetCultureResXFileName(translation.Location, translation.Culture);
            var resXnodes1 = SolutionLoader.GetResXNodes(translation.Location);
            var resXnodes2 = SolutionLoader.GetResXNodes(cultureResXfileName);
            using (var resXresourceWriter = new ResXResourceWriter(cultureResXfileName))
            {
                foreach (var resXdataNode in resXnodes2)
                {
                    if (!SolutionLoader.IsResXNodeTranslatable(resXdataNode) && SolutionLoader.FindResXNode(resXnodes1, resXdataNode.Name) != null)
                        resXresourceWriter.AddResource(resXdataNode);
                }

                foreach (var resXdataNode in resXnodes2)
                {
                    if (!SolutionLoader.IsResXNodeTranslatable(resXdataNode)) continue;
                    if (resXdataNode.Name == translation.Key)
                    {
                        resXresourceWriter.AddResource(new ResXDataNode(translation.Key, translation.TranslatedValue));
                        return;
                    }
                    resXresourceWriter.AddResource(resXdataNode);
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
            _selectedNode = node;
            if (node.NodeType == TreeNodeType.Resource)
            {
                LoadResources(node as Resource);
                SelectedNodeDetailsView = new ResourceView();
            }
        }

        private void LoadResources(Resource resource)
        {
            _dataTable.Clear();
            _dataTable.Columns.Clear();
            
            var resXnodes = SolutionLoader.GetResXNodes(resource.Location);

            //Create Columns
            _dataTable.Columns.Add(new DataColumn("Key") {ReadOnly = true});
            _dataTable.Columns.Add(new DataColumn("Neutral"));

            //Create Language Headers
            foreach (var language in _configuration.SupportedLanguages)
            {
                _dataTable.Columns.Add(new DataColumn(language.EnglishName));
            }

            foreach (var pNode in resXnodes)
            {
                var resXnodeValue = SolutionLoader.GetResXNodeValue(pNode);
                if (SolutionLoader.IsResXNodeTranslatable(pNode) && (_excludePatterns == null || !_excludePatterns.IsMatch(pNode.Name))) //regex here
                {
                    var dataRow = _dataTable.NewRow();
                    dataRow[0] = pNode.Name;
                    dataRow[1] = resXnodeValue;

                    for (var i = 2; i < _configuration.SupportedLanguages.Count + 2; i++)
                    {
                        var cultureResXnodes = SolutionLoader.GetCultureResXNodes(resource.Location,
                            _configuration.SupportedLanguages.ElementAt(i - 2));
                        object obj = null;
                        var resXnode = SolutionLoader.FindResXNode(cultureResXnodes, pNode.Name);
                        if (resXnode != null)
                            obj = SolutionLoader.GetResXNodeValue(resXnode);
                        dataRow[i] = obj;
                    }

                    _dataTable.Rows.Add(dataRow);
                }
            }
            _dataTable.AcceptChanges();
            OnPropertyChanged("DefaultView");
        }

        public void LoadSolutionTree(BaseNode rootNode)
        {
            _rootNode = new TreeNodeViewModel(rootNode) {IsExpanded = true};
            _firstGeneration = new ReadOnlyCollection<TreeNodeViewModel>(new[] {_rootNode});
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
            get { return _firstGeneration; }
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