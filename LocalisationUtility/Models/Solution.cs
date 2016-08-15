using System.Linq;
using Loci.Enums;

namespace Loci.Models
{
    public class Solution : BaseNode
    {
        public override TreeNodeType NodeType => TreeNodeType.Solution;

        public void AddProject(Project project)
        {
            var folders =
                project.RelativePath.Split('\\').Where(s => !s.ToLowerInvariant().EndsWith(".csproj")).ToArray();
            AddProject(folders, project);
        }

        private void AddProject(string[] folders, Project project)
        {
            if (folders != null && folders.Count() != 0)
            {
                var existingFolder = Children.FirstOrDefault(e => e.Name == folders.First());
                if (existingFolder != null)
                {
                    existingFolder.AddNode(folders.Skip(1).ToArray(), project);
                }
                else
                {
                    var folder = new Folder() {Name = folders.First()};
                    folder.AddNode(folders.Skip(1).ToArray(), project);
                    Children.Add(folder);
                }
            }
            else
            {
                Children.Add(project);
            }
        }
    }
}