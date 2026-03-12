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
        private ModdedInstallsManager? _moddedInstallsManager;
        private ModApplicator? _modApplicator;
        private BaseGameBackupManager? _backupManager;
        
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
                
                // Ensure all storage directories are configured and exist.
                // GameInstallPath is NOT validated here — it is established
                // during the base game backup setup flow below.
                var storageReady = await InitializeStoragePathsAsync(config);
                if (!storageReady)
                {
                    Application.Current.Shutdown();
                    return;
                }
                config = _configManager.GetConfig();
                
                // Initialize the backup manager with the configured path
                _backupManager = new BaseGameBackupManager(config.BaseGameBackupPath);
                
                // Initialize services with loaded config.
                // ModApplicator is intentionally NOT initialized here — it requires a
                // valid GameInstallPath which is only confirmed after backup setup.
                _profileManager = new ProfileManager(config.ProfilesPath);
                _warehouseManager = new WarehouseManager(config.WarehousePath);
                _remoteWarehouseManager = new RemoteWarehouseManager(
                    _warehouseManager, 
                    config.RemoteRepositoryOwner, 
                    config.RemoteRepositoryName, 
                    config.RemoteRepositoryBranch);
                _moddedInstallsManager = new ModdedInstallsManager(
                    config.RemoteRepositoryOwner,
                    config.ModdedInstallsRepositoryName,
                    config.GitHubToken);
                
                // Load data
                await _warehouseManager.LoadCatalogAsync();
                await _profileManager.LoadAllProfilesAsync();
                
                // Require base game backup before proceeding. This runs on first launch
                // and any time the backup is missing (e.g. user deleted it).
                if (!config.FirstRunCompleted || !_backupManager.BackupExists())
                {
                    var completed = await RunFirstTimeBackupSetupAsync(config);
                    if (!completed)
                    {
                        Application.Current.Shutdown();
                        return;
                    }
                    config = _configManager.GetConfig();
                }
                else
                {
                    // Returning user: backup is healthy — initialize ModApplicator now.
                    InitializeModApplicator(config);
                    
                    // Ensure the Base Game Install profile exists for returning users
                    await EnsureBaseGameProfileExistsAsync(config);
                }
                
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
        
        /// <summary>
        /// Initializes (or re-initializes) the ModApplicator with the current game install path.
        /// Must only be called after a valid GameInstallPath is confirmed via backup setup.
        /// </summary>
        private void InitializeModApplicator(AppConfig config)
        {
            _modApplicator = new ModApplicator(config.GameInstallPath, config.BackupPath, _warehouseManager);
            _modApplicator.ProgressMessage += (s, message) =>
            {
                Dispatcher.Invoke(() => StatusText.Text = message);
            };
        }

        /// <summary>
        /// Ensures all storage directories (Warehouse, Profiles, Backup, BaseGameBackup) exist.
        /// If any path is missing or unconfigured, shows a dialog for the user to choose paths.
        /// Returns true if all paths are ready, false if the user cancelled.
        /// </summary>
        private async System.Threading.Tasks.Task<bool> InitializeStoragePathsAsync(AppConfig config)
        {
            if (_configManager == null) return false;

            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XvTHydrospanner");

            // Build defaults for any path that is empty
            var defaultWarehouse     = string.IsNullOrWhiteSpace(config.WarehousePath)
                                        ? Path.Combine(appDataPath, "Warehouse") : config.WarehousePath;
            var defaultProfiles      = string.IsNullOrWhiteSpace(config.ProfilesPath)
                                        ? Path.Combine(appDataPath, "Profiles") : config.ProfilesPath;
            var defaultBackup        = string.IsNullOrWhiteSpace(config.BackupPath)
                                        ? Path.Combine(appDataPath, "Backups") : config.BackupPath;
            var defaultBaseGameBackup = string.IsNullOrWhiteSpace(config.BaseGameBackupPath)
                                        ? Path.Combine(appDataPath, "BaseGameBackup") : config.BaseGameBackupPath;

            // Check whether any configured path is missing from disk
            bool anyMissing = !Directory.Exists(defaultWarehouse)
                           || !Directory.Exists(defaultProfiles)
                           || !Directory.Exists(defaultBackup)
                           || !Directory.Exists(defaultBaseGameBackup);

            if (anyMissing)
            {
                var dialog = new ConfigureStoragePathsDialog(
                    defaultWarehouse,
                    defaultProfiles,
                    defaultBackup,
                    defaultBaseGameBackup);
                dialog.Owner = this;

                if (dialog.ShowDialog() != true)
                    return false;

                // Persist the chosen paths
                await _configManager.SetWarehousePathAsync(dialog.WarehousePath);
                await _configManager.SetProfilesPathAsync(dialog.ProfilesPath);
                await _configManager.SetBackupPathAsync(dialog.BackupPath);

                // BaseGameBackupPath doesn't have a dedicated setter — update via config directly
                var updatedConfig = _configManager.GetConfig();
                updatedConfig.BaseGameBackupPath = dialog.BaseGameBackupPath;
                await _configManager.UpdateConfigAsync(updatedConfig);
            }
            else
            {
                // All paths exist — ensure they're in config in case defaults were used
                bool configChanged = false;
                var c = _configManager.GetConfig();

                if (string.IsNullOrWhiteSpace(c.WarehousePath))     { c.WarehousePath     = defaultWarehouse;      configChanged = true; }
                if (string.IsNullOrWhiteSpace(c.ProfilesPath))      { c.ProfilesPath      = defaultProfiles;       configChanged = true; }
                if (string.IsNullOrWhiteSpace(c.BackupPath))        { c.BackupPath        = defaultBackup;         configChanged = true; }
                if (string.IsNullOrWhiteSpace(c.BaseGameBackupPath)){ c.BaseGameBackupPath = defaultBaseGameBackup; configChanged = true; }

                if (configChanged)
                    await _configManager.UpdateConfigAsync(c);
            }

            return true;
        }

        /// <summary>
        /// Runs the mandatory first-time backup setup flow.
        /// Returns true if completed successfully, false if the user chose to exit.
        /// Also called when the backup is missing (e.g. user deleted the folder).
        /// </summary>
        private async System.Threading.Tasks.Task<bool> RunFirstTimeBackupSetupAsync(AppConfig config)
        {
            if (_profileManager == null || _configManager == null || _backupManager == null)
                return false;

            while (true)
            {
                // Show the backup required dialog — lets user select (or confirm) game path
                var requiredDialog = new BackupRequiredDialog();
                requiredDialog.Owner = this;

                if (requiredDialog.ShowDialog() != true || !requiredDialog.UserConfirmedBackup)
                    return false;

                // Persist the confirmed game path to config unconditionally
                var selectedPath = requiredDialog.SelectedGamePath!;
                await _configManager.SetGameInstallPathAsync(selectedPath);
                config = _configManager.GetConfig();

                // Run the backup using the confirmed path
                var progressDialog = new BackupProgressDialog(_backupManager, selectedPath);
                progressDialog.Owner = this;
                var backupResult = progressDialog.ShowDialog();

                if (backupResult == true)
                    break; // Backup completed successfully

                // Backup was cancelled or failed — ask user: retry or exit?
                var choice = MessageBox.Show(
                    "The base game backup was not completed.\n\n" +
                    "XvT Hydrospanner cannot run without a base game backup.\n\n" +
                    "Would you like to try again?",
                    "Backup Incomplete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (choice != MessageBoxResult.Yes)
                    return false;
                // Loop back to show the dialog again
            }

            // Backup completed — initialize ModApplicator with the confirmed path
            InitializeModApplicator(config);

            // Record backup completion
            await _configManager.SetBaseGameBackupCompletedAsync(config.BaseGameBackupPath, DateTime.Now);

            // Create the immutable Base Game Install profile
            var baseProfile = await _profileManager.CreateBaseGameProfileAsync();
            await _profileManager.SetActiveProfileAsync(baseProfile.Id);
            await _configManager.SetActiveProfileAsync(baseProfile.Id);
            await _configManager.SetBaseGameProfileIdAsync(baseProfile.Id);
            await _configManager.SetFirstRunCompletedAsync();

            StatusText.Text = "Base Game Install profile created";
            return true;
        }
        
        /// <summary>
        /// For existing users: ensure the Base Game Install profile exists (migration path).
        /// </summary>
        private async System.Threading.Tasks.Task EnsureBaseGameProfileExistsAsync(AppConfig config)
        {
            if (_profileManager == null || _configManager == null) return;
            
            var profiles = _profileManager.GetAllProfiles();
            
            // If we already have a proper immutable base profile, nothing to do
            if (profiles.Exists(p => p.IsBaseGameInstall && p.IsImmutable))
                return;
            
            // Migrate legacy read-only "Base Game Install" profile to the new immutable form
            var legacy = profiles.Find(p => p.Name == "Base Game Install" && p.IsReadOnly);
            if (legacy != null)
            {
                legacy.IsBaseGameInstall = true;
                legacy.IsImmutable = true;
                await _profileManager.SaveProfileAsync(legacy);
                await _configManager.SetBaseGameProfileIdAsync(legacy.Id);
                return;
            }
            
            // No profiles at all — create fresh
            if (profiles.Count == 0)
            {
                var baseProfile = await _profileManager.CreateBaseGameProfileAsync();
                await _profileManager.SetActiveProfileAsync(baseProfile.Id);
                await _configManager.SetActiveProfileAsync(baseProfile.Id);
                await _configManager.SetBaseGameProfileIdAsync(baseProfile.Id);
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
            
            var settingsWindow = new SettingsWindow(_configManager, _backupManager);
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
            
            ContentFrame.Navigate(new RemoteModsPage(_remoteWarehouseManager, _configManager));
            StatusText.Text = "Remote Mod Library";
        }
        
        private void ModdedInstallsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_moddedInstallsManager == null) return;
            
            ContentFrame.Navigate(new ModdedInstallsPage(_moddedInstallsManager));
            StatusText.Text = "Modded Installs";
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
                    
                    // Check if profile is immutable (Base Game Install)
                    if (profileToApply.IsImmutable || profileToApply.IsReadOnly)
                    {
                        MessageBox.Show(
                            $"Cannot apply profile '{profileToApply.Name}'.\n\n" +
                            "This is the immutable Base Game Install profile. It represents the clean game state.\n\n" +
                            "To apply mods:\n" +
                            "1. Clone this profile from Profile Management\n" +
                            "2. Add mods to the clone\n" +
                            "3. Apply the cloned profile",
                            "Immutable Profile",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
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
                
                // Step 1: Revert regular files
                var (success, failed) = await _modApplicator.RevertProfileAsync(activeProfile);
                
                // Step 2: CRITICAL - Restore base LST files to clean state
                StatusText.Text = "Restoring base LST files...";
                await _modApplicator.RestoreAllBaseLstFilesAsync();
                
                await _profileManager.SaveProfileAsync(activeProfile);
                
                StatusText.Text = $"Reverted: {success} succeeded, {failed} failed";
                MessageBox.Show($"Profile reverted:\n{success} modifications succeeded\n{failed} modifications failed\n\nBase LST files have been restored to original state.",
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
