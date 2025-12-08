namespace XvTHydrospanner.Models
{
    /// <summary>
    /// Application configuration and settings
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Path to the Star Wars XvT installation
        /// </summary>
        public string GameInstallPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to the Mod Warehouse directory
        /// </summary>
        public string WarehousePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to store profile data
        /// </summary>
        public string ProfilesPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to store backup files
        /// </summary>
        public string BackupPath { get; set; } = string.Empty;
        
        /// <summary>
        /// ID of the currently active profile
        /// </summary>
        public string? ActiveProfileId { get; set; }
        
        /// <summary>
        /// Whether to create backups before applying mods
        /// </summary>
        public bool AutoBackup { get; set; } = true;
        
        /// <summary>
        /// Whether to confirm before applying profile changes
        /// </summary>
        public bool ConfirmBeforeApply { get; set; } = true;
        
        /// <summary>
        /// Maximum number of backup versions to keep per file
        /// </summary>
        public int MaxBackupVersions { get; set; } = 5;
        
        /// <summary>
        /// Theme preference
        /// </summary>
        public string Theme { get; set; } = "Dark";
        
        /// <summary>
        /// Last directory used for importing files
        /// </summary>
        public string? LastImportDirectory { get; set; }
        
        /// <summary>
        /// GitHub repository owner for remote mods (optional)
        /// </summary>
        public string? RemoteRepositoryOwner { get; set; }
        
        /// <summary>
        /// GitHub repository name for remote mods (optional)
        /// </summary>
        public string? RemoteRepositoryName { get; set; }
        
        /// <summary>
        /// GitHub repository branch for remote mods (optional)
        /// </summary>
        public string? RemoteRepositoryBranch { get; set; }
    }
}
