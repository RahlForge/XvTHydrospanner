using System;
using System.Collections.Generic;

namespace XvTHydrospanner.Models
{
    /// <summary>
    /// Represents a file stored in the Mod Warehouse
    /// </summary>
    public class WarehouseFile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Physical file path in the warehouse storage
        /// </summary>
        public string StoragePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Original file name
        /// </summary>
        public string OriginalFileName { get; set; } = string.Empty;
        
        /// <summary>
        /// File extension (e.g., ".TIE", ".LST", ".LFD")
        /// </summary>
        public string FileExtension { get; set; } = string.Empty;
        
        /// <summary>
        /// Category of this mod file
        /// </summary>
        public ModCategory Category { get; set; }
        
        /// <summary>
        /// Intended target path relative to game root
        /// </summary>
        public string TargetRelativePath { get; set; } = string.Empty;
        
        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }
        
        /// <summary>
        /// Date added to warehouse
        /// </summary>
        public DateTime DateAdded { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Tags for organization and search
        /// </summary>
        public List<string> Tags { get; set; } = new();
        
        /// <summary>
        /// Author or source of the mod
        /// </summary>
        public string? Author { get; set; }
        
        /// <summary>
        /// Version of the mod file
        /// </summary>
        public string? Version { get; set; }
        
        /// <summary>
        /// Optional reference to a mod package this file belongs to
        /// </summary>
        public string? ModPackageId { get; set; }
    }
}
