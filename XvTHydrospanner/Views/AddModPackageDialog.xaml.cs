using System;
using System.IO;
using System.Linq;
using System.Windows;
using XvTHydrospanner.Services;

namespace XvTHydrospanner.Views
{
    public partial class AddModPackageDialog : Window
    {
        public string ModName => ModNameTextBox.Text;
        public string Description => DescriptionTextBox.Text;
        public bool CopyToGameRoot => CopyToGameRootCheckBox.IsChecked ?? false;
        
        public AddModPackageDialog(string archivePath)
        {
            InitializeComponent();
            ArchiveFileTextBox.Text = archivePath;
            
            // Set default name from archive filename
            ModNameTextBox.Text = Path.GetFileNameWithoutExtension(archivePath);
            
            try
            {
                // List files in archive
                var files = ArchiveExtractor.ListArchiveContents(archivePath);
                FilesPreviewTextBlock.Text = files.Count > 0 
                    ? string.Join("\n", files.Take(20)) + (files.Count > 20 ? $"\n... and {files.Count - 20} more" : "")
                    : "No files found in archive";
            }
            catch (Exception ex)
            {
                FilesPreviewTextBlock.Text = $"Error reading archive: {ex.Message}";
            }
        }
        
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ModNameTextBox.Text))
            {
                MessageBox.Show("Please enter a name for the mod package.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            DialogResult = true;
            Close();
        }
    }
}
