using LocalisationUtility.Enums;

namespace LocalisationUtility.Models
{
    public class Project : BaseNode
    {
        public override TreeNodeType NodeType => TreeNodeType.Project;

        public string AbsolutePath { get; set; }

        public string RelativePath { get; set; }

        public string Guid { get; set; }

        public void AddResource(string[] folders, Resource resource)
        {
            AddNode(folders, resource);
        }
    }
}