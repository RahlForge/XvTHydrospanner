using System;
using System.Collections.Generic;

namespace XvTHydrospanner.Models
{
    /// <summary>
    /// Represents a modification profile that contains a set of file modifications
    /// </summary>
    public class ModProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public bool IsActive { get; set; }
        
        /// <summary>
        /// Indicates if this profile is read-only (e.g., Base Game Install profile)
        /// Read-only profiles cannot have mods applied to them
        /// </summary>
        public bool IsReadOnly { get; set; }
        
        /// <summary>
        /// List of file modifications in this profile
        /// </summary>
        public List<FileModification> FileModifications { get; set; } = new();
        
        /// <summary>
        /// Custom settings specific to this profile
        /// </summary>
        public Dictionary<string, string> CustomSettings { get; set; } = new();
    }
}
