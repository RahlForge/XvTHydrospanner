using System;
using System.Linq;
using System.Windows;
using XvTHydrospanner.Models;
using XvTHydrospanner.Services;

namespace XvTHydrospanner.Views
{
    public partial class ModManagementDialog
    {
        private readonly ModPackage _package;
        private readonly WarehouseManager _warehouseManager;
        private readonly ProfileManager _profileManager;
        private readonly ModApplicator? _modApplicator;
        private ModProfile? _activeProfile;
        
        public ModManagementDialog(ModPackage package, WarehouseManager warehouseManager, ProfileManager profileManager, ModApplicator? modApplicator = null)
        {
            InitializeComponent();
            _package = package;
            _warehouseManager = warehouseManager;
            _profileManager = profileManager;
            _modApplicator = modApplicator;
            
            LoadModInfo();
            LoadFiles();
            UpdateButtonStates();
        }
        
        private void LoadModInfo()
        {
            ModNameTextBlock.Text = _package.Name;
            ModDescriptionTextBlock.Text = _package.Description;
            AuthorTextBlock.Text = _package.Author ?? "Unknown";
            VersionTextBlock.Text = _package.Version ?? "N/A";
        }
        
        private void LoadFiles()
        {
            var files = _warehouseManager.GetPackageFiles(_package.Id);
            FilesItemsControl.ItemsSource = files;
        }
        
        private void UpdateButtonStates()
        {
            _activeProfile = _profileManager.GetActiveProfile();
            
            if (_activeProfile == null)
            {
                AddToProfileButton.IsEnabled = false;
                RemoveFromProfileButton.IsEnabled = false;
                return;
            }
            
            // Check if any files from this package are in the active profile
            var packageFiles = _warehouseManager.GetPackageFiles(_package.Id);
            var filesInProfile = _activeProfile.FileModifications
                .Where(fm => packageFiles.Any(pf => pf.Id == fm.WarehouseFileId))
                .ToList();
            
            AddToProfileButton.IsEnabled = filesInProfile.Count < packageFiles.Count;
            RemoveFromProfileButton.IsEnabled = filesInProfile.Count > 0;
        }
        
        private async void AddToProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_activeProfile == null)
                {
                    MessageBox.Show("No active profile selected.", "Info", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                var packageFiles = _warehouseManager.GetPackageFiles(_package.Id);
                var addedModifications = new System.Collections.Generic.List<FileModification>();
                
                foreach (var file in packageFiles)
                {
                    var existingMod = _activeProfile.FileModifications
                        .FirstOrDefault(fm => fm.WarehouseFileId == file.Id);
                    
                    if (existingMod == null)
                    {
                        var modification = new FileModification
                        {
                            WarehouseFileId = file.Id,
                            RelativeGamePath = file.TargetRelativePath,
                            Category = file.Category,
                            IsApplied = false
                        };
                        
                        _activeProfile.FileModifications.Add(modification);
                        addedModifications.Add(modification);
                    }
                }
                
                if (addedModifications.Count > 0)
                {
                    await _profileManager.SaveProfileAsync(_activeProfile);
                    
                    // Apply the modifications to the game if ModApplicator is available
                    if (_modApplicator != null)
                    {
                        var result = MessageBox.Show(
                            $"Added {addedModifications.Count} file(s) from '{_package.Name}' to profile '{_activeProfile.Name}'.\n\n" +
                            "Apply these modifications to the game now?",
                            "Apply Modifications", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        
                        if (result == MessageBoxResult.Yes)
                        {
                            int successCount = 0;
                            int failedCount = 0;
                            
                            foreach (var modification in addedModifications)
                            {
                                var applied = await _modApplicator.ApplyModificationAsync(modification, true);
                                if (applied)
                                    successCount++;
                                else
                                    failedCount++;
                            }
                            
                            await _profileManager.SaveProfileAsync(_activeProfile);
                            
                            MessageBox.Show(
                                $"Applied modifications:\n{successCount} succeeded, {failedCount} failed",
                                "Apply Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Added {addedModifications.Count} file(s) from '{_package.Name}' to profile '{_activeProfile.Name}'.\n\n" +
                            "Use the 'Apply Profile' button to apply changes to the game.",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    
                    UpdateButtonStates();
                }
                else
                {
                    MessageBox.Show("All files from this mod are already in the profile.", 
                        "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding mod to profile: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void RemoveFromProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_activeProfile == null)
                {
                    MessageBox.Show("No active profile selected.", "Info", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                var result = MessageBox.Show(
                    $"Remove all files from '{_package.Name}' from the active profile?\n\n" +
                    "Note: If this mod contains LST files, you should re-apply the profile\n" +
                    "to rebuild LST files correctly for multiplayer compatibility.",
                    "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result != MessageBoxResult.Yes)
                    return;
                
                var packageFiles = _warehouseManager.GetPackageFiles(_package.Id);
                var removedModifications = new System.Collections.Generic.List<FileModification>();
                
                foreach (var file in packageFiles)
                {
                    var modification = _activeProfile.FileModifications
                        .FirstOrDefault(fm => fm.WarehouseFileId == file.Id);
                    
                    if (modification != null)
                    {
                        _activeProfile.FileModifications.Remove(modification);
                        removedModifications.Add(modification);
                    }
                }
                
                if (removedModifications.Count > 0)
                {
                    await _profileManager.SaveProfileAsync(_activeProfile);
                    
                    // Revert the modifications from the game if ModApplicator is available
                    if (_modApplicator != null)
                    {
                        var revertResult = MessageBox.Show(
                            $"Removed {removedModifications.Count} file(s) from profile '{_activeProfile.Name}'.\n\n" +
                            "Revert these modifications from the game now?\n" +
                            "(Recommended: Re-apply profile instead to properly rebuild LST files)",
                            "Revert Modifications", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        
                        if (revertResult == MessageBoxResult.Yes)
                        {
                            int successCount = 0;
                            int failedCount = 0;
                            
                            foreach (var modification in removedModifications)
                            {
                                var reverted = await _modApplicator.RevertModificationAsync(modification);
                                if (reverted)
                                    successCount++;
                                else
                                    failedCount++;
                            }
                            
                            MessageBox.Show(
                                $"Reverted modifications:\n{successCount} succeeded, {failedCount} failed\n\n" +
                                "Recommendation: Use 'Apply Profile' button to ensure LST files are correct.",
                                "Revert Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Removed {removedModifications.Count} file(s) from profile '{_activeProfile.Name}'.\n\n" +
                            "Use the 'Apply Profile' button to update the game.",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    
                    UpdateButtonStates();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing mod from profile: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    $"Delete mod package '{_package.Name}' and all its files from the warehouse?\n\nThis action cannot be undone.",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result != MessageBoxResult.Yes)
                    return;
                
                await _warehouseManager.RemovePackageAsync(_package.Id, removeFiles: true);
                
                MessageBox.Show($"Mod package '{_package.Name}' has been deleted.", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting mod package: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
