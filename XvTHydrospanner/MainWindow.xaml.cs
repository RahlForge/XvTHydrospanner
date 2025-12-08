using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using XvTHydrospanner.Models;
using XvTHydrospanner.Services;
using XvTHydrospanner.Views;

namespace XvTHydrospanner
{
    [SuppressMessage("ReSharper", "InvertIf")]
    public partial class MainWindow
    {
        private ConfigurationManager? _configManager;
        private ProfileManager? _profileManager;
        private WarehouseManager? _warehouseManager;
        private ModApplicator? _modApplicator;
        
        public MainWindow()
        {
            InitializeComponent();
            InitializeServices();
            Loaded += MainWindow_Loaded;
        }
        
        private void InitializeServices()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XvTHydrospanner"
            );
            
            var configPath = Path.Combine(appDataPath, "config.json");
            _configManager = new ConfigurationManager(configPath);
        }
        
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_configManager == null)
                {
                    MessageBox.Show("Configuration manager not initialized.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return;
                }
                
                // Load configuration
                var config = await _configManager.LoadConfigAsync();
                
                // Validate configuration
                var (isValid, errors) = _configManager.ValidateConfig();
                if (isValid == false)
                {
                    // Show message if game install path is not set
                    if (string.IsNullOrWhiteSpace(config.GameInstallPath))
                    {
                        MessageBox.Show(
                            "Game installation path is not configured.\n\nPlease select your Star Wars X-Wing vs TIE Fighter installation folder.",
                            "Configuration Required",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    
                    // Show settings dialog if config is invalid
                    var settingsWindow = new SettingsWindow(_configManager);
                    if (settingsWindow.ShowDialog() != true)
                    {
                        Application.Current.Shutdown();
                        return;
                    }
                    config = _configManager.GetConfig();
                }
                
                // Initialize services with loaded config
                _profileManager = new ProfileManager(config.ProfilesPath);
                _warehouseManager = new WarehouseManager(config.WarehousePath);
                _modApplicator = new ModApplicator(config.GameInstallPath, config.BackupPath, _warehouseManager);
                
                // Load data
                await _warehouseManager.LoadCatalogAsync();
                await _profileManager.LoadAllProfilesAsync();
                
                // Update UI
                UpdateProfileComboBox();
                UpdateStatusBar(config);
                
                // Navigate to default page
                NavigateToProfileManagement();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }
        
        private void UpdateProfileComboBox()
        {
            if (_profileManager == null) return;
            
            var profiles = _profileManager.GetAllProfiles();
            ProfileComboBox.ItemsSource = profiles;
            ProfileComboBox.DisplayMemberPath = "Name";
            
            var activeProfile = _profileManager.GetActiveProfile();
            if (activeProfile != null)
            {
                ProfileComboBox.SelectedItem = activeProfile;
            }
            else if (profiles.Count != 0)
            {
                ProfileComboBox.SelectedIndex = 0;
            }
        }
        
        private void UpdateStatusBar(AppConfig config)
        {
            GamePathText.Text = $"Game: {config.GameInstallPath}";
            StatusText.Text = "Ready";
        }
        
        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem is ModProfile profile)
            {
                _ = _profileManager?.SetActiveProfileAsync(profile.Id);
                _ = _configManager?.SetActiveProfileAsync(profile.Id);
                StatusText.Text = $"Active profile: {profile.Name}";
            }
        }
        
        private async void NewProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_profileManager == null) return;
                
                var dialog = new NewProfileDialog();
                if (dialog.ShowDialog() == true)
                {
                    var profile = await _profileManager.CreateProfileAsync(dialog.ProfileName, dialog.ProfileDescription);
                    UpdateProfileComboBox();
                    ProfileComboBox.SelectedItem = profile;
                    StatusText.Text = $"Created profile: {profile.Name}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating profile: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Failed to create profile";
            }
        }
        
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_configManager == null) return;
            
            var settingsWindow = new SettingsWindow(_configManager);
            if (settingsWindow.ShowDialog() == true)
            {
                var config = _configManager.GetConfig();
                UpdateStatusBar(config);
            }
        }
        
        private void ProfileManagementButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToProfileManagement();
        }
        
        private void ModLibraryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_warehouseManager == null || _profileManager == null) return;
            
            ContentFrame.Navigate(new ModLibraryPage(_warehouseManager, _profileManager));
            StatusText.Text = "Mod Library";
        }
        
        private void WarehouseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_warehouseManager == null) return;
            
            ContentFrame.Navigate(new WarehousePage(_warehouseManager));
            StatusText.Text = "Mod Warehouse";
        }
        
        private void ActiveModsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager == null || _warehouseManager == null) return;
            
            var activeProfile = _profileManager.GetActiveProfile();
            if (activeProfile != null)
            {
                ContentFrame.Navigate(new ActiveModsPage(activeProfile, _warehouseManager));
                StatusText.Text = $"Active modifications for {activeProfile.Name}";
            }
            else
            {
                MessageBox.Show("No active profile selected.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private void GameFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_configManager == null) return;
            
            var config = _configManager.GetConfig();
            ContentFrame.Navigate(new GameFilesBrowser(config.GameInstallPath));
            StatusText.Text = "Game Files Browser";
        }
        
        private async void ApplyProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_profileManager == null || _configManager == null || _modApplicator == null) return;
                
                var activeProfile = _profileManager.GetActiveProfile();
                if (activeProfile == null)
                {
                    MessageBox.Show("No active profile selected.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                var config = _configManager.GetConfig();
                if (config.ConfirmBeforeApply)
                {
                    var result = MessageBox.Show(
                        $"Apply all modifications from profile '{activeProfile.Name}'?\n\nThis will modify {activeProfile.FileModifications.Count} file(s).",
                        "Confirm Apply", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result != MessageBoxResult.Yes)
                        return;
                }
                
                StatusText.Text = "Applying profile...";
                var (success, failed) = await _modApplicator.ApplyProfileAsync(activeProfile, config.AutoBackup);
                await _profileManager.SaveProfileAsync(activeProfile);
                
                StatusText.Text = $"Applied: {success} succeeded, {failed} failed";
                MessageBox.Show($"Profile applied:\n{success} modifications succeeded\n{failed} modifications failed",
                    "Apply Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying profile: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Failed to apply profile";
            }
        }
        
        private async void RevertProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_profileManager == null || _modApplicator == null) return;
                
                var activeProfile = _profileManager.GetActiveProfile();
                if (activeProfile == null)
                {
                    MessageBox.Show("No active profile selected.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                var result = MessageBox.Show(
                    $"Revert all modifications from profile '{activeProfile.Name}'?\n\nThis will restore original files.",
                    "Confirm Revert", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result != MessageBoxResult.Yes)
                    return;
                
                StatusText.Text = "Reverting profile...";
                var (success, failed) = await _modApplicator.RevertProfileAsync(activeProfile);
                await _profileManager.SaveProfileAsync(activeProfile);
                
                StatusText.Text = $"Reverted: {success} succeeded, {failed} failed";
                MessageBox.Show($"Profile reverted:\n{success} modifications succeeded\n{failed} modifications failed",
                    "Revert Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reverting profile: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Failed to revert profile";
            }
        }
        
        private void NavigateToProfileManagement()
        {
            if (_profileManager == null || _warehouseManager == null) return;
            
            ContentFrame.Navigate(new ProfileManagementPage(_profileManager, _warehouseManager));
            StatusText.Text = "Profile Management";
        }
    }
}
