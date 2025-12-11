using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using XvTHydrospanner.Models;
using XvTHydrospanner.Services;

namespace XvTHydrospanner.Views
{
    public partial class RemoteModsPage : Page
    {
        private readonly RemoteWarehouseManager _remoteWarehouse;
        private List<RemoteModPackage> _allRemotePackages = new();
        
        public RemoteModsPage(RemoteWarehouseManager remoteWarehouse)
        {
            InitializeComponent();
            _remoteWarehouse = remoteWarehouse;
            
            // Subscribe to events
            _remoteWarehouse.DownloadProgress += OnDownloadProgress;
            _remoteWarehouse.PackageDownloaded += OnPackageDownloaded;
            
            _ = LoadRemoteCatalogAsync();
        }
        
        private async System.Threading.Tasks.Task LoadRemoteCatalogAsync()
        {
            try
            {
                StatusText.Text = "Loading remote catalog...";
                RefreshButton.IsEnabled = false;
                
                var catalog = await _remoteWarehouse.LoadRemoteCatalogAsync();
                
                if (catalog == null)
                {
                    MessageBox.Show("Remote catalog is null. Please check your internet connection and repository settings.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Failed to load remote catalog";
                    return;
                }
                
                RepositoryText.Text = catalog.RepositoryUrl;
                _allRemotePackages = catalog.Packages ?? new List<RemoteModPackage>();
                
                UpdatePackagesList();
                
                var packagesCount = _allRemotePackages.Count;
                var filesCount = catalog.Files?.Count ?? 0;
                
                // Build status message
                var statusParts = new List<string>();
                if (packagesCount > 0)
                    statusParts.Add($"{packagesCount} package(s)");
                if (filesCount > 0)
                    statusParts.Add($"{filesCount} individual file(s)");
                
                StatusText.Text = statusParts.Count > 0 
                    ? $"Loaded {string.Join(" and ", statusParts)} from remote catalog"
                    : "Remote catalog is empty";
                
                if (packagesCount == 0 && filesCount == 0)
                {
                    MessageBox.Show("The remote catalog is empty. No packages or files available for download.", 
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load remote catalog: {ex.Message}\n\nStack trace: {ex.StackTrace}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Failed to load remote catalog";
            }
            finally
            {
                RefreshButton.IsEnabled = true;
            }
        }
        
        private void UpdatePackagesList()
        {
            var searchTerm = SearchBox.Text?.ToLower() ?? string.Empty;
            
            var filteredPackages = _allRemotePackages.AsEnumerable();
            
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filteredPackages = filteredPackages.Where(p =>
                    (p.Name?.ToLower().Contains(searchTerm) ?? false) ||
                    (p.Description?.ToLower().Contains(searchTerm) ?? false) ||
                    (p.Author?.ToLower().Contains(searchTerm) ?? false));
            }
            
            var filteredList = filteredPackages.ToList();
            RemotePackagesGrid.ItemsSource = filteredList;
            
            // Update status with count
            var totalCount = _allRemotePackages.Count;
            var displayedCount = filteredList.Count;
            if (totalCount != displayedCount)
            {
                StatusText.Text = $"Showing {displayedCount} of {totalCount} package(s)";
            }
        }
        
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePackagesList();
        }
        
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is RemoteModPackage remotePackage)
            {
                try
                {
                    button.IsEnabled = false;
                    StatusText.Text = $"Downloading package {remotePackage.Name}...";
                    
                    await _remoteWarehouse.DownloadPackageAsync(remotePackage);
                    
                    UpdatePackagesList();
                    StatusText.Text = $"Successfully downloaded package {remotePackage.Name}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to download package: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Download failed";
                    button.IsEnabled = true;
                }
            }
        }
        
        private async void DownloadAllButton_Click(object sender, RoutedEventArgs e)
        {
            var availablePackages = _remoteWarehouse.GetAvailablePackages();
            
            if (availablePackages.Count == 0)
            {
                MessageBox.Show("No new packages available to download.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var result = MessageBox.Show(
                $"Download {availablePackages.Count} available package(s)?",
                "Confirm Download",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
                return;
            
            try
            {
                DownloadAllButton.IsEnabled = false;
                var successCount = 0;
                var failCount = 0;
                
                foreach (var package in availablePackages)
                {
                    try
                    {
                        await _remoteWarehouse.DownloadPackageAsync(package);
                        successCount++;
                    }
                    catch
                    {
                        failCount++;
                    }
                }
                
                UpdatePackagesList();
                MessageBox.Show($"Download complete:\n{successCount} packages succeeded\n{failCount} packages failed",
                    "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText.Text = $"Downloaded {successCount} packages";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Download failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DownloadAllButton.IsEnabled = true;
            }
        }
        
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadRemoteCatalogAsync();
        }
        
        private void OnDownloadProgress(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
            });
        }
        
        private void OnPackageDownloaded(object? sender, RemoteModPackage package)
        {
            Dispatcher.Invoke(() =>
            {
                _remoteWarehouse.RefreshDownloadStatus();
                UpdatePackagesList();
            });
        }
        
        private async void ContextMenu_Download_Click(object sender, RoutedEventArgs e)
        {
            if (RemotePackagesGrid.SelectedItem is RemoteModPackage remotePackage)
            {
                if (remotePackage.IsDownloaded)
                {
                    MessageBox.Show($"Package '{remotePackage.Name}' is already downloaded.", "Already Downloaded",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                try
                {
                    StatusText.Text = $"Downloading package {remotePackage.Name}...";
                    await _remoteWarehouse.DownloadPackageAsync(remotePackage);
                    
                    UpdatePackagesList();
                    StatusText.Text = $"Successfully downloaded package {remotePackage.Name}";
                    MessageBox.Show($"Package '{remotePackage.Name}' downloaded successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to download package: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Download failed";
                }
            }
        }
    }
}
