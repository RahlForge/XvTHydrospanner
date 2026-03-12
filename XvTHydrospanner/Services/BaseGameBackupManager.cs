using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XvTHydrospanner.Models;

namespace XvTHydrospanner.Services
{
    /// <summary>
    /// Manages creating, validating, and restoring the base game backup
    /// </summary>
    public class BaseGameBackupManager
    {
        private readonly string _backupPath;
        private const string MetadataFileName = "backup_metadata.json";
        
        /// <summary>
        /// Raised to report backup/restore progress
        /// </summary>
        public event EventHandler<BackupProgress>? ProgressChanged;
        
        public BaseGameBackupManager(string backupPath)
        {
            _backupPath = backupPath;
        }
        
        /// <summary>
        /// Returns true if a backup already exists at the configured path
        /// </summary>
        public bool BackupExists()
        {
            return Directory.Exists(_backupPath) &&
                   File.Exists(Path.Combine(_backupPath, MetadataFileName));
        }
        
        /// <summary>
        /// Load backup metadata, or null if not present
        /// </summary>
        public async Task<BackupMetadata?> LoadMetadataAsync()
        {
            var metaPath = Path.Combine(_backupPath, MetadataFileName);
            if (!File.Exists(metaPath)) return null;
            
            try
            {
                var json = await File.ReadAllTextAsync(metaPath);
                return JsonConvert.DeserializeObject<BackupMetadata>(json);
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Create a full backup of the game directory.
        /// Deletes any existing backup first (caller is responsible for confirmation).
        /// Reports progress via ProgressChanged.
        /// </summary>
        public async Task CreateBackupAsync(string sourceGamePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceGamePath))
                throw new ArgumentException("Source game path must not be empty.", nameof(sourceGamePath));
            
            if (string.IsNullOrWhiteSpace(_backupPath))
                throw new InvalidOperationException(
                    "Backup path is not configured. Please check your application settings.");
            
            if (!Directory.Exists(sourceGamePath))
                throw new DirectoryNotFoundException($"Game directory not found: {sourceGamePath}");
            
            // Delete old backup if it exists
            if (Directory.Exists(_backupPath))
            {
                ReportProgress(new BackupProgress { StatusMessage = "Removing old backup..." });
                await Task.Run(() => Directory.Delete(_backupPath, true), cancellationToken);
            }
            
            Directory.CreateDirectory(_backupPath);
            
            // Enumerate all source files
            var allFiles = Directory.GetFiles(sourceGamePath, "*", SearchOption.AllDirectories);
            int total = allFiles.Length;
            int processed = 0;
            long totalSize = 0;
            
            ReportProgress(new BackupProgress
            {
                TotalFiles = total,
                FilesProcessed = 0,
                StatusMessage = $"Starting backup of {total} files..."
            });
            
            foreach (var sourceFile in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var relativePath = Path.GetRelativePath(sourceGamePath, sourceFile);
                var destFile = Path.Combine(_backupPath, relativePath);
                var destDir = Path.GetDirectoryName(destFile)!;
                
                Directory.CreateDirectory(destDir);
                await Task.Run(() => File.Copy(sourceFile, destFile, overwrite: true), cancellationToken);
                
                totalSize += new FileInfo(sourceFile).Length;
                processed++;
                
                ReportProgress(new BackupProgress
                {
                    TotalFiles = total,
                    FilesProcessed = processed,
                    CurrentFile = relativePath,
                    StatusMessage = $"Copying: {relativePath}"
                });
            }
            
            // Write metadata
            var metadata = new BackupMetadata
            {
                CreatedDate = DateTime.Now,
                SourcePath = sourceGamePath,
                BackupPath = _backupPath,
                TotalFiles = total,
                TotalSizeBytes = totalSize,
                AppVersion = GetAppVersion()
            };
            
            var metaJson = JsonConvert.SerializeObject(metadata, Formatting.Indented);
            await File.WriteAllTextAsync(Path.Combine(_backupPath, MetadataFileName), metaJson);
            
            ReportProgress(new BackupProgress
            {
                TotalFiles = total,
                FilesProcessed = total,
                StatusMessage = "Backup complete.",
                IsComplete = true
            });
        }
        
        /// <summary>
        /// Restore the backup to the specified game directory.
        /// Overwrites all existing game files with the backup copies.
        /// Reports progress via ProgressChanged.
        /// </summary>
        public async Task RestoreBackupAsync(string targetGamePath, CancellationToken cancellationToken = default)
        {
            if (!BackupExists())
                throw new InvalidOperationException("No base game backup exists to restore from.");
            
            if (string.IsNullOrWhiteSpace(targetGamePath))
                throw new ArgumentException("Target game path must not be empty.", nameof(targetGamePath));
            
            var allFiles = Directory.GetFiles(_backupPath, "*", SearchOption.AllDirectories);
            // Exclude the metadata file from the restore
            var filesToRestore = Array.FindAll(allFiles,
                f => !Path.GetFileName(f).Equals(MetadataFileName, StringComparison.OrdinalIgnoreCase));
            
            int total = filesToRestore.Length;
            int processed = 0;
            
            ReportProgress(new BackupProgress
            {
                TotalFiles = total,
                FilesProcessed = 0,
                StatusMessage = $"Restoring {total} files..."
            });
            
            foreach (var backupFile in filesToRestore)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var relativePath = Path.GetRelativePath(_backupPath, backupFile);
                var destFile = Path.Combine(targetGamePath, relativePath);
                var destDir = Path.GetDirectoryName(destFile)!;
                
                Directory.CreateDirectory(destDir);
                await Task.Run(() => File.Copy(backupFile, destFile, overwrite: true), cancellationToken);
                
                processed++;
                
                ReportProgress(new BackupProgress
                {
                    TotalFiles = total,
                    FilesProcessed = processed,
                    CurrentFile = relativePath,
                    StatusMessage = $"Restoring: {relativePath}"
                });
            }
            
            ReportProgress(new BackupProgress
            {
                TotalFiles = total,
                FilesProcessed = total,
                StatusMessage = "Restore complete.",
                IsComplete = true
            });
        }
        
        /// <summary>
        /// Validate the backup by checking all metadata-recorded files still exist
        /// </summary>
        public async Task<(bool isValid, string message)> ValidateBackupAsync()
        {
            if (!BackupExists())
                return (false, "No backup found.");
            
            var metadata = await LoadMetadataAsync();
            if (metadata == null)
                return (false, "Backup metadata is missing or corrupt.");
            
            var actualFiles = Directory.GetFiles(_backupPath, "*", SearchOption.AllDirectories);
            // Subtract 1 for the metadata file itself
            int actualCount = actualFiles.Length - 1;
            
            if (actualCount != metadata.TotalFiles)
                return (false, $"File count mismatch: expected {metadata.TotalFiles}, found {actualCount}.");
            
            return (true, $"Backup is valid ({metadata.TotalFiles} files, created {metadata.CreatedDate:g}).");
        }
        
        private void ReportProgress(BackupProgress progress)
        {
            ProgressChanged?.Invoke(this, progress);
        }
        
        private static string GetAppVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly()
                       .GetName().Version?.ToString() ?? "1.0.0";
        }
    }
}
