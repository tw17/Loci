using LocalisationUtility.Enums;

namespace LocalisationUtility.Models
{
    public class Resource : BaseNode
    {
        public string Location { get; }

        public override TreeNodeType NodeType => TreeNodeType.Resource;

        public Resource(string name, string location)
        {
            Name = name;
            Location = location;
        }
    }
}