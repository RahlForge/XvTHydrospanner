using System;
using System.Windows;
using System.Windows.Forms;
using XvTHydrospanner.Services;

namespace XvTHydrospanner.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigurationManager _configManager;
        
        public SettingsWindow(ConfigurationManager configManager)
        {
            InitializeComponent();
            _configManager = configManager;
            LoadSettings();
        }
        
        private void LoadSettings()
        {
            var config = _configManager.GetConfig();
            GamePathTextBox.Text = config.GameInstallPath;
            WarehousePathTextBox.Text = config.WarehousePath;
            AutoBackupCheckBox.IsChecked = config.AutoBackup;
            ConfirmApplyCheckBox.IsChecked = config.ConfirmBeforeApply;
        }
        
        private void BrowseGamePath_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Select Star Wars XvT installation folder";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                GamePathTextBox.Text = dialog.SelectedPath;
            }
        }
        
        private void BrowseWarehousePath_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Select Mod Warehouse folder";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                WarehousePathTextBox.Text = dialog.SelectedPath;
            }
        }
        
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(GamePathTextBox.Text))
                {
                    System.Windows.MessageBox.Show("Please specify the game install path.", "Validation", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var config = _configManager.GetConfig();
                config.GameInstallPath = GamePathTextBox.Text;
                config.WarehousePath = WarehousePathTextBox.Text;
                config.AutoBackup = AutoBackupCheckBox.IsChecked ?? true;
                config.ConfirmBeforeApply = ConfirmApplyCheckBox.IsChecked ?? true;
                
                await _configManager.UpdateConfigAsync(config);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
