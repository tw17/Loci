using System;
using System.Windows;
using System.Windows.Controls;
using Loci.ViewModels;

namespace Loci
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly MainViewModel _viewModel;

        private TreeNodeViewModel _currentTreeNode;

        public MainWindow(MainViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
            InitializeComponent();
        }

        private void SetSelectedTreeNode(TreeNodeViewModel treeNode)
        {
            _currentTreeNode = treeNode;
            _viewModel.SetSelectedNode(_currentTreeNode.Node);
        }

        private void ResourceTreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_currentTreeNode != null && _viewModel.PendingChanges)
            {
                if (e.NewValue == _currentTreeNode)
                {
                    // Will only end up here when reversing item
                    // Without this line childs can't be selected
                    // twice if "No" was pressed in the question..   
                    ResourceTreeView.Focus();
                }
                else
                {
                    if (MessageBox.Show("Change TreeViewItem?",
                        "Really change",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        EventHandler eventHandler = null;
                        eventHandler = delegate
                        {
                            ResourceTreeView.LayoutUpdated -= eventHandler;
                            _currentTreeNode.IsSelected = true;
                        };
                        // Will be fired after SelectedItemChanged, to early to change back here
                        ResourceTreeView.LayoutUpdated += eventHandler;
                    }
                    else
                    {
                        SetSelectedTreeNode(e.NewValue as TreeNodeViewModel);
                    }
                }
            }
            else
            {
                SetSelectedTreeNode(e.NewValue as TreeNodeViewModel);
            }
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            var toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }
            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = new Thickness();
            }
        }
    }
}