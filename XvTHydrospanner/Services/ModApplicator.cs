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
        /// Get the actual path on disk with correct casing (handles MELEE vs Melee vs melee)
        /// Windows is case-insensitive but we need the actual path that exists
        /// </summary>
        private string GetActualPathCaseInsensitive(string path)
        {
            // If the path already exists as-is, use it
            if (File.Exists(path) || Directory.Exists(path))
                return path;
            
            // Split into parts and check each directory level
            var root = Path.GetPathRoot(path);
            if (root == null) return path;
            
            var relativePath = path.Substring(root.Length);
            var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            var currentPath = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                
                // Try to find the actual directory/file with correct casing
                try
                {
                    if (Directory.Exists(currentPath))
                    {
                        // Look for matching directory (case-insensitive)
                        var entries = Directory.GetFileSystemEntries(currentPath);
                        var match = entries.FirstOrDefault(e => 
                            Path.GetFileName(e).Equals(part, StringComparison.OrdinalIgnoreCase));
                        
                        if (match != null)
                        {
                            currentPath = match;
                        }
                        else
                        {
                            // Not found, use the provided casing
                            currentPath = Path.Combine(currentPath, part);
                        }
                    }
                    else
                    {
                        currentPath = Path.Combine(currentPath, part);
                    }
                }
                catch
                {
                    // If we can't access, just combine with provided casing
                    currentPath = Path.Combine(currentPath, part);
                }
            }
            
            return currentPath;
        }
        
        /// <summary>
        /// Backup base game LST file if not already backed up
        /// </summary>
        private async Task<bool> BackupBaseLstFileIfNeededAsync(string relativePath)
        {
            if (_baseLstFilesBackedUp.Contains(relativePath))
                return true; // Already backed up in registry
            
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
            
            // Check if backup already exists (e.g., from previous interrupted run)
            if (File.Exists(backupPath))
            {
                // Backup file already exists, just add to registry
                ProgressMessage?.Invoke(this, $"Base LST backup already exists: {relativePath}");
            }
            else
            {
                // Create the backup
                await Task.Run(() => File.Copy(sourcePath, backupPath, overwrite: false));
                ProgressMessage?.Invoke(this, $"Backed up base LST file: {relativePath}");
            }
            
            _baseLstFilesBackedUp.Add(relativePath);
            await SaveBaseLstFileRegistryAsync();
            
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
        /// Represents a mission entry in an LST file (3 lines: ID, filename, name)
        /// </summary>
        private class LstMission
        {
            public string Id { get; set; } = string.Empty;
            public string Filename { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
        
        /// <summary>
        /// Represents a section in an LST file with a header and missions
        /// </summary>
        private class LstSection
        {
            public string Header { get; set; } = string.Empty;
            public List<LstMission> Missions { get; set; } = new List<LstMission>();
        }
        
        /// <summary>
        /// Parse an LST file into structured sections and missions
        /// LST Format: [optional //] Header // Mission1-Line1 Mission1-Line2 Mission1-Line3 ... // [repeat]
        /// </summary>
        private List<LstSection> ParseLstFile(string[] lines)
        {
            var sections = new List<LstSection>();
            var lineIndex = 0;
            
            // Skip leading empty lines
            while (lineIndex < lines.Length && string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                lineIndex++;
            }
            
            // Skip opening // if present
            if (lineIndex < lines.Length && lines[lineIndex].Trim() == "//")
            {
                lineIndex++;
            }
            
            // Parse sections
            while (lineIndex < lines.Length)
            {
                // Skip empty lines
                while (lineIndex < lines.Length && string.IsNullOrWhiteSpace(lines[lineIndex]))
                {
                    lineIndex++;
                }
                
                if (lineIndex >= lines.Length)
                    break;
                
                // Read header line
                var headerLine = lines[lineIndex].Trim();
                if (headerLine == "//")
                {
                    // Skip stray // markers
                    lineIndex++;
                    continue;
                }
                
                var section = new LstSection { Header = headerLine };
                lineIndex++;
                
                // Skip empty lines after header
                while (lineIndex < lines.Length && string.IsNullOrWhiteSpace(lines[lineIndex]))
                {
                    lineIndex++;
                }
                
                // Expect // after header
                if (lineIndex < lines.Length && lines[lineIndex].Trim() == "//")
                {
                    lineIndex++;
                }
                
                // Read missions until we hit // or EOF
                var missionLines = new List<string>();
                while (lineIndex < lines.Length)
                {
                    var line = lines[lineIndex].Trim();
                    
                    // Check if we've reached the end of this section
                    if (line == "//")
                    {
                        lineIndex++;
                        break;
                    }
                    
                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        lineIndex++;
                        continue;
                    }
                    
                    // Add line to mission data
                    missionLines.Add(line);
                    lineIndex++;
                    
                    // When we have 3 lines, create a mission
                    if (missionLines.Count == 3)
                    {
                        var mission = new LstMission
                        {
                            Id = missionLines[0],
                            Filename = missionLines[1],
                            Name = missionLines[2]
                        };
                        section.Missions.Add(mission);
                        missionLines.Clear();
                    }
                }
                
                // Add section if it has missions
                if (section.Missions.Count > 0)
                {
                    sections.Add(section);
                }
            }
            
            return sections;
        }
        
        /// <summary>
        /// Process 3 accumulated lines into a mission entry (DEPRECATED - kept for reference)
        /// </summary>
        private void ProcessMissionLines(List<string> lines, LstSection section)
        {
            if (lines.Count == 3)
            {
                var mission = new LstMission
                {
                    Id = lines[0],
                    Filename = lines[1],
                    Name = lines[2]
                };
                section.Missions.Add(mission);
            }
        }
        
        /// <summary>
        /// Intelligent merge of LST files that understands mission structure
        /// CRITICAL: Properly handles headers and missions, prevents duplicates on reapply
        /// </summary>
        private async Task MergeLstFileAsync(string modLstPath, string targetPath)
        {
            ProgressMessage?.Invoke(this, $"Parsing LST files for intelligent merge: {Path.GetFileName(targetPath)}");
            
            // Parse target LST (if exists)
            var targetSections = new List<LstSection>();
            if (File.Exists(targetPath))
            {
                var targetLines = await File.ReadAllLinesAsync(targetPath);
                targetSections = ParseLstFile(targetLines);
            }
            
            // Parse mod LST
            var modLines = await File.ReadAllLinesAsync(modLstPath);
            var modSections = ParseLstFile(modLines);
            
            // Build a lookup of existing missions by filename (case-insensitive)
            var existingMissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var section in targetSections)
            {
                foreach (var mission in section.Missions)
                {
                    existingMissions.Add(mission.Filename);
                }
            }
            
            // Track what we need to add
            var sectionsToAdd = new List<LstSection>();
            var missionsAddedCount = 0;
            
            // Process each mod section
            foreach (var modSection in modSections)
            {
                // Find matching target section by header (case-insensitive)
                var targetSection = targetSections.Find(s => 
                    string.Equals(s.Header, modSection.Header, StringComparison.OrdinalIgnoreCase));
                
                if (targetSection != null)
                {
                    // Section exists - add only new missions
                    var newMissions = modSection.Missions
                        .Where(m => !existingMissions.Contains(m.Filename))
                        .ToList();
                    
                    if (newMissions.Count > 0)
                    {
                        ProgressMessage?.Invoke(this, $"Adding {newMissions.Count} mission(s) to existing header '{modSection.Header}'");
                        
                        // Add to existing section
                        foreach (var mission in newMissions)
                        {
                            targetSection.Missions.Add(mission);
                            existingMissions.Add(mission.Filename);
                            missionsAddedCount++;
                        }
                    }
                }
                else
                {
                    // New section - check if any missions are new
                    var newMissions = modSection.Missions
                        .Where(m => !existingMissions.Contains(m.Filename))
                        .ToList();
                    
                    if (newMissions.Count > 0)
                    {
                        ProgressMessage?.Invoke(this, $"Adding new header '{modSection.Header}' with {newMissions.Count} mission(s)");
                        
                        var newSection = new LstSection
                        {
                            Header = modSection.Header,
                            Missions = newMissions
                        };
                        
                        sectionsToAdd.Add(newSection);
                        
                        foreach (var mission in newMissions)
                        {
                            existingMissions.Add(mission.Filename);
                            missionsAddedCount++;
                        }
                    }
                }
            }
            
            // Rebuild the target file if we have changes
            if (missionsAddedCount > 0 || sectionsToAdd.Count > 0)
            {
                // Combine existing sections with new sections
                var allSections = new List<LstSection>(targetSections);
                allSections.AddRange(sectionsToAdd);
                
                // Write the complete LST file
                var outputLines = new List<string>();
                
                foreach (var section in allSections)
                {
                    // Add header if present
                    if (!string.IsNullOrEmpty(section.Header))
                    {
                        outputLines.Add(section.Header);
                    }
                    
                    // Add separator
                    outputLines.Add("//");
                    
                    // Add all missions in this section
                    foreach (var mission in section.Missions)
                    {
                        outputLines.Add(mission.Id);
                        outputLines.Add(mission.Filename);
                        outputLines.Add(mission.Name);
                    }
                    
                    // Add closing separator
                    outputLines.Add("//");
                }
                
                // Write to file
                await File.WriteAllLinesAsync(targetPath, outputLines, Encoding.UTF8);
                
                ProgressMessage?.Invoke(this, $"Merged LST: Added {missionsAddedCount} new mission(s) across {allSections.Count} section(s)");
            }
            else
            {
                ProgressMessage?.Invoke(this, "No new missions to add - all missions already exist in target LST");
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
                
                // CRITICAL: Normalize path for case-insensitive matching on Windows
                // Find the actual file path that exists (handles MELEE vs Melee vs melee)
                var actualTargetPath = GetActualPathCaseInsensitive(targetPath);
                
                var targetDir = Path.GetDirectoryName(actualTargetPath);
                
                if (targetDir != null)
                {
                    Directory.CreateDirectory(targetDir);
                }
                
                var isLst = IsLstFile(warehouseFile.OriginalFileName);
                
                // DEBUG: Log file type detection
                ProgressMessage?.Invoke(this, $"File: {warehouseFile.OriginalFileName}, IsLST: {isLst}, Target: {modification.RelativeGamePath}, ActualPath: {actualTargetPath}");
                
                if (isLst)
                {
                    // LST FILE HANDLING - CRITICAL FOR MULTIPLAYER
                    ProgressMessage?.Invoke(this, $"Processing LST file: {modification.RelativeGamePath}");
                    
                    // Backup base game LST file if not already done
                    await BackupBaseLstFileIfNeededAsync(modification.RelativeGamePath);
                    
                    if (File.Exists(actualTargetPath))
                    {
                        // Target LST exists - MERGE content
                        ProgressMessage?.Invoke(this, $"Merging LST file: {warehouseFile.OriginalFileName} into {actualTargetPath}");
                        await MergeLstFileAsync(warehouseFile.StoragePath, actualTargetPath);
                    }
                    else
                    {
                        // Target LST doesn't exist - COPY
                        ProgressMessage?.Invoke(this, $"Copying new LST file: {warehouseFile.OriginalFileName} to {actualTargetPath}");
                        await Task.Run(() => File.Copy(warehouseFile.StoragePath, actualTargetPath, false));
                    }
                    
                    ProgressMessage?.Invoke(this, $"LST file operation complete for {modification.RelativeGamePath}");
                }
                else
                {
                    // REGULAR FILE HANDLING
                    // Create backup if requested and file exists
                    if (createBackup && File.Exists(actualTargetPath))
                    {
                        modification.BackupPath = await CreateBackupAsync(actualTargetPath, modification.Id);
                        BackupCreated?.Invoke(this, modification.BackupPath);
                    }
                    
                    // Copy warehouse file to game location, overwriting
                    await Task.Run(() => File.Copy(warehouseFile.StoragePath, actualTargetPath, true));
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
