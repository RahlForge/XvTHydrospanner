using System;
using System.Collections.Generic;

namespace XvTHydrospanner.Models
{
    /// <summary>
    /// Represents a mod package containing multiple related files
    /// </summary>
    public class ModPackage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Author { get; set; }
        public string? Version { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.Now;
        public List<string> Tags { get; set; } = new();
        
        /// <summary>
        /// List of file IDs that belong to this package
        /// </summary>
        public List<string> FileIds { get; set; } = new();
    }
}
