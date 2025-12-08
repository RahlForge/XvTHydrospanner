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
    /// Manages loading, saving, and switching between mod profiles
    /// </summary>
    public class ProfileManager
    {
        private readonly string _profilesPath;
        private List<ModProfile> _profiles = new();
        
        public event EventHandler<ModProfile>? ProfileActivated;
        public event EventHandler<ModProfile>? ProfileCreated;
        public event EventHandler<ModProfile>? ProfileDeleted;
        public event EventHandler<ModProfile>? ProfileUpdated;
        
        public ProfileManager(string profilesPath)
        {
            _profilesPath = profilesPath;
            Directory.CreateDirectory(_profilesPath);
        }
        
        /// <summary>
        /// Load all profiles from disk
        /// </summary>
        public async Task<List<ModProfile>> LoadAllProfilesAsync()
        {
            _profiles.Clear();
            
            var profileFiles = Directory.GetFiles(_profilesPath, "*.json");
            foreach (var file in profileFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var profile = JsonConvert.DeserializeObject<ModProfile>(json);
                    if (profile != null)
                    {
                        _profiles.Add(profile);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue loading other profiles
                    Console.WriteLine($"Error loading profile {file}: {ex.Message}");
                }
            }
            
            return _profiles;
        }
        
        /// <summary>
        /// Save a profile to disk
        /// </summary>
        public async Task SaveProfileAsync(ModProfile profile)
        {
            profile.LastModified = DateTime.Now;
            
            var filePath = Path.Combine(_profilesPath, $"{profile.Id}.json");
            var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json);
            
            var existing = _profiles.FirstOrDefault(p => p.Id == profile.Id);
            if (existing != null)
            {
                _profiles.Remove(existing);
                ProfileUpdated?.Invoke(this, profile);
            }
            else
            {
                ProfileCreated?.Invoke(this, profile);
            }
            
            _profiles.Add(profile);
        }
        
        /// <summary>
        /// Create a new profile
        /// </summary>
        public async Task<ModProfile> CreateProfileAsync(string name, string description = "")
        {
            var profile = new ModProfile
            {
                Name = name,
                Description = description,
                CreatedDate = DateTime.Now,
                LastModified = DateTime.Now
            };
            
            await SaveProfileAsync(profile);
            return profile;
        }
        
        /// <summary>
        /// Delete a profile
        /// </summary>
        public async Task DeleteProfileAsync(string profileId)
        {
            var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
            if (profile == null)
                throw new InvalidOperationException($"Profile {profileId} not found");
            
            if (profile.IsActive)
                throw new InvalidOperationException("Cannot delete the active profile");
            
            var filePath = Path.Combine(_profilesPath, $"{profileId}.json");
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
            }
            
            _profiles.Remove(profile);
            ProfileDeleted?.Invoke(this, profile);
        }
        
        /// <summary>
        /// Get a profile by ID
        /// </summary>
        public ModProfile? GetProfile(string profileId)
        {
            return _profiles.FirstOrDefault(p => p.Id == profileId);
        }
        
        /// <summary>
        /// Get the currently active profile
        /// </summary>
        public ModProfile? GetActiveProfile()
        {
            return _profiles.FirstOrDefault(p => p.IsActive);
        }
        
        /// <summary>
        /// Set a profile as active
        /// </summary>
        public async Task SetActiveProfileAsync(string profileId)
        {
            var currentActive = GetActiveProfile();
            if (currentActive != null)
            {
                currentActive.IsActive = false;
                await SaveProfileAsync(currentActive);
            }
            
            var newActive = GetProfile(profileId);
            if (newActive == null)
                throw new InvalidOperationException($"Profile {profileId} not found");
            
            newActive.IsActive = true;
            await SaveProfileAsync(newActive);
            
            ProfileActivated?.Invoke(this, newActive);
        }
        
        /// <summary>
        /// Clone an existing profile
        /// </summary>
        public async Task<ModProfile> CloneProfileAsync(string sourceProfileId, string newName)
        {
            var source = GetProfile(sourceProfileId);
            if (source == null)
                throw new InvalidOperationException($"Profile {sourceProfileId} not found");
            
            var clone = new ModProfile
            {
                Id = Guid.NewGuid().ToString(),
                Name = newName,
                Description = $"Cloned from {source.Name}",
                CreatedDate = DateTime.Now,
                LastModified = DateTime.Now,
                FileModifications = source.FileModifications.Select(fm => new FileModification
                {
                    Id = Guid.NewGuid().ToString(),
                    RelativeGamePath = fm.RelativeGamePath,
                    WarehouseFileId = fm.WarehouseFileId,
                    Category = fm.Category,
                    Description = fm.Description
                }).ToList(),
                CustomSettings = new Dictionary<string, string>(source.CustomSettings)
            };
            
            await SaveProfileAsync(clone);
            return clone;
        }
        
        /// <summary>
        /// Get all profiles
        /// </summary>
        public List<ModProfile> GetAllProfiles()
        {
            return _profiles.ToList();
        }
    }
}
