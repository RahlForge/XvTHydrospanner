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
        private List<RemoteWarehouseFile> _allRemoteFiles = new();
        
        public RemoteModsPage(RemoteWarehouseManager remoteWarehouse)
        {
            InitializeComponent();
            _remoteWarehouse = remoteWarehouse;
            
            // Subscribe to events
            _remoteWarehouse.DownloadProgress += OnDownloadProgress;
            _remoteWarehouse.FileDownloaded += OnFileDownloaded;
            
            InitializeCategoryFilter();
            _ = LoadRemoteCatalogAsync();
        }
        
        private void InitializeCategoryFilter()
        {
            var categories = new List<string> { "All Categories" };
            categories.AddRange(Enum.GetNames(typeof(ModCategory)));
            CategoryFilter.ItemsSource = categories;
            CategoryFilter.SelectedIndex = 0;
        }
        
        private async System.Threading.Tasks.Task LoadRemoteCatalogAsync()
        {
            try
            {
                StatusText.Text = "Loading remote catalog...";
                RefreshButton.IsEnabled = false;
                
                var catalog = await _remoteWarehouse.LoadRemoteCatalogAsync();
                
                RepositoryText.Text = catalog.RepositoryUrl;
                _allRemoteFiles = catalog.Files;
                
                UpdateFilesList();
                StatusText.Text = $"Loaded {_allRemoteFiles.Count} files from remote catalog";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load remote catalog: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Failed to load remote catalog";
            }
            finally
            {
                RefreshButton.IsEnabled = true;
            }
        }
        
        private void UpdateFilesList()
        {
            var searchTerm = SearchBox.Text.ToLower();
            var categoryFilter = CategoryFilter.SelectedItem as string;
            
            var filteredFiles = _allRemoteFiles.AsEnumerable();
            
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filteredFiles = filteredFiles.Where(f =>
                    f.Name.ToLower().Contains(searchTerm) ||
                    f.Description.ToLower().Contains(searchTerm) ||
                    (f.Author?.ToLower().Contains(searchTerm) ?? false));
            }
            
            // Apply category filter
            if (categoryFilter != "All Categories" && Enum.TryParse<ModCategory>(categoryFilter, out var category))
            {
                filteredFiles = filteredFiles.Where(f => f.Category == category);
            }
            
            RemoteFilesGrid.ItemsSource = filteredFiles.ToList();
        }
        
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilesList();
        }
        
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateFilesList();
        }
        
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is RemoteWarehouseFile remoteFile)
            {
                try
                {
                    button.IsEnabled = false;
                    StatusText.Text = $"Downloading {remoteFile.Name}...";
                    
                    await _remoteWarehouse.DownloadFileAsync(remoteFile);
                    
                    UpdateFilesList();
                    StatusText.Text = $"Successfully downloaded {remoteFile.Name}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to download file: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Download failed";
                    button.IsEnabled = true;
                }
            }
        }
        
        private async void DownloadAllButton_Click(object sender, RoutedEventArgs e)
        {
            var availableFiles = _remoteWarehouse.GetAvailableFiles();
            
            if (availableFiles.Count == 0)
            {
                MessageBox.Show("No new files available to download.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var result = MessageBox.Show(
                $"Download {availableFiles.Count} available file(s)?",
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
                
                foreach (var file in availableFiles)
                {
                    try
                    {
                        await _remoteWarehouse.DownloadFileAsync(file);
                        successCount++;
                    }
                    catch
                    {
                        failCount++;
                    }
                }
                
                UpdateFilesList();
                MessageBox.Show($"Download complete:\n{successCount} succeeded\n{failCount} failed",
                    "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText.Text = $"Downloaded {successCount} files";
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
        
        private void ViewPackagesButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to packages view
            MessageBox.Show("Package view coming soon!", "Info", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void OnDownloadProgress(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
            });
        }
        
        private void OnFileDownloaded(object? sender, RemoteWarehouseFile file)
        {
            Dispatcher.Invoke(() =>
            {
                _remoteWarehouse.RefreshDownloadStatus();
                UpdateFilesList();
            });
        }
    }
}
