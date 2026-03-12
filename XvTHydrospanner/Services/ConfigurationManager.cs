using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XvTHydrospanner.Models;

namespace XvTHydrospanner.Services
{
    /// <summary>
    /// Manages application configuration
    /// </summary>
    public class ConfigurationManager
    {
        private readonly string _configPath;
        private AppConfig _config;
        
        public event EventHandler<AppConfig>? ConfigurationChanged;
        
        public ConfigurationManager(string configPath)
        {
            _configPath = configPath;
            _config = new AppConfig();
        }
        
        /// <summary>
        /// Load configuration from disk
        /// </summary>
        public async Task<AppConfig> LoadConfigAsync()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_configPath);
                    _config = JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading config: {ex.Message}");
                    _config = new AppConfig();
                }
            }
            else
            {
                // Create default configuration
                _config = CreateDefaultConfig();
                await SaveConfigAsync();
            }
            
            return _config;
        }
        
        /// <summary>
        /// Save configuration to disk
        /// </summary>
        public async Task SaveConfigAsync()
        {
            var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
            var directory = Path.GetDirectoryName(_configPath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(_configPath, json);
            
            ConfigurationChanged?.Invoke(this, _config);
        }
        
        /// <summary>
        /// Get current configuration
        /// </summary>
        public AppConfig GetConfig()
        {
            return _config;
        }
        
        /// <summary>
        /// Update configuration
        /// </summary>
        public async Task UpdateConfigAsync(AppConfig config)
        {
            _config = config;
            await SaveConfigAsync();
        }
        
        /// <summary>
        /// Set game install path
        /// </summary>
        public async Task SetGameInstallPathAsync(string path)
        {
            _config.GameInstallPath = path;
            await SaveConfigAsync();
        }
        
        /// <summary>
        /// Set warehouse path
        /// </summary>
        public async Task SetWarehousePathAsync(string path)
        {
            _config.WarehousePath = path;
            Directory.CreateDirectory(path);
            await SaveConfigAsync();
        }
        
        /// <summary>
        /// Set profiles path
        /// </summary>
        public async Task SetProfilesPathAsync(string path)
        {
            _config.ProfilesPath = path;
            Directory.CreateDirectory(path);
            await SaveConfigAsync();
        }
        
        /// <summary>
        /// Set backup path
        /// </summary>
        public async Task SetBackupPathAsync(string path)
        {
            _config.BackupPath = path;
            Directory.CreateDirectory(path);
            await SaveConfigAsync();
        }
        
        /// <summary>
        /// Mark the base game backup as complete and record metadata
        /// </summary>
        public async Task SetBaseGameBackupCompletedAsync(string backupPath, DateTime createdDate)
        {
            _config.BaseGameBackupPath = backupPath;
            _config.BaseGameBackupExists = true;
            _config.BaseGameBackupCreatedDate = createdDate;
            await SaveConfigAsync();
        }
        
        /// <summary>
        /// Set the Base Game Install profile ID
        /// </summary>
        public async Task SetBaseGameProfileIdAsync(string profileId)
        {
            _config.BaseGameProfileId = profileId;
            await SaveConfigAsync();
        }
        
        /// <summary>
        /// Mark first-run setup as complete
        /// </summary>
        public async Task SetFirstRunCompletedAsync()
        {
            _config.FirstRunCompleted = true;
            await SaveConfigAsync();
        }
        
        /// <summary>
        /// Set active profile
        /// </summary>
        public async Task SetActiveProfileAsync(string? profileId)
        {
            _config.ActiveProfileId = profileId;
            await SaveConfigAsync();
        }
        
        /// <summary>
        /// Create default configuration
        /// </summary>
        private AppConfig CreateDefaultConfig()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XvTHydrospanner"
            );
            
            return new AppConfig
            {
                GameInstallPath = string.Empty,
                WarehousePath = Path.Combine(appDataPath, "Warehouse"),
                ProfilesPath = Path.Combine(appDataPath, "Profiles"),
                BackupPath = Path.Combine(appDataPath, "Backups"),
                BaseGameBackupPath = Path.Combine(appDataPath, "BaseGameBackup"),
                AutoBackup = true,
                ConfirmBeforeApply = true,
                MaxBackupVersions = 5,
                Theme = "Dark",
                FirstRunCompleted = false
            };
        }
        
        /// <summary>
        /// Validate that storage paths are configured.
        /// Note: GameInstallPath is intentionally excluded — it is established
        /// during the base game backup setup flow, not required at startup.
        /// </summary>
        public (bool isValid, List<string> errors) ValidateConfig()
        {
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(_config.WarehousePath))
                errors.Add("Warehouse path is not set");
            
            if (string.IsNullOrWhiteSpace(_config.ProfilesPath))
                errors.Add("Profiles path is not set");
            
            if (string.IsNullOrWhiteSpace(_config.BackupPath))
                errors.Add("Backup path is not set");
            
            if (string.IsNullOrWhiteSpace(_config.BaseGameBackupPath))
                errors.Add("Base game backup path is not set");
            
            return (errors.Count == 0, errors);
        }
    }
}
