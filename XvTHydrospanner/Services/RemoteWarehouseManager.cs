using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
                
                // Build file location mapping from catalog
                // This is critical for preserving file target paths when downloading packages
                // 
                // CONTEXT: When packages are uploaded, files are stored in ZIP with their full 
                // TargetRelativePath as the entry name (e.g., "BalanceOfPower/BATTLE/BATTLE01.TIE").
                // The remote catalog contains metadata for each file including its TargetRelativePath.
                // 
                // PROBLEM: Archive extraction flattens files by filename, causing collisions when
                // multiple files have the same name but different paths (e.g., BATTLE01.TIE in both
                // game root and BalanceOfPower directories).
                // 
                // SOLUTION: Map ZIP entry paths (which match TargetRelativePath) to their target
                // locations BEFORE extraction, so the warehouse manager knows where each file belongs.
                Dictionary<string, List<string>>? customFileLocations = null;
                if (_remoteCatalog != null)
                {
                    // Get all files that belong to this package from the catalog
                    var packageFiles = _remoteCatalog.Files.Where(f => f.ModPackageId == remotePackage.Id).ToList();
                    
                    if (packageFiles.Any())
                    {
                        customFileLocations = new Dictionary<string, List<string>>();
                        
                        // Build mapping: ZIP entry path → target location(s)
                        // Example: {"BalanceOfPower/BATTLE/BATTLE01.TIE" → ["BalanceOfPower/BATTLE/BATTLE01.TIE"]}
                        foreach (var file in packageFiles)
                        {
                            // The archive entry path uses forward slashes (ZIP standard)
                            // and matches the TargetRelativePath from when it was uploaded
                            var archiveEntryPath = file.TargetRelativePath.Replace("\\", "/");
                            
                            if (!customFileLocations.ContainsKey(archiveEntryPath))
                            {
                                customFileLocations[archiveEntryPath] = new List<string>();
                            }
                            
                            // Store the actual target path for this file
                            customFileLocations[archiveEntryPath].Add(file.TargetRelativePath);
                        }
                    }
                }
                
                // Add package to local warehouse with file mappings
                var localPackage = await _localWarehouse.AddModPackageFromArchiveAsync(
                    tempPath,
                    remotePackage.Name,
                    remotePackage.Description,
                    remotePackage.Author,
                    remotePackage.Version,
                    remotePackage.Tags,
                    customFileLocations
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
        
        /// <summary>
        /// Upload a file from local warehouse to remote GitHub repository
        /// </summary>
        public async Task UploadFileAsync(WarehouseFile localFile, string githubToken)
        {
            if (string.IsNullOrEmpty(githubToken))
                throw new InvalidOperationException("GitHub token is required for uploading files");
            
            if (!File.Exists(localFile.StoragePath))
                throw new FileNotFoundException("Local file not found", localFile.StoragePath);
            
            try
            {
                DownloadProgress?.Invoke(this, $"Uploading {localFile.Name}...");
                
                // Read file content
                var fileBytes = await File.ReadAllBytesAsync(localFile.StoragePath);
                var base64Content = Convert.ToBase64String(fileBytes);
                
                // Prepare the path in the repository
                var fileName = $"{localFile.Id}{localFile.FileExtension}";
                var repoPath = $"files/{fileName}";
                
                // Check if file already exists
                var checkUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/contents/{repoPath}?ref={_branch}";
                var checkRequest = new HttpRequestMessage(HttpMethod.Get, checkUrl);
                checkRequest.Headers.Add("Authorization", $"Bearer {githubToken}");
                checkRequest.Headers.Add("Accept", "application/vnd.github+json");
                
                var checkResponse = await _httpClient.SendAsync(checkRequest);
                string? existingSha = null;
                
                if (checkResponse.IsSuccessStatusCode)
                {
                    var existingContent = await checkResponse.Content.ReadAsStringAsync();
                    var existingJson = JsonConvert.DeserializeObject<dynamic>(existingContent);
                    existingSha = existingJson?.sha;
                }
                
                // Upload file via GitHub API
                var uploadUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/contents/{repoPath}";
                var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
                uploadRequest.Headers.Add("Authorization", $"Bearer {githubToken}");
                uploadRequest.Headers.Add("Accept", "application/vnd.github+json");
                
                var uploadPayload = new
                {
                    message = $"Upload mod: {localFile.Name}",
                    content = base64Content,
                    branch = _branch,
                    sha = existingSha
                };
                
                uploadRequest.Content = new StringContent(
                    JsonConvert.SerializeObject(uploadPayload),
                    System.Text.Encoding.UTF8,
                    "application/json");
                
                var uploadResponse = await _httpClient.SendAsync(uploadRequest);
                
                if (!uploadResponse.IsSuccessStatusCode)
                {
                    var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Failed to upload file: {uploadResponse.StatusCode} - {errorContent}");
                }
                
                var responseContent = await uploadResponse.Content.ReadAsStringAsync();
                var responseJson = JsonConvert.DeserializeObject<dynamic>(responseContent);
                var downloadUrl = $"https://raw.githubusercontent.com/{_repositoryOwner}/{_repositoryName}/{_branch}/{repoPath}";
                
                DownloadProgress?.Invoke(this, $"Uploaded {localFile.Name} successfully");
                
                // Now update the catalog
                await UpdateRemoteCatalogWithFileAsync(localFile, downloadUrl, githubToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to upload file: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Update or create remote catalog.json with new file entry
        /// </summary>
        private async Task UpdateRemoteCatalogWithFileAsync(WarehouseFile localFile, string downloadUrl, string githubToken)
        {
            try
            {
                DownloadProgress?.Invoke(this, "Updating remote catalog...");
                
                // Try to get existing catalog
                RemoteCatalog catalog;
                string? existingCatalogSha = null;
                
                var catalogUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/contents/catalog.json?ref={_branch}";
                var catalogRequest = new HttpRequestMessage(HttpMethod.Get, catalogUrl);
                catalogRequest.Headers.Add("Authorization", $"Bearer {githubToken}");
                catalogRequest.Headers.Add("Accept", "application/vnd.github+json");
                
                var catalogResponse = await _httpClient.SendAsync(catalogRequest);
                
                if (catalogResponse.IsSuccessStatusCode)
                {
                    // Catalog exists, download and update it
                    var catalogContent = await catalogResponse.Content.ReadAsStringAsync();
                    var catalogJson = JsonConvert.DeserializeObject<dynamic>(catalogContent);
                    existingCatalogSha = catalogJson?.sha;
                    
                    var base64Content = catalogJson?.content?.ToString().Replace("\n", "");
                    var decodedContent = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Content));
                    catalog = JsonConvert.DeserializeObject<RemoteCatalog>(decodedContent) ?? new RemoteCatalog();
                }
                else
                {
                    // Catalog doesn't exist, create new one from local
                    catalog = new RemoteCatalog
                    {
                        Version = "1.0",
                        RepositoryUrl = $"https://github.com/{_repositoryOwner}/{_repositoryName}",
                        Files = new List<RemoteWarehouseFile>(),
                        Packages = new List<RemoteModPackage>()
                    };
                }
                
                // Remove existing entry with same ID if present
                catalog.Files.RemoveAll(f => f.Id == localFile.Id);
                
                // Add new file entry
                var remoteFile = new RemoteWarehouseFile
                {
                    Id = localFile.Id,
                    Name = localFile.Name,
                    Description = localFile.Description,
                    OriginalFileName = localFile.OriginalFileName,
                    FileExtension = localFile.FileExtension,
                    Category = localFile.Category,
                    TargetRelativePath = localFile.TargetRelativePath,
                    FileSizeBytes = localFile.FileSizeBytes,
                    DateAdded = localFile.DateAdded,
                    Tags = localFile.Tags,
                    Author = localFile.Author,
                    Version = localFile.Version,
                    DownloadUrl = downloadUrl,
                    ModPackageId = localFile.ModPackageId
                };
                
                catalog.Files.Add(remoteFile);
                
                // Upload updated catalog
                var catalogJsonContent = JsonConvert.SerializeObject(catalog, Formatting.Indented);
                var catalogBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(catalogJsonContent));
                
                var uploadCatalogUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/contents/catalog.json";
                var uploadCatalogRequest = new HttpRequestMessage(HttpMethod.Put, uploadCatalogUrl);
                uploadCatalogRequest.Headers.Add("Authorization", $"Bearer {githubToken}");
                uploadCatalogRequest.Headers.Add("Accept", "application/vnd.github+json");
                
                var catalogPayload = new
                {
                    message = $"Update catalog with {localFile.Name}",
                    content = catalogBase64,
                    branch = _branch,
                    sha = existingCatalogSha
                };
                
                uploadCatalogRequest.Content = new StringContent(
                    JsonConvert.SerializeObject(catalogPayload),
                    System.Text.Encoding.UTF8,
                    "application/json");
                
                var uploadCatalogResponse = await _httpClient.SendAsync(uploadCatalogRequest);
                
                if (!uploadCatalogResponse.IsSuccessStatusCode)
                {
                    var errorContent = await uploadCatalogResponse.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Failed to update catalog: {uploadCatalogResponse.StatusCode} - {errorContent}");
                }
                
                DownloadProgress?.Invoke(this, "Catalog updated successfully");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update catalog: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Upload a mod package from local warehouse to remote GitHub repository
        /// </summary>
        public async Task UploadPackageAsync(ModPackage package, string githubToken)
        {
            if (string.IsNullOrEmpty(githubToken))
                throw new InvalidOperationException("GitHub token is required for uploading packages");
            
            string? zipPath = null;
            try
            {
                DownloadProgress?.Invoke(this, $"Uploading package {package.Name}...");
                
                // Get all files in the package
                var packageFiles = _localWarehouse.GetPackageFiles(package.Id);
                if (packageFiles.Count == 0)
                {
                    throw new InvalidOperationException($"Package '{package.Name}' has no files to upload");
                }
                
                // Upload each individual file to the files directory
                int uploadedCount = 0;
                foreach (var file in packageFiles)
                {
                    DownloadProgress?.Invoke(this, $"Uploading file {++uploadedCount}/{packageFiles.Count}: {file.Name}");
                    await UploadFileAsync(file, githubToken);
                }
                
                // Create a zip archive of all package files
                DownloadProgress?.Invoke(this, $"Creating package archive...");
                zipPath = Path.Combine(Path.GetTempPath(), $"{package.Id}.zip");
                
                using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    foreach (var file in packageFiles)
                    {
                        if (File.Exists(file.StoragePath))
                        {
                            // Add file to zip with its target relative path as the entry name
                            var entryName = file.TargetRelativePath.Replace("\\", "/");
                            zipArchive.CreateEntryFromFile(file.StoragePath, entryName);
                        }
                    }
                }
                
                // Upload the package archive to packages directory
                DownloadProgress?.Invoke(this, $"Uploading package archive...");
                await UploadPackageArchiveAsync(package.Id, zipPath, githubToken);
                
                // Now update the catalog with package information
                await UpdateRemoteCatalogWithPackageAsync(package, githubToken);
                
                DownloadProgress?.Invoke(this, $"Package {package.Name} uploaded successfully");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to upload package: {ex.Message}", ex);
            }
            finally
            {
                // Clean up temp zip file
                if (zipPath != null && File.Exists(zipPath))
                {
                    try { File.Delete(zipPath); } catch { }
                }
            }
        }
        
        /// <summary>
        /// Upload package archive to packages directory on GitHub
        /// </summary>
        private async Task UploadPackageArchiveAsync(string packageId, string zipPath, string githubToken)
        {
            var fileName = $"{packageId}.zip";
            var uploadUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/contents/packages/{fileName}";
            
            // Read zip file
            var fileBytes = await File.ReadAllBytesAsync(zipPath);
            var base64Content = Convert.ToBase64String(fileBytes);
            
            // Check if file already exists (to get SHA for update)
            var getRequest = new HttpRequestMessage(HttpMethod.Get, uploadUrl);
            getRequest.Headers.Add("Authorization", $"Bearer {githubToken}");
            getRequest.Headers.Add("Accept", "application/vnd.github+json");
            
            var getResponse = await _httpClient.SendAsync(getRequest);
            string? existingSha = null;
            
            if (getResponse.IsSuccessStatusCode)
            {
                var existingContent = await getResponse.Content.ReadAsStringAsync();
                var existingJson = JsonConvert.DeserializeObject<dynamic>(existingContent);
                existingSha = existingJson?.sha;
            }
            
            // Upload or update file
            var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
            uploadRequest.Headers.Add("Authorization", $"Bearer {githubToken}");
            uploadRequest.Headers.Add("Accept", "application/vnd.github+json");
            
            var uploadData = new
            {
                message = existingSha != null ? $"Update package archive {fileName}" : $"Add package archive {fileName}",
                content = base64Content,
                branch = _branch,
                sha = existingSha
            };
            
            var jsonContent = JsonConvert.SerializeObject(uploadData);
            uploadRequest.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            
            var uploadResponse = await _httpClient.SendAsync(uploadRequest);
            
            if (!uploadResponse.IsSuccessStatusCode)
            {
                var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to upload package archive: {uploadResponse.StatusCode} - {errorContent}");
            }
        }
        
        /// <summary>
        /// Update or create remote catalog.json with new package entry
        /// </summary>
        private async Task UpdateRemoteCatalogWithPackageAsync(ModPackage package, string githubToken)
        {
            try
            {
                DownloadProgress?.Invoke(this, "Updating remote catalog with package...");
                
                // Try to get existing catalog
                RemoteCatalog catalog;
                string? existingCatalogSha = null;
                
                var catalogUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/contents/catalog.json?ref={_branch}";
                var catalogRequest = new HttpRequestMessage(HttpMethod.Get, catalogUrl);
                catalogRequest.Headers.Add("Authorization", $"Bearer {githubToken}");
                catalogRequest.Headers.Add("Accept", "application/vnd.github+json");
                
                var catalogResponse = await _httpClient.SendAsync(catalogRequest);
                
                if (catalogResponse.IsSuccessStatusCode)
                {
                    // Catalog exists, download and update it
                    var catalogContent = await catalogResponse.Content.ReadAsStringAsync();
                    var catalogJson = JsonConvert.DeserializeObject<dynamic>(catalogContent);
                    existingCatalogSha = catalogJson?.sha;
                    
                    var base64Content = catalogJson?.content?.ToString().Replace("\n", "");
                    var decodedContent = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Content));
                    catalog = JsonConvert.DeserializeObject<RemoteCatalog>(decodedContent) ?? new RemoteCatalog();
                }
                else
                {
                    // Catalog doesn't exist, create new one
                    catalog = new RemoteCatalog
                    {
                        Version = "1.0",
                        RepositoryUrl = $"https://github.com/{_repositoryOwner}/{_repositoryName}",
                        Files = new List<RemoteWarehouseFile>(),
                        Packages = new List<RemoteModPackage>()
                    };
                }
                
                // Remove existing package entry with same ID if present
                catalog.Packages.RemoveAll(p => p.Id == package.Id);
                
                // Add new package entry
                var remotePackage = new RemoteModPackage
                {
                    Id = package.Id,
                    Name = package.Name,
                    Description = package.Description,
                    Author = package.Author,
                    Version = package.Version,
                    DateAdded = package.DateAdded,
                    Tags = package.Tags,
                    FileIds = package.FileIds,
                    DownloadUrl = $"https://raw.githubusercontent.com/{_repositoryOwner}/{_repositoryName}/{_branch}/packages/{package.Id}.zip"
                };
                
                catalog.Packages.Add(remotePackage);
                
                // Ensure all files in the package are in the catalog
                var packageFiles = _localWarehouse.GetPackageFiles(package.Id);
                foreach (var file in packageFiles)
                {
                    // Check if file already exists in catalog
                    if (!catalog.Files.Any(f => f.Id == file.Id))
                    {
                        var fileName = $"{file.Id}{file.FileExtension}";
                        var downloadUrl = $"https://raw.githubusercontent.com/{_repositoryOwner}/{_repositoryName}/{_branch}/files/{fileName}";
                        
                        var remoteFile = new RemoteWarehouseFile
                        {
                            Id = file.Id,
                            Name = file.Name,
                            Description = file.Description,
                            OriginalFileName = file.OriginalFileName,
                            FileExtension = file.FileExtension,
                            Category = file.Category,
                            TargetRelativePath = file.TargetRelativePath,
                            FileSizeBytes = file.FileSizeBytes,
                            DateAdded = file.DateAdded,
                            Tags = file.Tags,
                            Author = file.Author,
                            Version = file.Version,
                            DownloadUrl = downloadUrl,
                            ModPackageId = package.Id
                        };
                        
                        catalog.Files.Add(remoteFile);
                    }
                    else
                    {
                        // Update ModPackageId for existing files
                        var existingFile = catalog.Files.First(f => f.Id == file.Id);
                        existingFile.ModPackageId = package.Id;
                    }
                }
                
                // Upload updated catalog
                var catalogJsonContent = JsonConvert.SerializeObject(catalog, Formatting.Indented);
                var catalogBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(catalogJsonContent));
                
                var uploadCatalogUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/contents/catalog.json";
                var uploadCatalogRequest = new HttpRequestMessage(HttpMethod.Put, uploadCatalogUrl);
                uploadCatalogRequest.Headers.Add("Authorization", $"Bearer {githubToken}");
                uploadCatalogRequest.Headers.Add("Accept", "application/vnd.github+json");
                
                var catalogPayload = new
                {
                    message = $"Upload package: {package.Name}",
                    content = catalogBase64,
                    branch = _branch,
                    sha = existingCatalogSha
                };
                
                uploadCatalogRequest.Content = new StringContent(
                    JsonConvert.SerializeObject(catalogPayload),
                    System.Text.Encoding.UTF8,
                    "application/json");
                
                var uploadCatalogResponse = await _httpClient.SendAsync(uploadCatalogRequest);
                
                if (!uploadCatalogResponse.IsSuccessStatusCode)
                {
                    var errorContent = await uploadCatalogResponse.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Failed to update catalog: {uploadCatalogResponse.StatusCode} - {errorContent}");
                }
                
                DownloadProgress?.Invoke(this, "Catalog updated successfully with package");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update catalog: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get repository information
        /// </summary>
        public (string owner, string repo, string branch) GetRepositoryInfo()
        {
            return (_repositoryOwner, _repositoryName, _branch);
        }
        
        /// <summary>
        /// Validate that a GitHub token has write access to the repository
        /// </summary>
        public async Task<bool> ValidateGitHubTokenAsync(string githubToken)
        {
            if (string.IsNullOrEmpty(githubToken))
                return false;
            
            try
            {
                // Try to get repository information to validate token
                var url = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {githubToken}");
                request.Headers.Add("Accept", "application/vnd.github+json");
                
                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                    return false;
                
                var content = await response.Content.ReadAsStringAsync();
                var repoInfo = JsonConvert.DeserializeObject<dynamic>(content);
                
                // Check if user has push access
                var permissions = repoInfo?.permissions;
                return permissions?.push == true || permissions?.admin == true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Delete a mod package from the remote repository
        /// This removes the package ZIP file, all individual files, and updates the catalog
        /// </summary>
        public async Task DeletePackageAsync(RemoteModPackage package, string githubToken)
        {
            if (string.IsNullOrEmpty(githubToken))
                throw new InvalidOperationException("GitHub token is required for deleting packages");
            
            if (_remoteCatalog == null)
                throw new InvalidOperationException("Remote catalog not loaded");
            
            // Validate token has write access
            if (!await ValidateGitHubTokenAsync(githubToken))
                throw new UnauthorizedAccessException("GitHub token does not have write access to the repository");
            
            try
            {
                DownloadProgress?.Invoke(this, $"Deleting package '{package.Name}'...");
                
                // Step 1: Delete the package ZIP file
                var packageFileName = $"{package.Id}.zip";
                var packagePath = $"packages/{packageFileName}";
                await DeleteFileFromRepositoryAsync(packagePath, githubToken, $"Delete package: {package.Name}");
                DownloadProgress?.Invoke(this, $"Deleted package ZIP: {packageFileName}");
                
                // Step 2: Delete all associated individual files
                var filesInPackage = _remoteCatalog.Files.Where(f => package.FileIds.Contains(f.Id)).ToList();
                foreach (var file in filesInPackage)
                {
                    var fileName = $"{file.Id}{file.FileExtension}";
                    var filePath = $"files/{fileName}";
                    
                    try
                    {
                        await DeleteFileFromRepositoryAsync(filePath, githubToken, $"Delete file from package: {package.Name}");
                        DownloadProgress?.Invoke(this, $"Deleted file: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        DownloadProgress?.Invoke(this, $"Warning: Could not delete file {fileName}: {ex.Message}");
                    }
                }
                
                // Step 3: Update the catalog to remove the package and its files
                _remoteCatalog.Packages.RemoveAll(p => p.Id == package.Id);
                
                foreach (var fileId in package.FileIds)
                {
                    _remoteCatalog.Files.RemoveAll(f => f.Id == fileId);
                }
                
                await UpdateRemoteCatalogAsync(githubToken, $"Remove package '{package.Name}' from catalog");
                
                DownloadProgress?.Invoke(this, $"Successfully deleted package '{package.Name}' and all associated files");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete package: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Delete a file from the GitHub repository
        /// </summary>
        private async Task DeleteFileFromRepositoryAsync(string repoPath, string githubToken, string commitMessage)
        {
            // Get the current file SHA (required for deletion)
            var checkUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/contents/{repoPath}?ref={_branch}";
            var checkRequest = new HttpRequestMessage(HttpMethod.Get, checkUrl);
            checkRequest.Headers.Add("Authorization", $"Bearer {githubToken}");
            checkRequest.Headers.Add("Accept", "application/vnd.github+json");
            
            var checkResponse = await _httpClient.SendAsync(checkRequest);
            
            if (!checkResponse.IsSuccessStatusCode)
            {
                // File doesn't exist, consider it already deleted
                return;
            }
            
            var existingContent = await checkResponse.Content.ReadAsStringAsync();
            var existingJson = JsonConvert.DeserializeObject<dynamic>(existingContent);
            var sha = existingJson?.sha?.ToString();
            
            if (string.IsNullOrEmpty(sha))
                throw new InvalidOperationException($"Could not get SHA for file: {repoPath}");
            
            // Delete the file
            var deleteUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/contents/{repoPath}";
            var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
            deleteRequest.Headers.Add("Authorization", $"Bearer {githubToken}");
            deleteRequest.Headers.Add("Accept", "application/vnd.github+json");
            
            var deletePayload = new
            {
                message = commitMessage,
                sha = sha,
                branch = _branch
            };
            
            var jsonPayload = JsonConvert.SerializeObject(deletePayload);
            deleteRequest.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            
            var deleteResponse = await _httpClient.SendAsync(deleteRequest);
            
            if (!deleteResponse.IsSuccessStatusCode)
            {
                var errorContent = await deleteResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to delete file {repoPath}: {deleteResponse.StatusCode} - {errorContent}");
            }
        }
        
        /// <summary>
        /// Update the remote catalog file on GitHub
        /// </summary>
        private async Task UpdateRemoteCatalogAsync(string githubToken, string commitMessage)
        {
            if (_remoteCatalog == null)
                throw new InvalidOperationException("Remote catalog not loaded");
            
            // Serialize the updated catalog
            var catalogJson = JsonConvert.SerializeObject(_remoteCatalog, Formatting.Indented);
            var base64Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(catalogJson));
            
            // Get current catalog SHA
            var catalogPath = "catalog.json";
            var checkUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/contents/{catalogPath}?ref={_branch}";
            var checkRequest = new HttpRequestMessage(HttpMethod.Get, checkUrl);
            checkRequest.Headers.Add("Authorization", $"Bearer {githubToken}");
            checkRequest.Headers.Add("Accept", "application/vnd.github+json");
            
            var checkResponse = await _httpClient.SendAsync(checkRequest);
            
            string? existingSha = null;
            if (checkResponse.IsSuccessStatusCode)
            {
                var existingContent = await checkResponse.Content.ReadAsStringAsync();
                var existingJson = JsonConvert.DeserializeObject<dynamic>(existingContent);
                existingSha = existingJson?.sha;
            }
            
            // Update catalog
            var updateUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/contents/{catalogPath}";
            var updateRequest = new HttpRequestMessage(HttpMethod.Put, updateUrl);
            updateRequest.Headers.Add("Authorization", $"Bearer {githubToken}");
            updateRequest.Headers.Add("Accept", "application/vnd.github+json");
            
            var updatePayload = new
            {
                message = commitMessage,
                content = base64Content,
                branch = _branch,
                sha = existingSha
            };
            
            var jsonPayload = JsonConvert.SerializeObject(updatePayload);
            updateRequest.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            
            var updateResponse = await _httpClient.SendAsync(updateRequest);
            
            if (!updateResponse.IsSuccessStatusCode)
            {
                var errorContent = await updateResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to update catalog: {updateResponse.StatusCode} - {errorContent}");
            }
        }
    }
}
