using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XvTHydrospanner.Models;

namespace XvTHydrospanner.Services
{
    /// <summary>
    /// Handles applying and reverting file modifications with special LST file handling
    /// </summary>
    public class ModApplicator
    {
        private readonly string _gameInstallPath;
        private readonly string _backupPath;
        private readonly string _baseLstBackupPath;
        private readonly WarehouseManager _warehouseManager;
        
        // Track which LST files have been backed up from base game
        private readonly HashSet<string> _baseLstFilesBackedUp = new();
        
        public event EventHandler<string>? ModificationApplied;
        public event EventHandler<string>? ModificationReverted;
        public event EventHandler<string>? BackupCreated;
        public event EventHandler<string>? ProgressMessage;
        
        public ModApplicator(string gameInstallPath, string backupPath, WarehouseManager warehouseManager)
        {
            _gameInstallPath = gameInstallPath;
            _backupPath = backupPath;
            _baseLstBackupPath = Path.Combine(backupPath, "BaseLstFiles");
            _warehouseManager = warehouseManager;
            
            Directory.CreateDirectory(_backupPath);
            Directory.CreateDirectory(_baseLstBackupPath);
            
            LoadBaseLstFileRegistry();
        }
        
        /// <summary>
        /// Load registry of which base LST files have been backed up
        /// </summary>
        private void LoadBaseLstFileRegistry()
        {
            var registryPath = Path.Combine(_baseLstBackupPath, "registry.txt");
            if (File.Exists(registryPath))
            {
                var lines = File.ReadAllLines(registryPath);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _baseLstFilesBackedUp.Add(line.Trim());
                    }
                }
            }
        }
        
        /// <summary>
        /// Save registry of backed up base LST files
        /// </summary>
        private async Task SaveBaseLstFileRegistryAsync()
        {
            var registryPath = Path.Combine(_baseLstBackupPath, "registry.txt");
            await File.WriteAllLinesAsync(registryPath, _baseLstFilesBackedUp.OrderBy(f => f));
        }
        
        /// <summary>
        /// Check if a file is a LST file based on extension
        /// </summary>
        private bool IsLstFile(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".lst", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Backup base game LST file if not already backed up
        /// </summary>
        private async Task<bool> BackupBaseLstFileIfNeededAsync(string relativePath)
        {
            if (_baseLstFilesBackedUp.Contains(relativePath))
                return true; // Already backed up
            
            var sourcePath = Path.Combine(_gameInstallPath, relativePath);
            if (!File.Exists(sourcePath))
                return false; // Source file doesn't exist
            
            // Create backup with same folder structure
            var backupPath = Path.Combine(_baseLstBackupPath, relativePath);
            var backupDir = Path.GetDirectoryName(backupPath);
            
            if (backupDir != null)
            {
                Directory.CreateDirectory(backupDir);
            }
            
            await Task.Run(() => File.Copy(sourcePath, backupPath, overwrite: false));
            
            _baseLstFilesBackedUp.Add(relativePath);
            await SaveBaseLstFileRegistryAsync();
            
            ProgressMessage?.Invoke(this, $"Backed up base LST file: {relativePath}");
            return true;
        }
        
        /// <summary>
        /// Restore base game LST file from backup
        /// </summary>
        private async Task<bool> RestoreBaseLstFileAsync(string relativePath)
        {
            if (!_baseLstFilesBackedUp.Contains(relativePath))
                return false; // No backup exists
            
            var backupPath = Path.Combine(_baseLstBackupPath, relativePath);
            var targetPath = Path.Combine(_gameInstallPath, relativePath);
            
            if (!File.Exists(backupPath))
            {
                ProgressMessage?.Invoke(this, $"Warning: Base LST backup not found for {relativePath}");
                return false;
            }
            
            var targetDir = Path.GetDirectoryName(targetPath);
            if (targetDir != null)
            {
                Directory.CreateDirectory(targetDir);
            }
            
            await Task.Run(() => File.Copy(backupPath, targetPath, overwrite: true));
            ProgressMessage?.Invoke(this, $"Restored base LST file: {relativePath}");
            
            return true;
        }
        
        /// <summary>
        /// Merge LST file content by appending mod lines that aren't already present
        /// IMPORTANT: Preserves comment lines (starting with //) which are vital for XvT in-game headers
        /// </summary>
        private async Task MergeLstFileAsync(string modLstPath, string targetPath)
        {
            // Read existing target content
            var existingLines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(targetPath))
            {
                var existing = await File.ReadAllLinesAsync(targetPath);
                foreach (var line in existing)
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        existingLines.Add(trimmed);
                    }
                }
            }
            
            // Read mod LST content
            var modLines = await File.ReadAllLinesAsync(modLstPath);
            var linesToAdd = new List<string>();
            
            foreach (var line in modLines)
            {
                var trimmed = line.Trim();
                
                // Skip truly empty lines, but preserve everything else including comments
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;
                
                // CRITICAL: Always add comment lines (// headers) even if they appear to be duplicates
                // These define sections in XvT's in-game drop-down lists and must be preserved
                if (trimmed.StartsWith("//"))
                {
                    linesToAdd.Add(trimmed);
                    ProgressMessage?.Invoke(this, $"Adding LST header comment: {trimmed}");
                }
                else if (!existingLines.Contains(trimmed))
                {
                    // For non-comment lines, check for duplicates before adding
                    linesToAdd.Add(trimmed);
                    existingLines.Add(trimmed); // Prevent duplicates within same mod
                }
            }
            
            // Append new lines if any
            if (linesToAdd.Count > 0)
            {
                await File.AppendAllLinesAsync(targetPath, linesToAdd, Encoding.UTF8);
                ProgressMessage?.Invoke(this, $"Merged {linesToAdd.Count} line(s) into {Path.GetFileName(targetPath)}");
            }
        }
        
        /// <summary>
        /// Apply a single file modification with LST file special handling
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
                
                var isLst = IsLstFile(warehouseFile.OriginalFileName);
                
                if (isLst)
                {
                    // LST FILE HANDLING - CRITICAL FOR MULTIPLAYER
                    ProgressMessage?.Invoke(this, $"Processing LST file: {modification.RelativeGamePath}");
                    
                    // Backup base game LST file if not already done
                    await BackupBaseLstFileIfNeededAsync(modification.RelativeGamePath);
                    
                    if (File.Exists(targetPath))
                    {
                        // Target LST exists - MERGE content
                        ProgressMessage?.Invoke(this, $"Merging LST file: {warehouseFile.OriginalFileName}");
                        await MergeLstFileAsync(warehouseFile.StoragePath, targetPath);
                    }
                    else
                    {
                        // Target LST doesn't exist - COPY
                        ProgressMessage?.Invoke(this, $"Copying new LST file: {warehouseFile.OriginalFileName}");
                        await Task.Run(() => File.Copy(warehouseFile.StoragePath, targetPath, false));
                    }
                }
                else
                {
                    // REGULAR FILE HANDLING
                    // Create backup if requested and file exists
                    if (createBackup && File.Exists(targetPath))
                    {
                        modification.BackupPath = await CreateBackupAsync(targetPath, modification.Id);
                        BackupCreated?.Invoke(this, modification.BackupPath);
                    }
                    
                    // Copy warehouse file to game location, overwriting
                    await Task.Run(() => File.Copy(warehouseFile.StoragePath, targetPath, true));
                }
                
                modification.IsApplied = true;
                ModificationApplied?.Invoke(this, modification.RelativeGamePath);
                
                return true;
            }
            catch (Exception ex)
            {
                ProgressMessage?.Invoke(this, $"Error applying modification: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Revert a single file modification (not used for LST files)
        /// </summary>
        public async Task<bool> RevertModificationAsync(FileModification modification)
        {
            try
            {
                var warehouseFile = _warehouseManager.GetFile(modification.WarehouseFileId);
                if (warehouseFile != null && IsLstFile(warehouseFile.OriginalFileName))
                {
                    // LST files should not be reverted individually - use RestoreAllBaseLstFilesAsync instead
                    ProgressMessage?.Invoke(this, $"Warning: LST files should be restored as a set, not individually");
                    return false;
                }
                
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
                ProgressMessage?.Invoke(this, $"Error reverting modification: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Restore all base LST files that have been backed up
        /// CRITICAL: Must be called before switching profiles to ensure clean LST state
        /// </summary>
        public async Task<int> RestoreAllBaseLstFilesAsync()
        {
            ProgressMessage?.Invoke(this, "Restoring all base LST files for clean state...");
            
            int restored = 0;
            foreach (var relativePath in _baseLstFilesBackedUp.ToList())
            {
                if (await RestoreBaseLstFileAsync(relativePath))
                {
                    restored++;
                }
            }
            
            ProgressMessage?.Invoke(this, $"Restored {restored} base LST file(s)");
            return restored;
        }
        
        /// <summary>
        /// Apply all modifications in a profile with LST-aware processing
        /// </summary>
        public async Task<(int success, int failed)> ApplyProfileAsync(ModProfile profile, bool createBackup = true)
        {
            ProgressMessage?.Invoke(this, $"Applying profile: {profile.Name}");
            
            int successCount = 0;
            int failedCount = 0;
            
            // Separate LST and non-LST modifications
            var lstMods = new List<FileModification>();
            var regularMods = new List<FileModification>();
            
            foreach (var modification in profile.FileModifications)
            {
                var warehouseFile = _warehouseManager.GetFile(modification.WarehouseFileId);
                if (warehouseFile != null && IsLstFile(warehouseFile.OriginalFileName))
                {
                    lstMods.Add(modification);
                }
                else
                {
                    regularMods.Add(modification);
                }
            }
            
            // Apply regular files first
            ProgressMessage?.Invoke(this, $"Applying {regularMods.Count} regular file(s)...");
            foreach (var modification in regularMods)
            {
                var result = await ApplyModificationAsync(modification, createBackup);
                if (result)
                    successCount++;
                else
                    failedCount++;
            }
            
            // Then apply LST files (which will merge into existing)
            ProgressMessage?.Invoke(this, $"Applying {lstMods.Count} LST file(s)...");
            foreach (var modification in lstMods)
            {
                var result = await ApplyModificationAsync(modification, createBackup);
                if (result)
                    successCount++;
                else
                    failedCount++;
            }
            
            ProgressMessage?.Invoke(this, $"Profile applied: {successCount} succeeded, {failedCount} failed");
            return (successCount, failedCount);
        }
        
        /// <summary>
        /// Revert all non-LST modifications in a profile
        /// LST files are handled separately via RestoreAllBaseLstFilesAsync
        /// </summary>
        public async Task<(int success, int failed)> RevertProfileAsync(ModProfile profile)
        {
            ProgressMessage?.Invoke(this, $"Reverting profile: {profile.Name}");
            
            int successCount = 0;
            int failedCount = 0;
            
            foreach (var modification in profile.FileModifications.Where(m => m.IsApplied))
            {
                var warehouseFile = _warehouseManager.GetFile(modification.WarehouseFileId);
                
                // Skip LST files - they'll be restored via RestoreAllBaseLstFilesAsync
                if (warehouseFile != null && IsLstFile(warehouseFile.OriginalFileName))
                {
                    modification.IsApplied = false;
                    continue;
                }
                
                var result = await RevertModificationAsync(modification);
                if (result)
                    successCount++;
                else
                    failedCount++;
            }
            
            ProgressMessage?.Invoke(this, $"Profile reverted: {successCount} succeeded, {failedCount} failed");
            return (successCount, failedCount);
        }
        
        /// <summary>
        /// Switch from one profile to another with proper LST handling
        /// CRITICAL FOR MULTIPLAYER: Ensures LST files are rebuilt correctly
        /// </summary>
        public async Task<(int applied, int failed)> SwitchProfileAsync(ModProfile? oldProfile, ModProfile newProfile, bool createBackup = true)
        {
            ProgressMessage?.Invoke(this, $"Switching to profile: {newProfile.Name}");
            
            // Step 1: Revert old profile's non-LST modifications
            if (oldProfile != null)
            {
                ProgressMessage?.Invoke(this, "Step 1: Reverting previous profile's regular files...");
                await RevertProfileAsync(oldProfile);
            }
            
            // Step 2: Restore ALL base LST files to clean state
            ProgressMessage?.Invoke(this, "Step 2: Restoring base LST files to clean state...");
            await RestoreAllBaseLstFilesAsync();
            
            // Step 3: Apply new profile (which will properly merge LST files)
            ProgressMessage?.Invoke(this, "Step 3: Applying new profile...");
            var result = await ApplyProfileAsync(newProfile, createBackup);
            
            ProgressMessage?.Invoke(this, "Profile switch complete!");
            return result;
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
