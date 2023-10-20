using System.Collections.Generic;
using System.Linq;

namespace Upolnicek
{
    internal class FileExplorerTree
    {
        public FileCheckBox CheckBox { get; set; }

        public List<FileExplorerTree> children { get; set; }

        public bool IsDirectory { get; set; }

        public FileExplorerTree Parent { get; set; }

        public FileExplorerTree(FileCheckBox cb, FileExplorerTree parent, bool isDirectory = false)
        {
            children = new List<FileExplorerTree>();
            CheckBox = cb;
            Parent = parent;
            IsDirectory = isDirectory;
        }

        public void ChangeStateToChildren(bool stateTo)
        {
            foreach (var child in children)
            {
                child.CheckBox.IsChecked = stateTo;
                child.ChangeStateToChildren(stateTo);
            }
        }

        public void RepairTreeAbove(FileExplorerTree node)
        {
            if (node == null)
                return;

            node.CheckBox.IsChecked = node.children.Any(x => x.CheckBox.IsChecked == true);

            if (node.Parent != null)
                RepairTreeAbove(node.Parent);
        }

        public FileExplorerTree Find(FileCheckBox checkBox)
        {
            if (checkBox == CheckBox)
                return this;

            foreach (var child in children)
            {
                var result = child.Find(checkBox);
                if (result != null)
                    return result;
            }
            

            return null;
        }

        public List<FileExplorerTree> ToList()
        {
            var list = new List<FileExplorerTree>();
            list.Add(this);

            foreach (var child in children)
                list.AddRange(child.ToList());

            return list;
        }
    }
}
