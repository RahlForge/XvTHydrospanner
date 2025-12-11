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
        private RemoteWarehouseManager? _remoteWarehouseManager;
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
                _remoteWarehouseManager = new RemoteWarehouseManager(
                    _warehouseManager, 
                    config.RemoteRepositoryOwner, 
                    config.RemoteRepositoryName, 
                    config.RemoteRepositoryBranch);
                _modApplicator = new ModApplicator(config.GameInstallPath, config.BackupPath, _warehouseManager);
                
                // Subscribe to progress messages
                _modApplicator.ProgressMessage += (sender, message) => 
                {
                    Dispatcher.Invoke(() => StatusText.Text = message);
                };
                
                // Load data
                await _warehouseManager.LoadCatalogAsync();
                await _profileManager.LoadAllProfilesAsync();
                
                // Update UI
                UpdateActiveProfileDisplay();
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
        
        public void UpdateActiveProfileDisplay()
        {
            if (_profileManager == null) return;
            
            var activeProfile = _profileManager.GetActiveProfile();
            if (activeProfile != null)
            {
                ActiveProfileText.Text = $"Active Profile: {activeProfile.Name}";
            }
            else
            {
                ActiveProfileText.Text = "Active Profile: None";
            }
        }
        
        private void UpdateStatusBar(AppConfig config)
        {
            GamePathText.Text = $"Game: {config.GameInstallPath}";
            StatusText.Text = "Ready";
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
            
            ContentFrame.Navigate(new ModLibraryPage(_warehouseManager, _profileManager, _modApplicator));
            StatusText.Text = "Mod Library";
        }
        
        private void WarehouseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_warehouseManager == null) return;
            
            ContentFrame.Navigate(new WarehousePage(_warehouseManager, _remoteWarehouseManager, _configManager));
            StatusText.Text = "Mod Warehouse";
        }
        
        private void RemoteModsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_remoteWarehouseManager == null) return;
            
            ContentFrame.Navigate(new RemoteModsPage(_remoteWarehouseManager));
            StatusText.Text = "Remote Mod Library";
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
                
                ModProfile? profileToApply = null;
                var oldProfile = _profileManager.GetActiveProfile();
                
                // If on Profile Management page, get selected profile from there
                if (ContentFrame.Content is ProfileManagementPage profilePage)
                {
                    profileToApply = profilePage.GetSelectedProfile();
                    if (profileToApply == null)
                    {
                        MessageBox.Show("Please select a profile to apply.", "Info", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
                else
                {
                    // Otherwise use the current active profile
                    profileToApply = oldProfile;
                    if (profileToApply == null)
                    {
                        MessageBox.Show("No profile selected. Please go to Profile Management.", "Info", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
                
                var config = _configManager.GetConfig();
                
                // Check if we're switching profiles
                var isSwitchingProfiles = oldProfile != null && oldProfile.Id != profileToApply.Id;
                
                // Confirm action
                if (config.ConfirmBeforeApply || isSwitchingProfiles)
                {
                    var message = isSwitchingProfiles
                        ? $"Switch from profile '{oldProfile?.Name}' to '{profileToApply.Name}' and apply?\n\n" +
                          "This will:\n" +
                          "1. Set '{profileToApply.Name}' as the active profile\n" +
                          "2. Revert previous profile's regular files\n" +
                          "3. Restore base LST files to clean state\n" +
                          "4. Apply new profile's modifications\n\n" +
                          "This ensures LST files are rebuilt correctly for multiplayer compatibility."
                        : $"Apply profile '{profileToApply.Name}'?\n\n" +
                          $"This will modify {profileToApply.FileModifications.Count} file(s).";
                    
                    var result = MessageBox.Show(message, "Confirm Apply", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result != MessageBoxResult.Yes)
                        return;
                }
                
                StatusText.Text = isSwitchingProfiles ? "Switching and applying profile..." : "Applying profile...";
                
                int success, failed;
                if (isSwitchingProfiles)
                {
                    // Use SwitchProfileAsync for proper LST handling
                    (success, failed) = await _modApplicator.SwitchProfileAsync(oldProfile, profileToApply, config.AutoBackup);
                }
                else
                {
                    // Just apply the profile
                    (success, failed) = await _modApplicator.ApplyProfileAsync(profileToApply, config.AutoBackup);
                }
                
                // Set as active profile
                await _profileManager.SetActiveProfileAsync(profileToApply.Id);
                await _configManager.SetActiveProfileAsync(profileToApply.Id);
                await _profileManager.SaveProfileAsync(profileToApply);
                if (oldProfile != null && isSwitchingProfiles) await _profileManager.SaveProfileAsync(oldProfile);
                
                // Update the display
                UpdateActiveProfileDisplay();
                
                // Refresh Profile Management page if it's currently visible
                if (ContentFrame.Content is ProfileManagementPage profileMgmtPage)
                {
                    profileMgmtPage.LoadProfiles();
                }
                
                StatusText.Text = $"Applied: {success} succeeded, {failed} failed";
                MessageBox.Show($"Profile '{profileToApply.Name}' applied:\n{success} modifications succeeded\n{failed} modifications failed",
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
