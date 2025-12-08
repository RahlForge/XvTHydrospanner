using System.Collections.Generic;
using System.Windows.Controls;
using System.IO;

namespace XvTHydrospanner.Views
{
    public partial class GameFilesBrowser : Page
    {
        public GameFilesBrowser(string gamePath)
        {
            InitializeComponent();
            GamePathText.Text = $"Browsing: {gamePath}";
            LoadGameFiles(gamePath);
        }
        
        private void LoadGameFiles(string gamePath)
        {
            if (Directory.Exists(gamePath) == false)
            {
                GamePathText.Text = "Game path not found";
                return;
            }
            
            var rootNode = new FileSystemNode { Name = "Game Root", Path = gamePath };
            LoadDirectory(rootNode, gamePath);
            FilesTreeView.ItemsSource = new[] { rootNode };
        }
        
        private void LoadDirectory(FileSystemNode node, string path)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var childNode = new FileSystemNode 
                    { 
                        Name = $"📁 {dirInfo.Name}", 
                        Path = dir 
                    };
                    node.Children.Add(childNode);
                    LoadDirectory(childNode, dir);
                }
                
                foreach (var file in Directory.GetFiles(path))
                {
                    var fileInfo = new FileInfo(file);
                    node.Children.Add(new FileSystemNode 
                    { 
                        Name = $"📄 {fileInfo.Name}", 
                        Path = file 
                    });
                }
            }
            catch { }
        }
    }
    
    public class FileSystemNode
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public List<FileSystemNode> Children { get; set; } = new();
    }
}
