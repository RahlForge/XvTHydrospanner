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
    public partial class ModLibraryPage : Page
    {
        private readonly WarehouseManager _warehouseManager;
        private readonly ProfileManager _profileManager;
        
        public ModLibraryPage(WarehouseManager warehouseManager, ProfileManager profileManager)
        {
            InitializeComponent();
            _warehouseManager = warehouseManager;
            _profileManager = profileManager;
            LoadMods();
        }
        
        private void LoadMods()
        {
            var packages = _warehouseManager.GetAllPackages();
            
            if (packages.Count == 0)
            {
                EmptyStatePanel.Visibility = Visibility.Visible;
                ModsItemsControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyStatePanel.Visibility = Visibility.Collapsed;
                ModsItemsControl.Visibility = Visibility.Visible;
                ModsItemsControl.ItemsSource = packages;
            }
        }
        
        private async void AddModButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select mod archive to add",
                Filter = "Archives (*.zip;*.rar;*.7z)|*.zip;*.rar;*.7z|All Files (*.*)|*.*",
                Multiselect = false
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                var filePath = openFileDialog.FileName;
                
                if (ArchiveExtractor.IsArchive(filePath))
                {
                    var packageDialog = new AddModPackageDialog(filePath);
                    packageDialog.Owner = Window.GetWindow(this);
                    
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
                                locationDialog.Owner = Window.GetWindow(this);
                                
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
                            LoadMods();
                            MessageBox.Show($"Mod package '{packageDialog.ModName}' added successfully.", "Success", 
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
        
        private void ManageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ModPackage package)
            {
                var dialog = new ModManagementDialog(package, _warehouseManager, _profileManager);
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true)
                {
                    // Refresh the list if changes were made
                    LoadMods();
                }
            }
        }
    }
}
