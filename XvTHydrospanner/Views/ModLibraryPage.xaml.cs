using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using XvTHydrospanner.Models;
using XvTHydrospanner.Services;

namespace XvTHydrospanner.Views
{
    /// <summary>
    /// Wrapper class for ModPackage that includes active status for display
    /// </summary>
    public class ModPackageViewModel : INotifyPropertyChanged
    {
        private readonly ModPackage _package;
        private bool _isActiveInProfile;

        public ModPackageViewModel(ModPackage package, bool isActiveInProfile)
        {
            _package = package;
            _isActiveInProfile = isActiveInProfile;
        }

        public ModPackage Package => _package;
        
        // Expose ModPackage properties for binding
        public string Id => _package.Id;
        public string Name => _package.Name;
        public string Description => _package.Description;
        public string? Author => _package.Author;
        public string? Version => _package.Version;
        public DateTime DateAdded => _package.DateAdded;
        public List<string> Tags => _package.Tags;
        public List<string> FileIds => _package.FileIds;

        public bool IsActiveInProfile
        {
            get => _isActiveInProfile;
            set
            {
                if (_isActiveInProfile != value)
                {
                    _isActiveInProfile = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class ModLibraryPage : Page
    {
        private readonly WarehouseManager _warehouseManager;
        private readonly ProfileManager _profileManager;
        private readonly ModApplicator? _modApplicator;
        
        public ModLibraryPage(WarehouseManager warehouseManager, ProfileManager profileManager, ModApplicator? modApplicator = null)
        {
            InitializeComponent();
            _warehouseManager = warehouseManager;
            _profileManager = profileManager;
            _modApplicator = modApplicator;
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
                
                // Get active profile to check which packages are active
                var activeProfile = _profileManager.GetActiveProfile();
                var activeFileIds = activeProfile?.FileModifications
                    .Select(fm => fm.WarehouseFileId)
                    .ToHashSet() ?? new HashSet<string>();
                
                // Create view models with active status
                var packageViewModels = packages.Select(package =>
                {
                    // A package is active if any of its files are in the active profile
                    var isActive = package.FileIds.Any(fileId => activeFileIds.Contains(fileId));
                    return new ModPackageViewModel(package, isActive);
                }).ToList();
                
                ModsItemsControl.ItemsSource = packageViewModels;
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
            if (sender is Button button)
            {
                ModPackage? package = null;
                
                // Handle both ModPackage and ModPackageViewModel
                if (button.Tag is ModPackage modPackage)
                {
                    package = modPackage;
                }
                else if (button.Tag is ModPackageViewModel viewModel)
                {
                    package = viewModel.Package;
                }
                
                if (package != null)
                {
                    var dialog = new ModManagementDialog(package, _warehouseManager, _profileManager, _modApplicator);
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
}
