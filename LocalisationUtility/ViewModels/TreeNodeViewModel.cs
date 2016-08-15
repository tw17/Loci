using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loci.Enums;
using Loci.Models;

namespace Loci.ViewModels
{
    public class TreeNodeViewModel : BaseViewModel
    {
        #region Data

        private readonly ReadOnlyCollection<TreeNodeViewModel> _children;
        private readonly TreeNodeViewModel mParent;
        private readonly BaseNode mNode;

        private bool mIsExpanded;
        private bool mIsSelected;
        private string mIcon;

        #endregion // Data

        public TreeNodeViewModel(BaseNode node)
            : this(node, null)
        {
        }

        private TreeNodeViewModel(BaseNode node, TreeNodeViewModel parent)
        {
            mNode = node;
            mParent = parent;

            _children = new ReadOnlyCollection<TreeNodeViewModel>(
                (from child in mNode.Children
                    select new TreeNodeViewModel(child, this))
                    .ToList<TreeNodeViewModel>());
        }

        #region Person Properties

        public ReadOnlyCollection<TreeNodeViewModel> Children
        {
            get { return _children; }
        }

        public string Name
        {
            get { return mNode.Name; }
        }

        public TreeNodeType NodeType
        {
            get { return mNode.NodeType; }
        }

        #endregion // Person Properties

        #region Presentation Members

        public BaseNode Node
        {
            get { return mNode; }
        }

        #region IsExpanded

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return mIsExpanded; }
            set
            {
                if (value != mIsExpanded)
                {
                    mIsExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                    this.OnPropertyChanged("Icon");
                }

                // Expand all the way up to the root.
                if (mIsExpanded && mParent != null)
                    mParent.IsExpanded = true;
            }
        }

        #endregion // IsExpanded

        public string Icon
        {
            get
            {
                if (mNode.NodeType == TreeNodeType.Solution)
                    return "/Resources/Images/Solution.png";
                if (mNode.NodeType == TreeNodeType.Project)
                    return "/Resources/Images/Project.png";
                if (mNode.NodeType == TreeNodeType.Folder)
                    return mIsExpanded ? "/Resources/Images/OpenFolder.png" : "/Resources/Images/ClosedFolder.png";
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
            get { return mIsSelected; }
            set
            {
                if (value != mIsSelected)
                {
                    mIsSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        #endregion // IsSelected

        #region NameContainsText

        public bool NameContainsText(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(this.Name))
                return false;

            return this.Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        #endregion // NameContainsText

        #region Parent

        public TreeNodeViewModel Parent
        {
            get { return mParent; }
        }

        #endregion // Parent

        #endregion // Presentation Members  
    }
}