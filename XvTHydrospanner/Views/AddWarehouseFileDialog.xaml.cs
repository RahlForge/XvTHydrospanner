using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using XvTHydrospanner.Models;

namespace XvTHydrospanner.Views
{
    public partial class AddWarehouseFileDialog : Window
    {
        public string FileName => NameTextBox.Text;
        public string FileDescription => DescriptionTextBox.Text;
        public ModCategory FileCategory => (ModCategory)(CategoryComboBox.SelectedItem ?? ModCategory.Other);
        public string TargetPath => TargetPathTextBox.Text;
        public string? Author => null;
        public string? Version => null;
        public List<string> Tags => new();
        
        public AddWarehouseFileDialog(string sourceFilePath)
        {
            InitializeComponent();
            SourceFileTextBox.Text = sourceFilePath;
            
            // Set default name from filename
            NameTextBox.Text = Path.GetFileNameWithoutExtension(sourceFilePath);
            
            // Populate category dropdown
            CategoryComboBox.ItemsSource = Enum.GetValues(typeof(ModCategory));
            CategoryComboBox.SelectedIndex = 0;
            
            // Try to guess target path based on file extension
            var extension = Path.GetExtension(sourceFilePath).ToUpper();
            if (extension == ".TIE")
            {
                TargetPathTextBox.Text = "BalanceOfPower/BATTLE/" + Path.GetFileName(sourceFilePath);
                CategoryComboBox.SelectedItem = ModCategory.Mission;
            }
            else if (extension == ".LST")
            {
                TargetPathTextBox.Text = "BalanceOfPower/BATTLE/" + Path.GetFileName(sourceFilePath);
                CategoryComboBox.SelectedItem = ModCategory.Battle;
            }
        }
        
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Please enter a name for the file.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(TargetPathTextBox.Text))
            {
                MessageBox.Show("Please specify the target path.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            DialogResult = true;
            Close();
        }
    }
}
