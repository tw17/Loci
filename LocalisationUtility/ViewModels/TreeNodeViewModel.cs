using System;
using System.Collections.ObjectModel;
using System.Linq;
using Loci.Enums;
using Loci.Models;

namespace Loci.ViewModels
{
    public class TreeNodeViewModel : BaseViewModel
    {
        #region Data

        private bool _isExpanded;
        private bool _isSelected;
        private string _icon;

        #endregion // Data

        public TreeNodeViewModel(BaseNode node)
            : this(node, null)
        {
        }

        private TreeNodeViewModel(BaseNode node, TreeNodeViewModel parent)
        {
            Node = node;
            Parent = parent;

            Children = new ReadOnlyCollection<TreeNodeViewModel>(
                (from child in Node.Children
                    select new TreeNodeViewModel(child, this))
                    .ToList());
        }

        #region Person Properties

        public ReadOnlyCollection<TreeNodeViewModel> Children { get; }

        public string Name => Node.Name;

        public TreeNodeType NodeType => Node.NodeType;

        #endregion // Person Properties

        #region Presentation Members

        public BaseNode Node { get; }

        #region IsExpanded

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                    OnPropertyChanged("Icon");
                }

                // Expand all the way up to the root.
                if (_isExpanded && Parent != null)
                    Parent.IsExpanded = true;
            }
        }

        #endregion // IsExpanded

        public string Icon
        {
            get
            {
                if (Node.NodeType == TreeNodeType.Solution)
                    return "/Resources/Images/Solution.png";
                if (Node.NodeType == TreeNodeType.Project)
                    return "/Resources/Images/Project.png";
                if (Node.NodeType == TreeNodeType.Folder)
                    return _isExpanded ? "/Resources/Images/OpenFolder.png" : "/Resources/Images/ClosedFolder.png";
                return "/Resources/Images/Resource.png";
            }
        }

        #region IsSelected

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        #endregion // IsSelected

        #region NameContainsText

        public bool NameContainsText(string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(Name))
                return false;

            return Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        #endregion // NameContainsText

        #region Parent

        public TreeNodeViewModel Parent { get; }

        #endregion // Parent

        #endregion // Presentation Members  
    }
}