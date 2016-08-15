using System.Collections.Generic;
using System.Linq;
using Loci.Enums;

namespace Loci.Models
{
    public abstract class BaseNode
    {
        private readonly List<BaseNode> _children = new List<BaseNode>();

        public IList<BaseNode> Children => _children;

        public abstract TreeNodeType NodeType { get; }

        public string Name { get; set; }

        public void AddNode(string[] folders, BaseNode node)
        {
            if (folders != null && folders.Count() != 0)
            {
                var existingFolder = Children.FirstOrDefault(e => e.Name == folders.First());
                if (existingFolder != null)
                {
                    existingFolder.AddNode(folders.Skip(1).ToArray(), node);
                }
                else
                {
                    var folder = new Folder() {Name = folders.First()};
                    folder.AddNode(folders.Skip(1).ToArray(), node);
                    Children.Add(folder);
                }
            }
            else
            {
                Children.Add(node);
            }
        }
    }
}