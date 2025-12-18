using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XvTHydrospanner.Services
{
    /// <summary>
    /// Manages downloading and uploading complete modded game installations via GitHub branches
    /// </summary>
    public class ModdedInstallsManager
    {
        private readonly HttpClient _httpClient;
        private readonly string _repositoryOwner;
        private readonly string _repositoryName;
        private readonly string? _githubToken;
        
        public event EventHandler<string>? ProgressMessage;
        
        private const string DefaultRepositoryOwner = "RahlForge";
        private const string DefaultRepositoryName = "XvTHydrospanner-Installs";
        
        public ModdedInstallsManager(string? owner = null, string? repo = null, string? githubToken = null)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "XvTHydrospanner-ModManager");
            _repositoryOwner = owner ?? DefaultRepositoryOwner;
            _repositoryName = repo ?? DefaultRepositoryName;
            _githubToken = githubToken;
            
            if (!string.IsNullOrEmpty(_githubToken))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {_githubToken}");
            }
        }
        
        /// <summary>
        /// Get list of available branches (each branch is a different modded install)
        /// </summary>
        public async Task<List<string>> GetAvailableBranchesAsync()
        {
            try
            {
                ProgressMessage?.Invoke(this, "Fetching available installations...");
                
                var url = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/branches";
                var response = await _httpClient.GetStringAsync(url);
                var branches = JsonConvert.DeserializeObject<List<JObject>>(response);
                
                if (branches == null)
                {
                    return new List<string>();
                }
                
                var branchNames = branches
                    .Select(b => b["name"]?.ToString())
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>()
                    .ToList();
                
                ProgressMessage?.Invoke(this, $"Found {branchNames.Count} installation(s)");
                return branchNames;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Failed to fetch branches: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Download a complete installation from a specific branch
        /// </summary>
        public async Task DownloadBranchAsync(string branchName, string destinationPath)
        {
            try
            {
                ProgressMessage?.Invoke(this, $"Downloading {branchName}...");
                
                // Download the branch as a ZIP archive
                var downloadUrl = $"https://github.com/{_repositoryOwner}/{_repositoryName}/archive/refs/heads/{branchName}.zip";
                
                var zipBytes = await _httpClient.GetByteArrayAsync(downloadUrl);
                
                ProgressMessage?.Invoke(this, "Extracting files...");
                
                // Extract to destination
                var tempZipPath = Path.Combine(Path.GetTempPath(), $"{branchName}.zip");
                await File.WriteAllBytesAsync(tempZipPath, zipBytes);
                
                // Create destination directory if it doesn't exist
                Directory.CreateDirectory(destinationPath);
                
                // Extract - GitHub adds a root folder with repo name and branch
                using (var archive = ZipFile.OpenRead(tempZipPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        // Skip the root directory (format: reponame-branchname/)
                        var parts = entry.FullName.Split('/', '\\');
                        if (parts.Length <= 1) continue; // Skip root directory entry
                        
                        // Reconstruct path without the root directory
                        var relativePath = string.Join(Path.DirectorySeparatorChar.ToString(), parts.Skip(1));
                        if (string.IsNullOrEmpty(relativePath)) continue;
                        
                        var destPath = Path.Combine(destinationPath, relativePath);
                        
                        // Create directory if needed
                        var destDir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        
                        // Extract file (skip if it's a directory entry)
                        if (!entry.FullName.EndsWith("/") && !entry.FullName.EndsWith("\\"))
                        {
                            entry.ExtractToFile(destPath, overwrite: true);
                        }
                    }
                }
                
                // Clean up temp file
                File.Delete(tempZipPath);
                
                ProgressMessage?.Invoke(this, "Download complete");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to download branch: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Upload a complete installation as a new branch
        /// Note: This requires a GitHub Personal Access Token with repo permissions
        /// </summary>
        public async Task UploadInstallationAsync(string installPath, string branchName)
        {
            if (string.IsNullOrEmpty(_githubToken))
            {
                throw new InvalidOperationException(
                    "GitHub Personal Access Token is required to upload installations.\n\n" +
                    "Please configure your token in Settings > Remote Repository Settings.");
            }
            
            try
            {
                ProgressMessage?.Invoke(this, "Preparing files...");
                
                // Get all files from the installation directory
                var allFiles = Directory.GetFiles(installPath, "*.*", SearchOption.AllDirectories);
                
                if (allFiles.Length == 0)
                {
                    throw new InvalidOperationException("No files found in the selected directory.");
                }
                
                ProgressMessage?.Invoke(this, $"Uploading {allFiles.Length} files...");
                
                // Get the default branch's latest commit SHA
                var defaultBranchUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/git/refs/heads/main";
                var branchResponse = await _httpClient.GetStringAsync(defaultBranchUrl);
                var branchData = JsonConvert.DeserializeObject<JObject>(branchResponse);
                var baseSha = branchData?["object"]?["sha"]?.ToString();
                
                if (string.IsNullOrEmpty(baseSha))
                {
                    throw new InvalidOperationException("Could not get base commit SHA from main branch.");
                }
                
                // Create a new branch
                ProgressMessage?.Invoke(this, $"Creating branch '{branchName}'...");
                var createBranchUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/git/refs";
                var createBranchData = new
                {
                    @ref = $"refs/heads/{branchName}",
                    sha = baseSha
                };
                var createBranchContent = new StringContent(
                    JsonConvert.SerializeObject(createBranchData),
                    Encoding.UTF8,
                    "application/json");
                var createBranchResult = await _httpClient.PostAsync(createBranchUrl, createBranchContent);
                
                if (!createBranchResult.IsSuccessStatusCode)
                {
                    var errorContent = await createBranchResult.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Failed to create branch: {errorContent}");
                }
                
                // Create blobs and tree entries for all files
                var treeEntries = new List<object>();
                int processedFiles = 0;
                
                foreach (var filePath in allFiles)
                {
                    processedFiles++;
                    var relativePath = Path.GetRelativePath(installPath, filePath).Replace("\\", "/");
                    
                    ProgressMessage?.Invoke(this, $"Uploading file {processedFiles}/{allFiles.Length}: {relativePath}");
                    
                    // Read file and create blob
                    var fileContent = await File.ReadAllBytesAsync(filePath);
                    var base64Content = Convert.ToBase64String(fileContent);
                    
                    var createBlobUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/git/blobs";
                    var blobData = new
                    {
                        content = base64Content,
                        encoding = "base64"
                    };
                    var blobContent = new StringContent(
                        JsonConvert.SerializeObject(blobData),
                        Encoding.UTF8,
                        "application/json");
                    var blobResult = await _httpClient.PostAsync(createBlobUrl, blobContent);
                    
                    if (!blobResult.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException($"Failed to upload file: {relativePath}");
                    }
                    
                    var blobResponse = await blobResult.Content.ReadAsStringAsync();
                    var blobJson = JsonConvert.DeserializeObject<JObject>(blobResponse);
                    var blobSha = blobJson?["sha"]?.ToString();
                    
                    treeEntries.Add(new
                    {
                        path = relativePath,
                        mode = "100644", // Regular file
                        type = "blob",
                        sha = blobSha
                    });
                }
                
                // Create tree
                ProgressMessage?.Invoke(this, "Creating directory tree...");
                var createTreeUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/git/trees";
                var treeData = new
                {
                    tree = treeEntries
                };
                var treeContent = new StringContent(
                    JsonConvert.SerializeObject(treeData),
                    Encoding.UTF8,
                    "application/json");
                var treeResult = await _httpClient.PostAsync(createTreeUrl, treeContent);
                
                if (!treeResult.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Failed to create tree.");
                }
                
                var treeResponse = await treeResult.Content.ReadAsStringAsync();
                var treeJson = JsonConvert.DeserializeObject<JObject>(treeResponse);
                var treeSha = treeJson?["sha"]?.ToString();
                
                // Create commit
                ProgressMessage?.Invoke(this, "Creating commit...");
                var createCommitUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/git/commits";
                var commitData = new
                {
                    message = $"Upload modded installation: {branchName}",
                    tree = treeSha,
                    parents = new[] { baseSha }
                };
                var commitContent = new StringContent(
                    JsonConvert.SerializeObject(commitData),
                    Encoding.UTF8,
                    "application/json");
                var commitResult = await _httpClient.PostAsync(createCommitUrl, commitContent);
                
                if (!commitResult.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Failed to create commit.");
                }
                
                var commitResponse = await commitResult.Content.ReadAsStringAsync();
                var commitJson = JsonConvert.DeserializeObject<JObject>(commitResponse);
                var commitSha = commitJson?["sha"]?.ToString();
                
                // Update branch reference
                ProgressMessage?.Invoke(this, "Finalizing upload...");
                var updateRefUrl = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/git/refs/heads/{branchName}";
                var updateRefData = new
                {
                    sha = commitSha,
                    force = true
                };
                var updateRefContent = new StringContent(
                    JsonConvert.SerializeObject(updateRefData),
                    Encoding.UTF8,
                    "application/json");
                var updateRefResult = await _httpClient.PatchAsync(updateRefUrl, updateRefContent);
                
                if (!updateRefResult.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Failed to update branch reference.");
                }
                
                ProgressMessage?.Invoke(this, "Upload complete!");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to upload installation: {ex.Message}", ex);
            }
        }
    }
}

