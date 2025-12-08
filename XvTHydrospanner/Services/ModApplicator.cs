using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XvTHydrospanner.Models;

namespace XvTHydrospanner.Services
{
    /// <summary>
    /// Handles applying and reverting file modifications
    /// </summary>
    public class ModApplicator
    {
        private readonly string _gameInstallPath;
        private readonly string _backupPath;
        private readonly WarehouseManager _warehouseManager;
        
        public event EventHandler<string>? ModificationApplied;
        public event EventHandler<string>? ModificationReverted;
        public event EventHandler<string>? BackupCreated;
        
        public ModApplicator(string gameInstallPath, string backupPath, WarehouseManager warehouseManager)
        {
            _gameInstallPath = gameInstallPath;
            _backupPath = backupPath;
            _warehouseManager = warehouseManager;
            Directory.CreateDirectory(_backupPath);
        }
        
        /// <summary>
        /// Apply a single file modification
        /// </summary>
        public async Task<bool> ApplyModificationAsync(FileModification modification, bool createBackup = true)
        {
            try
            {
                var warehouseFile = _warehouseManager.GetFile(modification.WarehouseFileId);
                if (warehouseFile == null)
                    throw new InvalidOperationException($"Warehouse file {modification.WarehouseFileId} not found");
                
                var targetPath = Path.Combine(_gameInstallPath, modification.RelativeGamePath);
                var targetDir = Path.GetDirectoryName(targetPath);
                
                if (targetDir != null)
                {
                    Directory.CreateDirectory(targetDir);
                }
                
                // Create backup if requested and file exists
                if (createBackup && File.Exists(targetPath))
                {
                    modification.BackupPath = await CreateBackupAsync(targetPath, modification.Id);
                    BackupCreated?.Invoke(this, modification.BackupPath);
                }
                
                // Copy warehouse file to game location
                await Task.Run(() => File.Copy(warehouseFile.StoragePath, targetPath, true));
                
                modification.IsApplied = true;
                ModificationApplied?.Invoke(this, modification.RelativeGamePath);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying modification: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Revert a single file modification
        /// </summary>
        public async Task<bool> RevertModificationAsync(FileModification modification)
        {
            try
            {
                var targetPath = Path.Combine(_gameInstallPath, modification.RelativeGamePath);
                
                if (modification.BackupPath != null && File.Exists(modification.BackupPath))
                {
                    // Restore from backup
                    await Task.Run(() => File.Copy(modification.BackupPath, targetPath, true));
                    modification.IsApplied = false;
                    ModificationReverted?.Invoke(this, modification.RelativeGamePath);
                    return true;
                }
                else if (File.Exists(targetPath))
                {
                    // No backup exists, just delete the modified file
                    await Task.Run(() => File.Delete(targetPath));
                    modification.IsApplied = false;
                    ModificationReverted?.Invoke(this, modification.RelativeGamePath);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reverting modification: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Apply all modifications in a profile
        /// </summary>
        public async Task<(int success, int failed)> ApplyProfileAsync(ModProfile profile, bool createBackup = true)
        {
            int successCount = 0;
            int failedCount = 0;
            
            foreach (var modification in profile.FileModifications)
            {
                var result = await ApplyModificationAsync(modification, createBackup);
                if (result)
                    successCount++;
                else
                    failedCount++;
            }
            
            return (successCount, failedCount);
        }
        
        /// <summary>
        /// Revert all modifications in a profile
        /// </summary>
        public async Task<(int success, int failed)> RevertProfileAsync(ModProfile profile)
        {
            int successCount = 0;
            int failedCount = 0;
            
            foreach (var modification in profile.FileModifications.Where(m => m.IsApplied))
            {
                var result = await RevertModificationAsync(modification);
                if (result)
                    successCount++;
                else
                    failedCount++;
            }
            
            return (successCount, failedCount);
        }
        
        /// <summary>
        /// Create a backup of a file
        /// </summary>
        private async Task<string> CreateBackupAsync(string filePath, string modificationId)
        {
            var fileName = Path.GetFileName(filePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"{modificationId}_{timestamp}_{fileName}";
            var backupFilePath = Path.Combine(_backupPath, backupFileName);
            
            await Task.Run(() => File.Copy(filePath, backupFilePath, true));
            
            return backupFilePath;
        }
        
        /// <summary>
        /// Clean up old backups, keeping only the most recent N versions
        /// </summary>
        public async Task CleanupOldBackupsAsync(string modificationId, int maxVersions = 5)
        {
            var backupFiles = Directory.GetFiles(_backupPath, $"{modificationId}_*")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();
            
            var filesToDelete = backupFiles.Skip(maxVersions);
            
            foreach (var file in filesToDelete)
            {
                await Task.Run(() => file.Delete());
            }
        }
        
        /// <summary>
        /// Verify that a file matches the warehouse version
        /// </summary>
        public async Task<bool> VerifyFileAsync(FileModification modification)
        {
            try
            {
                var warehouseFile = _warehouseManager.GetFile(modification.WarehouseFileId);
                if (warehouseFile == null) return false;
                
                var targetPath = Path.Combine(_gameInstallPath, modification.RelativeGamePath);
                if (File.Exists(targetPath) == false) return false;
                
                var warehouseBytes = await File.ReadAllBytesAsync(warehouseFile.StoragePath);
                var targetBytes = await File.ReadAllBytesAsync(targetPath);
                
                return warehouseBytes.SequenceEqual(targetBytes);
            }
            catch
            {
                return false;
            }
        }
    }
}
