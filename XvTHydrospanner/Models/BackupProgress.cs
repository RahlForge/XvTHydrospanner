namespace XvTHydrospanner.Models
{
    /// <summary>
    /// Represents the progress of a backup or restore operation
    /// </summary>
    public class BackupProgress
    {
        /// <summary>
        /// Number of files processed so far
        /// </summary>
        public int FilesProcessed { get; set; }
        
        /// <summary>
        /// Total number of files to process
        /// </summary>
        public int TotalFiles { get; set; }
        
        /// <summary>
        /// The file currently being processed
        /// </summary>
        public string CurrentFile { get; set; } = string.Empty;
        
        /// <summary>
        /// Human-readable status message
        /// </summary>
        public string StatusMessage { get; set; } = string.Empty;
        
        /// <summary>
        /// Progress percentage (0–100)
        /// </summary>
        public double Percentage => TotalFiles > 0 ? (double)FilesProcessed / TotalFiles * 100 : 0;
        
        /// <summary>
        /// Whether the operation has completed
        /// </summary>
        public bool IsComplete { get; set; }
        
        /// <summary>
        /// Whether the operation failed
        /// </summary>
        public bool IsFailed { get; set; }
        
        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
