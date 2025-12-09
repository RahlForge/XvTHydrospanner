using System;
using System.Collections.Generic;
using System.Linq;
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
            var files = _warehouseManager.GetAllFiles();
            var packages = _warehouseManager.GetAllPackages();
            
            var viewModels = files.Select(file =>
            {
                string? modPackageName = null;
                if (file.ModPackageId != null)
                {
                    var package = packages.FirstOrDefault(p => p.Id == file.ModPackageId);
                    modPackageName = package?.Name;
                }
                return WarehouseFileViewModel.FromWarehouseFile(file, modPackageName);
            }).ToList();
            
            WarehouseDataGrid.ItemsSource = viewModels;
        }
        
        private async void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select file or archive to add to warehouse",
                Filter = "All Files (*.*)|*.*|Archives (*.zip;*.rar;*.7z)|*.zip;*.rar;*.7z|Mission Files (*.TIE)|*.TIE|List Files (*.LST)|*.LST",
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
                    // Single file
                    var dialog = new AddWarehouseFileDialog(filePath);
                    if (dialog.ShowDialog() == true)
                    {
                        try
                        {
                            await _warehouseManager.AddFileAsync(
                                filePath,
                                dialog.FileName,
                                dialog.FileDescription,
                                dialog.FileCategory,
                                dialog.TargetPath,
                                dialog.Author,
                                dialog.Version,
                                dialog.Tags
                            );
                            LoadFiles();
                            MessageBox.Show("File added to warehouse successfully.", "Success", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error adding file: {ex.Message}", "Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }
        
        private async void DeleteFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WarehouseFileViewModel fileViewModel)
            {
                var result = MessageBox.Show(
                    $"Delete '{fileViewModel.Name}' from warehouse?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _warehouseManager.RemoveFileAsync(fileViewModel.Id);
                        LoadFiles();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting file: {ex.Message}", "Error", 
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
                var files = _warehouseManager.Search(searchTerm);
                var packages = _warehouseManager.GetAllPackages();
                
                var viewModels = files.Select(file =>
                {
                    string? modPackageName = null;
                    if (file.ModPackageId != null)
                    {
                        var package = packages.FirstOrDefault(p => p.Id == file.ModPackageId);
                        modPackageName = package?.Name;
                    }
                    return WarehouseFileViewModel.FromWarehouseFile(file, modPackageName);
                }).ToList();
                
                WarehouseDataGrid.ItemsSource = viewModels;
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
            
            var config = _configManager.GetConfig();
            
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
            
            // Get selected file
            if (WarehouseDataGrid.SelectedItem is WarehouseFileViewModel selectedViewModel)
            {
                var selectedFile = _warehouseManager.GetFile(selectedViewModel.Id);
                if (selectedFile == null)
                {
                    MessageBox.Show("Selected file not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                var (owner, repo, branch) = _remoteWarehouseManager.GetRepositoryInfo();
                var confirmResult = MessageBox.Show(
                    $"Upload '{selectedFile.Name}' to remote repository?\n\nRepository: {owner}/{repo}\nBranch: {branch}",
                    "Confirm Upload",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (confirmResult != MessageBoxResult.Yes)
                    return;
                
                try
                {
                    UploadToRemoteButton.IsEnabled = false;
                    await _remoteWarehouseManager.UploadFileAsync(selectedFile, config.GitHubToken);
                    
                    MessageBox.Show($"Successfully uploaded '{selectedFile.Name}' to remote repository!",
                        "Upload Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to upload file: {ex.Message}\n\nMake sure:\n" +
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
            else
            {
                MessageBox.Show("Please select a file to upload.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
