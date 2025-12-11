using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XvTHydrospanner.Models;

namespace XvTHydrospanner.Services
{
    /// <summary>
    /// Manages the warehouse of mod files
    /// </summary>
    public class WarehouseManager
    {
        private readonly string _warehousePath;
        private readonly string _catalogPath;
        private readonly string _packagesPath;
        private List<WarehouseFile> _catalog = new();
        private List<ModPackage> _packages = new();
        
        public event EventHandler<WarehouseFile>? FileAdded;
        public event EventHandler<WarehouseFile>? FileRemoved;
        public event EventHandler<WarehouseFile>? FileUpdated;
        
        public WarehouseManager(string warehousePath)
        {
            _warehousePath = warehousePath;
            _catalogPath = Path.Combine(_warehousePath, "catalog.json");
            _packagesPath = Path.Combine(_warehousePath, "packages.json");
            Directory.CreateDirectory(_warehousePath);
        }
        
        /// <summary>
        /// Load the warehouse catalog
        /// </summary>
        public async Task LoadCatalogAsync()
        {
            if (File.Exists(_catalogPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_catalogPath);
                    _catalog = JsonConvert.DeserializeObject<List<WarehouseFile>>(json) ?? new();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading warehouse catalog: {ex.Message}");
                    _catalog = new();
                }
            }
            else
            {
                _catalog = new();
            }
            
            if (File.Exists(_packagesPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_packagesPath);
                    _packages = JsonConvert.DeserializeObject<List<ModPackage>>(json) ?? new();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading mod packages: {ex.Message}");
                    _packages = new();
                }
            }
            else
            {
                _packages = new();
            }
        }
        
        /// <summary>
        /// Save the warehouse catalog
        /// </summary>
        private async Task SaveCatalogAsync()
        {
            var json = JsonConvert.SerializeObject(_catalog, Formatting.Indented);
            await File.WriteAllTextAsync(_catalogPath, json);
        }
        
        /// <summary>
        /// Save mod packages
        /// </summary>
        private async Task SavePackagesAsync()
        {
            var json = JsonConvert.SerializeObject(_packages, Formatting.Indented);
            await File.WriteAllTextAsync(_packagesPath, json);
        }
        
        /// <summary>
        /// Add a file to the warehouse
        /// </summary>
        public async Task<WarehouseFile> AddFileAsync(string sourceFilePath, string name, string description, 
            ModCategory category, string targetRelativePath, string? author = null, string? version = null, List<string>? tags = null)
        {
            if (File.Exists(sourceFilePath) == false)
                throw new FileNotFoundException("Source file not found", sourceFilePath);
            
            var fileInfo = new FileInfo(sourceFilePath);
            var warehouseFile = new WarehouseFile
            {
                Name = name,
                Description = description,
                OriginalFileName = fileInfo.Name,
                FileExtension = fileInfo.Extension,
                Category = category,
                TargetRelativePath = targetRelativePath,
                FileSizeBytes = fileInfo.Length,
                DateAdded = DateTime.Now,
                Author = author,
                Version = version,
                Tags = tags ?? new List<string>()
            };
            
            // Store file with unique ID-based name to avoid conflicts
            var storagePath = Path.Combine(_warehousePath, $"{warehouseFile.Id}{warehouseFile.FileExtension}");
            await Task.Run(() => File.Copy(sourceFilePath, storagePath, true));
            
            warehouseFile.StoragePath = storagePath;
            _catalog.Add(warehouseFile);
            
            await SaveCatalogAsync();
            FileAdded?.Invoke(this, warehouseFile);
            
            return warehouseFile;
        }
        
        /// <summary>
        /// Remove a file from the warehouse
        /// </summary>
        public async Task RemoveFileAsync(string fileId)
        {
            var file = _catalog.FirstOrDefault(f => f.Id == fileId);
            if (file == null)
                throw new InvalidOperationException($"File {fileId} not found in warehouse");
            
            if (File.Exists(file.StoragePath))
            {
                await Task.Run(() => File.Delete(file.StoragePath));
            }
            
            _catalog.Remove(file);
            await SaveCatalogAsync();
            
            FileRemoved?.Invoke(this, file);
        }
        
        /// <summary>
        /// Update warehouse file metadata
        /// </summary>
        public async Task UpdateFileAsync(WarehouseFile file)
        {
            var existing = _catalog.FirstOrDefault(f => f.Id == file.Id);
            if (existing == null)
                throw new InvalidOperationException($"File {file.Id} not found in warehouse");
            
            _catalog.Remove(existing);
            _catalog.Add(file);
            
            await SaveCatalogAsync();
            FileUpdated?.Invoke(this, file);
        }
        
        /// <summary>
        /// Get a file by ID
        /// </summary>
        public WarehouseFile? GetFile(string fileId)
        {
            return _catalog.FirstOrDefault(f => f.Id == fileId);
        }
        
        /// <summary>
        /// Get all files in the warehouse
        /// </summary>
        public List<WarehouseFile> GetAllFiles()
        {
            return _catalog.ToList();
        }
        
        /// <summary>
        /// Search files by category
        /// </summary>
        public List<WarehouseFile> GetFilesByCategory(ModCategory category)
        {
            return _catalog.Where(f => f.Category == category).ToList();
        }
        
        /// <summary>
        /// Search files by tag
        /// </summary>
        public List<WarehouseFile> SearchByTag(string tag)
        {
            return _catalog.Where(f => f.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
        }
        
        /// <summary>
        /// Search files by name or description
        /// </summary>
        public List<WarehouseFile> Search(string searchTerm)
        {
            searchTerm = searchTerm.ToLower();
            return _catalog.Where(f => 
                f.Name.ToLower().Contains(searchTerm) || 
                f.Description.ToLower().Contains(searchTerm) ||
                f.OriginalFileName.ToLower().Contains(searchTerm)
            ).ToList();
        }
        
        /// <summary>
        /// Export a warehouse file to a specified location
        /// </summary>
        public async Task ExportFileAsync(string fileId, string destinationPath)
        {
            var file = GetFile(fileId);
            if (file == null)
                throw new InvalidOperationException($"File {fileId} not found in warehouse");
            
            if (File.Exists(file.StoragePath) == false)
                throw new FileNotFoundException("Warehouse file not found", file.StoragePath);
            
            await Task.Run(() => File.Copy(file.StoragePath, destinationPath, true));
        }
        
        /// <summary>
        /// Add a mod package from an archive file
        /// </summary>
        public async Task<ModPackage> AddModPackageFromArchiveAsync(string archivePath, string name, string description,
            string? author = null, string? version = null, List<string>? tags = null, Dictionary<string, List<string>>? customFileLocations = null, bool copyToGameRoot = false)
        {
            if (ArchiveExtractor.IsArchive(archivePath) == false)
            {
                throw new InvalidOperationException("File is not a supported archive format");
            }
            
            var extractedFiles = ArchiveExtractor.ExtractArchive(archivePath);
            
            var package = new ModPackage
            {
                Name = name,
                Description = description,
                Author = author,
                Version = version,
                Tags = tags ?? new List<string>(),
                DateAdded = DateTime.Now
            };
            
            try
            {
                foreach (var kvp in extractedFiles)
                {
                    var originalPath = kvp.Key;
                    var extractedPath = kvp.Value;
                    var fileName = Path.GetFileName(extractedPath);
                    
                    List<string> targetPaths = new List<string>();
                    
                    // Check if custom locations were provided for this file
                    if (customFileLocations != null && customFileLocations.TryGetValue(fileName, out var customPaths))
                    {
                        targetPaths = new List<string>(customPaths);
                        
                        // If copyToGameRoot is enabled, also add game root equivalents for BalanceOfPower paths
                        if (copyToGameRoot)
                        {
                            var additionalPaths = new List<string>();
                            foreach (var customPath in customPaths)
                            {
                                if (customPath.StartsWith("BalanceOfPower/", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Add the game root equivalent path
                                    var gameRootPath = customPath.Substring("BalanceOfPower/".Length);
                                    additionalPaths.Add(gameRootPath);
                                }
                            }
                            targetPaths.AddRange(additionalPaths);
                        }
                    }
                    else
                    {
                        // Determine category and target path based on folder structure and file extension
                        var (category, targetPath) = DetermineFilePathAndCategory(originalPath, fileName);
                        targetPaths.Add(targetPath);
                        
                        // If copyToGameRoot is enabled and the path starts with BalanceOfPower/
                        if (copyToGameRoot && targetPath.StartsWith("BalanceOfPower/", StringComparison.OrdinalIgnoreCase))
                        {
                            // Also add the game root equivalent path
                            var gameRootPath = targetPath.Substring("BalanceOfPower/".Length);
                            targetPaths.Add(gameRootPath);
                        }
                    }
                    
                    // Create a warehouse file for each target location
                    foreach (var targetPath in targetPaths)
                    {
                        var category = DetermineCategoryFromPath(targetPath);
                        
                        var warehouseFile = await AddFileAsync(
                            extractedPath,
                            Path.GetFileNameWithoutExtension(fileName),
                            $"Part of {name}",
                            category,
                            targetPath,
                            author,
                            version,
                            tags
                        );
                        
                        warehouseFile.ModPackageId = package.Id;
                        package.FileIds.Add(warehouseFile.Id);
                    }
                }
                
                _packages.Add(package);
                await SaveCatalogAsync();
                await SavePackagesAsync();
                
                return package;
            }
            finally
            {
                // Clean up extracted files
                foreach (var extractedPath in extractedFiles.Values)
                {
                    try
                    {
                        if (File.Exists(extractedPath))
                        {
                            File.Delete(extractedPath);
                        }
                    }
                    catch { }
                }
                
                // Clean up temp directory
                var tempDir = Path.GetDirectoryName(extractedFiles.Values.FirstOrDefault());
                if (tempDir != null && Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }
        
        /// <summary>
        /// Identifies files in an archive that have no folder structure
        /// </summary>
        public static List<string> GetFilesWithoutFolderStructure(string archivePath)
        {
            if (ArchiveExtractor.IsArchive(archivePath) == false)
            {
                throw new InvalidOperationException("File is not a supported archive format");
            }
            
            var files = ArchiveExtractor.ListArchiveContents(archivePath);
            var filesWithoutStructure = new List<string>();
            
            var knownFolders = new[]
            {
                "BATTLE", "COMBAT", "TRAIN", "TRAINING", "MELEE", 
                "CAMPAIGN", "TOURN", "TOURNAMENT", "CP320", "CP640",
                "AMOVIE", "BMOVIE", "MUSIC", "WAVE", "RESOURCE"
            };
            
            foreach (var file in files)
            {
                var normalizedPath = file.Replace('\\', '/');
                var pathParts = normalizedPath.Split('/');
                
                // If file is directly in root (only 1 part = filename) or no known folders detected
                var hasKnownFolder = pathParts.Any(p => 
                    knownFolders.Any(f => f.Equals(p, StringComparison.OrdinalIgnoreCase)));
                
                if (!hasKnownFolder)
                {
                    var fileName = Path.GetFileName(file);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        filesWithoutStructure.Add(fileName);
                    }
                }
            }
            
            return filesWithoutStructure;
        }
        
        /// <summary>
        /// Determines category from a target path
        /// </summary>
        private ModCategory DetermineCategoryFromPath(string targetPath)
        {
            var upperPath = targetPath.ToUpperInvariant();
            
            if (upperPath.Contains("/BATTLE/")) return ModCategory.Battle;
            if (upperPath.Contains("COMBAT/")) return ModCategory.Mission;
            if (upperPath.Contains("/TRAIN/")) return ModCategory.Training;
            if (upperPath.Contains("/MELEE/")) return ModCategory.Melee;
            if (upperPath.Contains("/CAMPAIGN/")) return ModCategory.Campaign;
            if (upperPath.Contains("/TOURN/")) return ModCategory.Tournament;
            if (upperPath.Contains("CP320/") || upperPath.Contains("CP640/")) return ModCategory.Graphics;
            if (upperPath.Contains("AMOVIE/") || upperPath.Contains("BMOVIE/")) return ModCategory.Graphics;
            if (upperPath.Contains("MUSIC/")) return ModCategory.Music;
            if (upperPath.Contains("WAVE/")) return ModCategory.Sound;
            if (upperPath.Contains("RESOURCE/")) return ModCategory.Resource;
            
            var extension = Path.GetExtension(targetPath).ToUpperInvariant();
            return DetermineCategoryForExtension(extension);
        }
        
        /// <summary>
        /// Determines the target path and category for a file based on its location in the archive
        /// </summary>
        private (ModCategory category, string targetPath) DetermineFilePathAndCategory(string originalArchivePath, string fileName)
        {
            var extension = Path.GetExtension(fileName).ToUpperInvariant();
            var category = ModCategory.Other;
            var targetPath = fileName;
            
            // Normalize path separators
            originalArchivePath = originalArchivePath.Replace('\\', '/');
            
            // Known XvT/BoP folder names to look for
            var knownFolders = new[]
            {
                "BATTLE", "COMBAT", "TRAIN", "TRAINING", "MELEE", 
                "CAMPAIGN", "TOURN", "TOURNAMENT", "CP320", "CP640",
                "AMOVIE", "BMOVIE", "MUSIC", "WAVE", "RESOURCE"
            };
            
            // Try to find a known folder in the path
            string? detectedFolder = null;
            foreach (var folder in knownFolders)
            {
                // Check if the path contains this folder (case-insensitive)
                var pathParts = originalArchivePath.Split('/');
                var matchingPart = pathParts.FirstOrDefault(p => 
                    p.Equals(folder, StringComparison.OrdinalIgnoreCase));
                
                if (matchingPart != null)
                {
                    detectedFolder = matchingPart.ToUpperInvariant();
                    break;
                }
            }
            
            // If we found a known folder, use it to construct the path
            if (detectedFolder != null)
            {
                targetPath = DetermineTargetPathForFolder(detectedFolder, fileName);
                category = DetermineCategoryForFolder(detectedFolder, extension);
            }
            else
            {
                // No known folder detected, use default logic based on extension
                targetPath = DetermineDefaultTargetPath(extension, fileName);
                category = DetermineCategoryForExtension(extension);
            }
            
            return (category, targetPath);
        }
        
        /// <summary>
        /// Determines target path for a file based on the detected folder
        /// </summary>
        private string DetermineTargetPathForFolder(string folder, string fileName)
        {
            return folder switch
            {
                "BATTLE" => "BalanceOfPower/BATTLE/" + fileName,
                "COMBAT" => "Combat/" + fileName,
                "TRAIN" or "TRAINING" => "BalanceOfPower/TRAIN/" + fileName,
                "MELEE" => "BalanceOfPower/MELEE/" + fileName,
                "CAMPAIGN" => "BalanceOfPower/CAMPAIGN/" + fileName,
                "TOURN" or "TOURNAMENT" => "BalanceOfPower/TOURN/" + fileName,
                "CP320" => "cp320/" + fileName,
                "CP640" => "cp640/" + fileName,
                "AMOVIE" => "Amovie/" + fileName,
                "BMOVIE" => "Bmovie/" + fileName,
                "MUSIC" => "Music/" + fileName,
                "WAVE" => "wave/" + fileName,
                "RESOURCE" => "resource/" + fileName,
                _ => "BalanceOfPower/" + fileName
            };
        }
        
        /// <summary>
        /// Determines category based on the detected folder
        /// </summary>
        private ModCategory DetermineCategoryForFolder(string folder, string extension)
        {
            return folder switch
            {
                "BATTLE" => ModCategory.Battle,
                "COMBAT" => ModCategory.Mission,
                "TRAIN" or "TRAINING" => ModCategory.Training,
                "MELEE" => ModCategory.Melee,
                "CAMPAIGN" => ModCategory.Campaign,
                "TOURN" or "TOURNAMENT" => ModCategory.Tournament,
                "CP320" or "CP640" => ModCategory.Graphics,
                "AMOVIE" or "BMOVIE" => ModCategory.Graphics,
                "MUSIC" => ModCategory.Music,
                "WAVE" => ModCategory.Sound,
                "RESOURCE" => ModCategory.Resource,
                _ => DetermineCategoryForExtension(extension)
            };
        }
        
        /// <summary>
        /// Determines default target path based on file extension
        /// </summary>
        private string DetermineDefaultTargetPath(string extension, string fileName)
        {
            return extension switch
            {
                ".TIE" => "BalanceOfPower/BATTLE/" + fileName,
                ".LST" => "BalanceOfPower/BATTLE/" + fileName,
                ".LFD" => "BalanceOfPower/" + fileName,
                ".WAV" or ".VOC" => "wave/" + fileName,
                ".WRK" => "Amovie/" + fileName,
                _ => fileName
            };
        }
        
        /// <summary>
        /// Determines category based on file extension
        /// </summary>
        private ModCategory DetermineCategoryForExtension(string extension)
        {
            return extension switch
            {
                ".TIE" => ModCategory.Mission,
                ".LST" => ModCategory.Battle,
                ".LFD" => ModCategory.Resource,
                ".WAV" or ".VOC" => ModCategory.Sound,
                ".WRK" => ModCategory.Graphics,
                ".CFG" or ".TXT" => ModCategory.Configuration,
                _ => ModCategory.Other
            };
        }
        
        /// <summary>
        /// Get all mod packages
        /// </summary>
        public List<ModPackage> GetAllPackages()
        {
            return _packages.ToList();
        }
        
        /// <summary>
        /// Get a mod package by ID
        /// </summary>
        public ModPackage? GetPackage(string packageId)
        {
            return _packages.FirstOrDefault(p => p.Id == packageId);
        }
        
        /// <summary>
        /// Get all files belonging to a mod package
        /// </summary>
        public List<WarehouseFile> GetPackageFiles(string packageId)
        {
            var package = GetPackage(packageId);
            if (package == null)
                return new List<WarehouseFile>();
            
            return _catalog.Where(f => package.FileIds.Contains(f.Id)).ToList();
        }
        
        /// <summary>
        /// Remove a mod package and optionally its files
        /// </summary>
        public async Task RemovePackageAsync(string packageId, bool removeFiles = true)
        {
            var package = GetPackage(packageId);
            if (package == null)
                throw new InvalidOperationException($"Package {packageId} not found");
            
            if (removeFiles)
            {
                foreach (var fileId in package.FileIds.ToList())
                {
                    try
                    {
                        await RemoveFileAsync(fileId);
                    }
                    catch { }
                }
            }
            
            _packages.Remove(package);
            await SavePackagesAsync();
        }
    }
}
