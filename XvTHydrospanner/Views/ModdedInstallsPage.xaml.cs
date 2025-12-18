using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using XvTHydrospanner.Services;

namespace XvTHydrospanner.Views
{
    /// <summary>
    /// Represents a modded game installation branch
    /// </summary>
    public class ModdedInstall : INotifyPropertyChanged
    {
        public string BranchName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public partial class ModdedInstallsPage : Page
    {
        private readonly ModdedInstallsManager _manager;
        
        public ModdedInstallsPage(ModdedInstallsManager manager)
        {
            InitializeComponent();
            _manager = manager;
            
            // Subscribe to progress events
            _manager.ProgressMessage += OnProgressMessage;
            
            Loaded += ModdedInstallsPage_Loaded;
        }
        
        private async void ModdedInstallsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadInstallsAsync();
        }
        
        private async System.Threading.Tasks.Task LoadInstallsAsync()
        {
            try
            {
                StatusPanel.Visibility = Visibility.Visible;
                InstallsScrollViewer.Visibility = Visibility.Collapsed;
                RefreshButton.Visibility = Visibility.Collapsed;
                StatusText.Text = "Loading modded installs...";
                
                var branches = await _manager.GetAvailableBranchesAsync();
                
                if (branches.Count == 0)
                {
                    StatusText.Text = "No modded installs available";
                    RefreshButton.Visibility = Visibility.Visible;
                    return;
                }
                
                var installs = new List<ModdedInstall>();
                foreach (var branch in branches)
                {
                    // Display "Base Game Installation" for main branch
                    var displayName = branch == "main" ? "Base Game Installation" : branch;
                    installs.Add(new ModdedInstall
                    {
                        BranchName = branch,
                        DisplayName = displayName,
                        Description = branch == "main" 
                            ? "Clean base game installation" 
                            : $"Modded installation: {branch}"
                    });
                }
                
                InstallsItemsControl.ItemsSource = installs;
                StatusPanel.Visibility = Visibility.Collapsed;
                InstallsScrollViewer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                RefreshButton.Visibility = Visibility.Visible;
            }
        }
        
        private void OnProgressMessage(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
            });
        }
        
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ModdedInstall install)
            {
                try
                {
                    var result = MessageBox.Show(
                        $"Download and install '{install.DisplayName}'?\n\n" +
                        "This will download the complete game installation from the remote repository.\n" +
                        "Choose a destination folder for the installation.",
                        "Download Install",
                        MessageBoxButton.OKCancel,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.OK)
                    {
                        // Show folder picker
                        using var dialog = new System.Windows.Forms.FolderBrowserDialog();
                        dialog.Description = $"Select destination folder for '{install.DisplayName}'";
                        dialog.ShowNewFolderButton = true;
                        
                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            button.IsEnabled = false;
                            button.Content = "⏳ Downloading...";
                            
                            await _manager.DownloadBranchAsync(install.BranchName, dialog.SelectedPath);
                            
                            MessageBox.Show(
                                $"Successfully downloaded '{install.DisplayName}' to:\n{dialog.SelectedPath}",
                                "Download Complete",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            
                            button.Content = "⬇ Download";
                            button.IsEnabled = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error downloading install: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    button.Content = "⬇ Download";
                    button.IsEnabled = true;
                }
            }
        }
        
        private async void UploadInstallButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show folder picker
                using var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "Select game installation folder to upload";
                dialog.ShowNewFolderButton = false;
                
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Ask for branch name
                    var branchDialog = new InputDialog("Upload Installation", 
                        "Enter a name for this modded installation:\n(This will be the branch name)");
                    branchDialog.Owner = Window.GetWindow(this);
                    
                    if (branchDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(branchDialog.InputText))
                    {
                        var branchName = branchDialog.InputText.Trim();
                        
                        // Validate branch name
                        if (branchName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0 ||
                            branchName.Contains(" "))
                        {
                            MessageBox.Show("Branch name cannot contain spaces or special characters.\n" +
                                          "Use hyphens or underscores instead.",
                                "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        
                        var result = MessageBox.Show(
                            $"Upload folder:\n{dialog.SelectedPath}\n\n" +
                            $"As branch: '{branchName}'\n\n" +
                            "This will create a new branch in the Modded Installs repository and upload all files.\n" +
                            "This may take some time depending on the installation size.\n\n" +
                            "Continue?",
                            "Confirm Upload",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        
                        if (result == MessageBoxResult.Yes)
                        {
                            StatusPanel.Visibility = Visibility.Visible;
                            InstallsScrollViewer.Visibility = Visibility.Collapsed;
                            StatusText.Text = "Uploading installation...";
                            
                            await _manager.UploadInstallationAsync(dialog.SelectedPath, branchName);
                            
                            MessageBox.Show(
                                $"Successfully uploaded installation as branch '{branchName}'",
                                "Upload Complete",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            
                            // Refresh the list
                            await LoadInstallsAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error uploading installation: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                await LoadInstallsAsync();
            }
        }
        
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadInstallsAsync();
        }
    }
}

