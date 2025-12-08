using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XvTHydrospanner.Models;

namespace XvTHydrospanner.Services
{
    /// <summary>
    /// Manages remote warehouse catalog and downloads from GitHub
    /// </summary>
    public class RemoteWarehouseManager
    {
        private readonly HttpClient _httpClient;
        private readonly WarehouseManager _localWarehouse;
        private readonly string _repositoryOwner;
        private readonly string _repositoryName;
        private readonly string _branch;
        private RemoteCatalog? _remoteCatalog;
        
        public event EventHandler<RemoteWarehouseFile>? FileDownloaded;
        public event EventHandler<RemoteModPackage>? PackageDownloaded;
        public event EventHandler<string>? DownloadProgress;
        
        private const string DefaultRepositoryOwner = "RahlForge";
        private const string DefaultRepositoryName = "XvTHydrospanner-Mods";
        private const string DefaultBranch = "main";
        
        public RemoteWarehouseManager(WarehouseManager localWarehouse, string? owner = null, string? repo = null, string? branch = null)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "XvTHydrospanner-ModManager");
            _localWarehouse = localWarehouse;
            _repositoryOwner = owner ?? DefaultRepositoryOwner;
            _repositoryName = repo ?? DefaultRepositoryName;
            _branch = branch ?? DefaultBranch;
        }
        
        /// <summary>
        /// Load remote catalog from GitHub repository
        /// </summary>
        public async Task<RemoteCatalog> LoadRemoteCatalogAsync(
            string? owner = null, 
            string? repo = null, 
            string? branch = null)
        {
            owner ??= _repositoryOwner;
            repo ??= _repositoryName;
            branch ??= _branch;
            
            var catalogUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/catalog.json";
            
            try
            {
                DownloadProgress?.Invoke(this, "Fetching remote catalog...");
                var json = await _httpClient.GetStringAsync(catalogUrl);
                _remoteCatalog = JsonConvert.DeserializeObject<RemoteCatalog>(json) ?? new RemoteCatalog();
                
                // Mark files that are already downloaded
                var localFiles = _localWarehouse.GetAllFiles();
                foreach (var remoteFile in _remoteCatalog.Files)
                {
                    remoteFile.IsDownloaded = localFiles.Any(f => 
                        f.Name == remoteFile.Name && 
                        f.Version == remoteFile.Version);
                }
                
                var localPackages = _localWarehouse.GetAllPackages();
                foreach (var remotePackage in _remoteCatalog.Packages)
                {
                    remotePackage.IsDownloaded = localPackages.Any(p => 
                        p.Name == remotePackage.Name && 
                        p.Version == remotePackage.Version);
                }
                
                DownloadProgress?.Invoke(this, $"Loaded {_remoteCatalog.Files.Count} files and {_remoteCatalog.Packages.Count} packages");
                return _remoteCatalog;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Failed to load remote catalog: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Download a single file from remote catalog to local warehouse
        /// </summary>
        public async Task<WarehouseFile> DownloadFileAsync(RemoteWarehouseFile remoteFile)
        {
            if (string.IsNullOrEmpty(remoteFile.DownloadUrl))
                throw new InvalidOperationException("Remote file has no download URL");
            
            try
            {
                DownloadProgress?.Invoke(this, $"Downloading {remoteFile.Name}...");
                
                // Download file to temp location
                var tempPath = Path.GetTempFileName();
                var fileBytes = await _httpClient.GetByteArrayAsync(remoteFile.DownloadUrl);
                await File.WriteAllBytesAsync(tempPath, fileBytes);
                
                // Add to local warehouse
                var localFile = await _localWarehouse.AddFileAsync(
                    tempPath,
                    remoteFile.Name,
                    remoteFile.Description,
                    remoteFile.Category,
                    remoteFile.TargetRelativePath,
                    remoteFile.Author,
                    remoteFile.Version,
                    remoteFile.Tags
                );
                
                // Clean up temp file
                File.Delete(tempPath);
                
                remoteFile.IsDownloaded = true;
                FileDownloaded?.Invoke(this, remoteFile);
                DownloadProgress?.Invoke(this, $"Downloaded {remoteFile.Name}");
                
                return localFile;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to download file: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Download a mod package archive from remote catalog
        /// </summary>
        public async Task<ModPackage> DownloadPackageAsync(RemoteModPackage remotePackage)
        {
            if (string.IsNullOrEmpty(remotePackage.DownloadUrl))
                throw new InvalidOperationException("Remote package has no download URL");
            
            try
            {
                DownloadProgress?.Invoke(this, $"Downloading package {remotePackage.Name}...");
                
                // Download archive to temp location
                var tempPath = Path.Combine(Path.GetTempPath(), $"{remotePackage.Id}.zip");
                var fileBytes = await _httpClient.GetByteArrayAsync(remotePackage.DownloadUrl);
                await File.WriteAllBytesAsync(tempPath, fileBytes);
                
                // Add package to local warehouse
                var localPackage = await _localWarehouse.AddModPackageFromArchiveAsync(
                    tempPath,
                    remotePackage.Name,
                    remotePackage.Description,
                    remotePackage.Author,
                    remotePackage.Version,
                    remotePackage.Tags
                );
                
                // Clean up temp file
                File.Delete(tempPath);
                
                remotePackage.IsDownloaded = true;
                PackageDownloaded?.Invoke(this, remotePackage);
                DownloadProgress?.Invoke(this, $"Downloaded package {remotePackage.Name}");
                
                return localPackage;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to download package: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get the current remote catalog
        /// </summary>
        public RemoteCatalog? GetRemoteCatalog()
        {
            return _remoteCatalog;
        }
        
        /// <summary>
        /// Search remote files by name or description
        /// </summary>
        public List<RemoteWarehouseFile> SearchRemoteFiles(string searchTerm)
        {
            if (_remoteCatalog == null)
                return new List<RemoteWarehouseFile>();
            
            searchTerm = searchTerm.ToLower();
            return _remoteCatalog.Files.Where(f =>
                f.Name.ToLower().Contains(searchTerm) ||
                f.Description.ToLower().Contains(searchTerm) ||
                f.Author?.ToLower().Contains(searchTerm) == true
            ).ToList();
        }
        
        /// <summary>
        /// Get remote files by category
        /// </summary>
        public List<RemoteWarehouseFile> GetRemoteFilesByCategory(ModCategory category)
        {
            if (_remoteCatalog == null)
                return new List<RemoteWarehouseFile>();
            
            return _remoteCatalog.Files.Where(f => f.Category == category).ToList();
        }
        
        /// <summary>
        /// Get only files not yet downloaded
        /// </summary>
        public List<RemoteWarehouseFile> GetAvailableFiles()
        {
            if (_remoteCatalog == null)
                return new List<RemoteWarehouseFile>();
            
            return _remoteCatalog.Files.Where(f => !f.IsDownloaded).ToList();
        }
        
        /// <summary>
        /// Get only packages not yet downloaded
        /// </summary>
        public List<RemoteModPackage> GetAvailablePackages()
        {
            if (_remoteCatalog == null)
                return new List<RemoteModPackage>();
            
            return _remoteCatalog.Packages.Where(p => !p.IsDownloaded).ToList();
        }
        
        /// <summary>
        /// Refresh download status of remote items
        /// </summary>
        public void RefreshDownloadStatus()
        {
            if (_remoteCatalog == null)
                return;
            
            var localFiles = _localWarehouse.GetAllFiles();
            foreach (var remoteFile in _remoteCatalog.Files)
            {
                remoteFile.IsDownloaded = localFiles.Any(f =>
                    f.Name == remoteFile.Name &&
                    f.Version == remoteFile.Version);
            }
            
            var localPackages = _localWarehouse.GetAllPackages();
            foreach (var remotePackage in _remoteCatalog.Packages)
            {
                remotePackage.IsDownloaded = localPackages.Any(p =>
                    p.Name == remotePackage.Name &&
                    p.Version == remotePackage.Version);
            }
        }
    }
}
