using System;

namespace XvTHydrospanner.Models
{
    /// <summary>
    /// Metadata stored alongside the base game backup to describe its contents
    /// </summary>
    public class BackupMetadata
    {
        /// <summary>
        /// Date and time the backup was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Path to the original game directory that was backed up
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Path where the backup was stored
        /// </summary>
        public string BackupPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Total number of files in the backup
        /// </summary>
        public int TotalFiles { get; set; }
        
        /// <summary>
        /// Total size of the backup in bytes
        /// </summary>
        public long TotalSizeBytes { get; set; }
        
        /// <summary>
        /// App version when backup was created
        /// </summary>
        public string AppVersion { get; set; } = string.Empty;
    }
}
