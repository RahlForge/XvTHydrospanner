using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using XvTHydrospanner.Models;
using XvTHydrospanner.Services;

namespace XvTHydrospanner.Views
{
    public partial class WarehousePage
    {
        private readonly WarehouseManager _warehouseManager;
        private readonly RemoteWarehouseManager? _remoteWarehouseManager;
        private readonly ConfigurationManager? _configManager;
        
        public WarehousePage(WarehouseManager warehouseManager, RemoteWarehouseManager? remoteWarehouseManager = null, ConfigurationManager? configManager = null)
        {
            InitializeComponent();
            _warehouseManager = warehouseManager;
            _remoteWarehouseManager = remoteWarehouseManager;
            _configManager = configManager;
            LoadFiles();
            
            // Show/hide upload button based on availability
            if (_remoteWarehouseManager == null || _configManager == null)
            {
                UploadToRemoteButton.Visibility = Visibility.Collapsed;
            }
        }
        
        private void LoadFiles()
        {
            var packages = _warehouseManager.GetAllPackages();
            WarehouseDataGrid.ItemsSource = packages;
        }
        
        private async void AddPackageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select mod archive to import",
                Filter = "Archives (*.zip;*.rar;*.7z)|*.zip;*.rar;*.7z|All Files (*.*)|*.*",
                Multiselect = false
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                var filePath = openFileDialog.FileName;
                
                // Check if it's an archive
                if (ArchiveExtractor.IsArchive(filePath))
                {
                    var packageDialog = new AddModPackageDialog(filePath);
                    if (packageDialog.ShowDialog() == true)
                    {
                        try
                        {
                            // Check for files without folder structure
                            var filesWithoutStructure = WarehouseManager.GetFilesWithoutFolderStructure(filePath);
                            Dictionary<string, List<string>>? customLocations = null;
                            
                            if (filesWithoutStructure.Count > 0)
                            {
                                var locationDialog = new FileLocationPromptDialog(filesWithoutStructure);
                                if (locationDialog.ShowDialog() == true)
                                {
                                    customLocations = locationDialog.FileLocations;
                                }
                                else
                                {
                                    return; // User cancelled location selection
                                }
                            }
                            
                            await _warehouseManager.AddModPackageFromArchiveAsync(
                                filePath,
                                packageDialog.ModName,
                                packageDialog.Description,
                                customFileLocations: customLocations,
                                copyToGameRoot: packageDialog.CopyToGameRoot
                            );
                            LoadFiles();
                            MessageBox.Show($"Mod package '{packageDialog.ModName}' added to warehouse successfully.", "Success", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error adding mod package: {ex.Message}", "Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Selected file is not a supported archive format.\n\nSupported formats: ZIP, RAR, 7z", 
                        "Invalid File", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
        
        private async void DeletePackageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ModPackage package)
            {
                var result = MessageBox.Show(
                    $"Delete mod package '{package.Name}' and all its files from warehouse?\n\nThis will remove {package.FileIds.Count} file(s).",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _warehouseManager.RemovePackageAsync(package.Id, removeFiles: true);
                        LoadFiles();
                        MessageBox.Show($"Package '{package.Name}' deleted successfully.", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting package: {ex.Message}", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTerm = SearchBox.Text;
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                LoadFiles();
            }
            else
            {
                var allPackages = _warehouseManager.GetAllPackages();
                var filtered = allPackages.Where(p =>
                    p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (p.Author != null && p.Author.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                ).ToList();
                
                WarehouseDataGrid.ItemsSource = filtered;
            }
        }
        
        private async void UploadPackageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_remoteWarehouseManager == null || _configManager == null)
            {
                MessageBox.Show("Remote warehouse manager not initialized.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (sender is Button button && button.Tag is ModPackage package)
            {
                await UploadPackage(package);
            }
        }
        
        private async void UploadToRemoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_remoteWarehouseManager == null || _configManager == null)
            {
                MessageBox.Show("Remote warehouse manager not initialized.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // Get selected package
            if (WarehouseDataGrid.SelectedItem is ModPackage selectedPackage)
            {
                await UploadPackage(selectedPackage);
            }
            else
            {
                MessageBox.Show("Please select a mod package to upload.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private async Task UploadPackage(ModPackage package)
        {
            var config = _configManager!.GetConfig();
            
            if (string.IsNullOrWhiteSpace(config.GitHubToken))
            {
                var result = MessageBox.Show(
                    "GitHub Personal Access Token is required to upload mods.\n\nWould you like to configure it now in Settings?",
                    "Token Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);
                
                if (result == MessageBoxResult.Yes)
                {
                    var settingsWindow = new SettingsWindow(_configManager);
                    settingsWindow.ShowDialog();
                    config = _configManager.GetConfig();
                    
                    if (string.IsNullOrWhiteSpace(config.GitHubToken))
                    {
                        return; // Still no token
                    }
                }
                else
                {
                    return;
                }
            }
            
            var (owner, repo, branch) = _remoteWarehouseManager!.GetRepositoryInfo();
            var confirmResult = MessageBox.Show(
                $"Upload mod package '{package.Name}' to remote repository?\n\n" +
                $"Repository: {owner}/{repo}\nBranch: {branch}\n" +
                $"Files to upload: {package.FileIds.Count}",
                "Confirm Upload",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (confirmResult != MessageBoxResult.Yes)
                return;
            
            try
            {
                UploadToRemoteButton.IsEnabled = false;
                await _remoteWarehouseManager.UploadPackageAsync(package, config.GitHubToken);
                
                MessageBox.Show($"Successfully uploaded mod package '{package.Name}' to remote repository!",
                    "Upload Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to upload package: {ex.Message}\n\nMake sure:\n" +
                    "1. Your GitHub token has 'repo' permissions\n" +
                    "2. You have write access to the repository\n" +
                    "3. The repository exists",
                    "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UploadToRemoteButton.IsEnabled = true;
            }
        }
    }
}
