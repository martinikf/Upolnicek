using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.IO;
using System.Windows.Media;

namespace Upolnicek.Builders
{
    internal class FileExplorerBuilder
    {
        private FileExplorerTree _fileExplorerTree;
        private Brush _fontColor;

        public FileExplorerBuilder(Brush fontColor)
        {
            _fontColor = fontColor;
        }

        public FileExplorerTree Build(StackPanel container, string projectPath)
        {
            try
            {
                container.Children.Clear();

                var directoryInfo = new DirectoryInfo(projectPath);
                InsertDirectory(directoryInfo, container);

                return _fileExplorerTree;
            }
            catch
            {
                //Probably happend because of: wrong path; os forbid access to file/dir
                return null;
            }
        }

        private void InsertDirectory(DirectoryInfo directoryInfo, StackPanel parentContainer, FileExplorerTree parentDirectoryDS = null)
        {
            // Add horizontal StackPanel that will hold directory arrow and checkbox
            var directoryContainer = CreateStackPanel(Orientation.Horizontal, new Thickness(0, 0, 0, 0), Visibility.Visible);
            parentContainer.Children.Add(directoryContainer);

            // Arrow next to dir to list contents
            var arrowIcon = CreateTextBlock("▶", 12, new Thickness(0, 0, 5, 0), VerticalAlignment.Center, HorizontalAlignment.Left);
            directoryContainer.Children.Add(arrowIcon);

            // Checkbox for selecting the directory
            var directoryCheckBox = CreateCheckBox(directoryInfo.Name, directoryInfo.FullName, true);
            directoryCheckBox.Click += FileOrDirectoryCheckBoxOnClick;
            directoryContainer.Children.Add(directoryCheckBox);

            // Fill data structure
            var currentDirectoryDS = CreateFileExplorerTree(directoryCheckBox, parentDirectoryDS);

            // StackPanel for contents of the directory
            var directoryContentsContainer = CreateStackPanel(Orientation.Vertical, new Thickness(20, 0, 0, 0), Visibility.Collapsed);
            parentContainer.Children.Add(directoryContentsContainer);

            arrowIcon.MouseLeftButtonDown += (s, e) =>
            {
                ToggleVisibility(arrowIcon, directoryContentsContainer);
            };

            // Recursively add subdirectories
            foreach (var subDirectory in directoryInfo.GetDirectories())
            {
                InsertDirectory(subDirectory, directoryContentsContainer, currentDirectoryDS);
            }

            // Add files (checkboxes) after subdirectories
            foreach (var file in directoryInfo.GetFiles())
            {
                var fileCheckBox = CreateCheckBox(file.Name, file.FullName, false);
                fileCheckBox.Click += FileOrDirectoryCheckBoxOnClick;
                directoryContentsContainer.Children.Add(fileCheckBox);

                currentDirectoryDS.children.Add(new FileExplorerTree(fileCheckBox, currentDirectoryDS));
            }
        }

        private StackPanel CreateStackPanel(Orientation orientation, Thickness margin, Visibility visibility)
        {
            return new StackPanel
            {
                Orientation = orientation,
                Margin = margin,
                Visibility = visibility
            };
        }

        private TextBlock CreateTextBlock(string text, double fontSize, Thickness margin, VerticalAlignment verticalAlignment, HorizontalAlignment horizontalAlignment)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                Margin = margin,
                VerticalAlignment = verticalAlignment,
                HorizontalAlignment = horizontalAlignment
            };
        }

        private FileCheckBox CreateCheckBox(string name, string fullName, bool isDirectory)
        {
            return new FileCheckBox
            {
                Content = name,
                FullPath = fullName,
                FontWeight = isDirectory ? FontWeights.Bold : FontWeights.Normal,
                Foreground = _fontColor
            };
        }

        private FileExplorerTree CreateFileExplorerTree(FileCheckBox directoryCheckBox, FileExplorerTree parentDirectoryDS)
        {
            FileExplorerTree currentDirectoryDS;
            if (parentDirectoryDS == null)
            {
                // Root directory
                _fileExplorerTree = new FileExplorerTree(directoryCheckBox, null, true);
                currentDirectoryDS = _fileExplorerTree;
            }
            else
            {
                currentDirectoryDS = new FileExplorerTree(directoryCheckBox, parentDirectoryDS, true);
                parentDirectoryDS.children.Add(currentDirectoryDS);
            }
            return currentDirectoryDS;
        }

        private void ToggleVisibility(TextBlock arrowIcon, UIElement container)
        {
            container.Visibility = container.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            arrowIcon.Text = container.Visibility == Visibility.Visible ? "▼" : "▶";
        }

        private void FileOrDirectoryCheckBoxOnClick(object s, RoutedEventArgs e)
        {
            try
            {
                var cb = s as FileCheckBox;

                var treeNode = _fileExplorerTree.Find(cb);
                bool isChecked = cb.IsChecked == true ? true : false;
                treeNode.ChangeStateToChildren(isChecked);
                treeNode.RepairTreeAbove(treeNode.Parent);
            }
            catch
            {
                return;
            }
        }
    }
}
