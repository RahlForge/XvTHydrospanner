using System;

namespace XvTHydrospanner.Models
{
    /// <summary>
    /// Represents a single file modification within a profile
    /// </summary>
    public class FileModification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Relative path from game root (e.g., "BalanceOfPower/TRAIN/mission.lst")
        /// </summary>
        public string RelativeGamePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Reference to the mod file in the warehouse
        /// </summary>
        public string WarehouseFileId { get; set; } = string.Empty;
        
        /// <summary>
        /// Original file backup location (for restoration)
        /// </summary>
        public string? BackupPath { get; set; }
        
        /// <summary>
        /// Category of the modification
        /// </summary>
        public ModCategory Category { get; set; }
        
        /// <summary>
        /// Whether this modification is currently applied
        /// </summary>
        public bool IsApplied { get; set; }
        
        /// <summary>
        /// Description of what this modification does
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
    
    public enum ModCategory
    {
        Mission,
        Graphics,
        Sound,
        Music,
        Configuration,
        Resource,
        Campaign,
        Training,
        Battle,
        Melee,
        Tournament,
        Other
    }
}
