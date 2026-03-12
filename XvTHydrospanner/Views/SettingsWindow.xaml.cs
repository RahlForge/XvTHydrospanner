using System;
using System.Windows;
using System.Windows.Forms;
using XvTHydrospanner.Services;

namespace XvTHydrospanner.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigurationManager _configManager;
        private readonly BaseGameBackupManager? _backupManager;
        
        public SettingsWindow(ConfigurationManager configManager, BaseGameBackupManager? backupManager = null)
        {
            InitializeComponent();
            _configManager = configManager;
            _backupManager = backupManager;
            LoadSettings();
        }
        
        private void LoadSettings()
        {
            var config = _configManager.GetConfig();
            GamePathTextBox.Text = config.GameInstallPath;
            WarehousePathTextBox.Text = config.WarehousePath;
            AutoBackupCheckBox.IsChecked = config.AutoBackup;
            ConfirmApplyCheckBox.IsChecked = config.ConfirmBeforeApply;
            RemoteOwnerTextBox.Text = config.RemoteRepositoryOwner ?? string.Empty;
            RemoteRepoTextBox.Text = config.RemoteRepositoryName ?? string.Empty;
            ModdedInstallsRepoTextBox.Text = config.ModdedInstallsRepositoryName ?? string.Empty;
            RemoteBranchTextBox.Text = config.RemoteRepositoryBranch ?? string.Empty;
            GitHubTokenBox.Password = config.GitHubToken ?? string.Empty;
            
            UpdateBackupStatus(config);
        }
        
        private void UpdateBackupStatus(Models.AppConfig config)
        {
            if (config.BaseGameBackupExists && config.BaseGameBackupCreatedDate.HasValue)
            {
                BackupStatusText.Text = $"Backup exists — created {config.BaseGameBackupCreatedDate.Value:g}  |  Location: {config.BaseGameBackupPath}";
                RestoreBackupButton.IsEnabled = true;
                RecreateBackupButton.IsEnabled = true;
            }
            else
            {
                BackupStatusText.Text = "No backup created yet.";
                RestoreBackupButton.IsEnabled = false;
                RecreateBackupButton.IsEnabled = false;
            }
            
            // Disable backup buttons if no manager was provided
            if (_backupManager == null)
            {
                RecreateBackupButton.IsEnabled = false;
                RestoreBackupButton.IsEnabled = false;
            }
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
        
        private async void RecreateBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (_backupManager == null) return;
            
            var config = _configManager.GetConfig();
            if (string.IsNullOrWhiteSpace(config.GameInstallPath))
            {
                System.Windows.MessageBox.Show("Please save a valid game install path before recreating the backup.",
                    "No Game Path", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var confirm = System.Windows.MessageBox.Show(
                "This will permanently delete the existing base game backup and create a new one from the current game directory.\n\n" +
                "This action is irreversible. Are you sure you want to continue?",
                "Confirm Recreate Backup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (confirm != MessageBoxResult.Yes) return;
            
            try
            {
                RecreateBackupButton.IsEnabled = false;
                RestoreBackupButton.IsEnabled = false;
                
                var progressDialog = new BackupProgressDialog(_backupManager, config.GameInstallPath);
                progressDialog.Owner = this;
                var result = progressDialog.ShowDialog();
                
                if (result == true)
                {
                    await _configManager.SetBaseGameBackupCompletedAsync(config.BaseGameBackupPath, DateTime.Now);
                    UpdateBackupStatus(_configManager.GetConfig());
                    System.Windows.MessageBox.Show("Base game backup recreated successfully.",
                        "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error recreating backup: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateBackupStatus(_configManager.GetConfig());
            }
        }
        
        private async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (_backupManager == null) return;
            
            var config = _configManager.GetConfig();
            
            var confirm = System.Windows.MessageBox.Show(
                "Restoring the base game backup will overwrite all current game files with the clean backup copies.\n\n" +
                "Any installed mods in the game directory will be removed. The 'Base Game Install' profile will be set as active.\n\n" +
                "Are you sure you want to restore?",
                "Confirm Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (confirm != MessageBoxResult.Yes) return;
            
            try
            {
                RestoreBackupButton.IsEnabled = false;
                RecreateBackupButton.IsEnabled = false;
                
                // Run restore on background thread and report progress via status text
                _backupManager.ProgressChanged += OnRestoreProgress;
                await _backupManager.RestoreBackupAsync(config.GameInstallPath);
                _backupManager.ProgressChanged -= OnRestoreProgress;
                
                System.Windows.MessageBox.Show(
                    "Base game restored successfully. The 'Base Game Install' profile is now active.",
                    "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error restoring backup: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateBackupStatus(_configManager.GetConfig());
            }
        }
        
        private void OnRestoreProgress(object? sender, Models.BackupProgress progress)
        {
            Dispatcher.Invoke(() =>
            {
                BackupStatusText.Text = progress.StatusMessage;
            });
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
                
                // Save remote repository settings (null if empty)
                config.RemoteRepositoryOwner = string.IsNullOrWhiteSpace(RemoteOwnerTextBox.Text) 
                    ? null : RemoteOwnerTextBox.Text;
                config.RemoteRepositoryName = string.IsNullOrWhiteSpace(RemoteRepoTextBox.Text) 
                    ? null : RemoteRepoTextBox.Text;
                config.ModdedInstallsRepositoryName = string.IsNullOrWhiteSpace(ModdedInstallsRepoTextBox.Text) 
                    ? null : ModdedInstallsRepoTextBox.Text;
                config.RemoteRepositoryBranch = string.IsNullOrWhiteSpace(RemoteBranchTextBox.Text) 
                    ? null : RemoteBranchTextBox.Text;
                config.GitHubToken = string.IsNullOrWhiteSpace(GitHubTokenBox.Password) 
                    ? null : GitHubTokenBox.Password;
                
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
