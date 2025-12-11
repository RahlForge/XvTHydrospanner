using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace XvTHydrospanner.Services
{
    /// <summary>
    /// Service for handling compressed archive files
    /// </summary>
    public class ArchiveExtractor
    {
        private static readonly string[] SupportedExtensions = { ".zip", ".rar", ".7z", ".tar", ".gz" };
        
        /// <summary>
        /// Checks if a file is a supported archive format
        /// </summary>
        public static bool IsArchive(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return SupportedExtensions.Contains(extension);
        }
        
        /// <summary>
        /// Extracts all files from an archive to a temporary directory
        /// </summary>
        /// <returns>Dictionary mapping original filenames to extracted file paths</returns>
        public static Dictionary<string, string> ExtractArchive(string archivePath)
        {
            if (IsArchive(archivePath) == false)
            {
                throw new InvalidOperationException("File is not a supported archive format");
            }
            
            var extractedFiles = new Dictionary<string, string>();
            var tempDir = Path.Combine(Path.GetTempPath(), "XvTHydrospanner_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                using var archive = ArchiveFactory.Open(archivePath);
                
                foreach (var entry in archive.Entries.Where(e => e.IsDirectory == false))
                {
                    if (string.IsNullOrEmpty(entry.Key))
                        continue;
                    
                    // Normalize the entry path (replace backslashes with forward slashes)
                    var normalizedEntryPath = entry.Key.Replace("\\", "/");
                    
                    // Get just the filename without directory structure
                    var fileName = Path.GetFileName(normalizedEntryPath);
                    
                    if (string.IsNullOrEmpty(fileName))
                        continue;
                    
                    // IMPORTANT: Preserve directory structure to avoid filename collisions
                    // Instead of flattening all files to tempDir, maintain the archive structure
                    // This prevents BATTLE01.TIE and BalanceOfPower/BATTLE/BATTLE01.TIE from colliding
                    var relativePath = normalizedEntryPath.Replace("/", Path.DirectorySeparatorChar.ToString());
                    var extractPath = Path.Combine(tempDir, relativePath);
                    
                    // Ensure the directory exists
                    var extractDir = Path.GetDirectoryName(extractPath);
                    if (!string.IsNullOrEmpty(extractDir) && !Directory.Exists(extractDir))
                    {
                        Directory.CreateDirectory(extractDir);
                    }
                    
                    // Extract with full path preservation
                    entry.WriteToFile(extractPath, new ExtractionOptions { ExtractFullPath = false, Overwrite = true });
                    
                    // Map the original path in archive to extracted file path
                    extractedFiles[entry.Key] = extractPath;
                }
            }
            catch
            {
                // Clean up on error
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
                throw;
            }
            
            return extractedFiles;
        }
        
        /// <summary>
        /// Gets a list of files contained in an archive without extracting
        /// </summary>
        public static List<string> ListArchiveContents(string archivePath)
        {
            if (IsArchive(archivePath) == false)
            {
                throw new InvalidOperationException("File is not a supported archive format");
            }
            
            var files = new List<string>();
            
            using var archive = ArchiveFactory.Open(archivePath);
            foreach (var entry in archive.Entries.Where(e => e.IsDirectory == false))
            {
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    files.Add(entry.Key);
                }
            }
            
            return files;
        }
    }
}
