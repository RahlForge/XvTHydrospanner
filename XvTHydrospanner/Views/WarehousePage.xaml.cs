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
        
        public WarehousePage(WarehouseManager warehouseManager)
        {
            InitializeComponent();
            _warehouseManager = warehouseManager;
            LoadFiles();
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
                                customFileLocations: customLocations
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
    }
}
