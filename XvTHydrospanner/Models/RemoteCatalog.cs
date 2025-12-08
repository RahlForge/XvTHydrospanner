using System.Collections.Generic;

namespace XvTHydrospanner.Models
{
    /// <summary>
    /// Represents the remote catalog of available mods from central repository
    /// </summary>
    public class RemoteCatalog
    {
        public string Version { get; set; } = "1.0";
        public string RepositoryUrl { get; set; } = string.Empty;
        public List<RemoteWarehouseFile> Files { get; set; } = new();
        public List<RemoteModPackage> Packages { get; set; } = new();
    }

    /// <summary>
    /// Represents a warehouse file available in the remote repository
    /// </summary>
    public class RemoteWarehouseFile : WarehouseFile
    {
        /// <summary>
        /// GitHub raw URL for downloading the file
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// SHA hash for verifying file integrity
        /// </summary>
        public string? Sha { get; set; }
        
        /// <summary>
        /// Whether this file is already in local warehouse
        /// </summary>
        public bool IsDownloaded { get; set; }
    }

    /// <summary>
    /// Represents a mod package available in the remote repository
    /// </summary>
    public class RemoteModPackage : ModPackage
    {
        /// <summary>
        /// GitHub raw URL for downloading the package archive
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// SHA hash for verifying archive integrity
        /// </summary>
        public string? Sha { get; set; }
        
        /// <summary>
        /// Whether this package is already in local warehouse
        /// </summary>
        public bool IsDownloaded { get; set; }
    }
}
