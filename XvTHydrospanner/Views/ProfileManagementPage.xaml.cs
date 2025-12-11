using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using XvTHydrospanner.Models;
using XvTHydrospanner.Services;

namespace XvTHydrospanner.Views
{
    public partial class ProfileManagementPage : Page
    {
        private readonly ProfileManager _profileManager;
        private readonly WarehouseManager _warehouseManager;
        private ModProfile? _selectedProfile;
        
        public ProfileManagementPage(ProfileManager profileManager, WarehouseManager warehouseManager)
        {
            InitializeComponent();
            _profileManager = profileManager;
            _warehouseManager = warehouseManager;
            
            // Subscribe to Loaded event to refresh when page is shown
            Loaded += ProfileManagementPage_Loaded;
            
            LoadProfiles();
        }
        
        private void ProfileManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Refresh profiles when page loads/becomes visible
            LoadProfiles();
        }
        
        public void LoadProfiles()
        {
            var profiles = _profileManager.GetAllProfiles();
            ProfilesListBox.ItemsSource = profiles;
            
            // Priority 1: Select the active profile if it exists
            var activeProfile = _profileManager.GetActiveProfile();
            if (activeProfile != null)
            {
                var activeInList = profiles.Find(p => p.Id == activeProfile.Id);
                if (activeInList != null)
                {
                    ProfilesListBox.SelectedItem = activeInList;
                    
                    // Update active indicators after UI has rendered
                    Dispatcher.BeginInvoke(new Action(() => UpdateActiveProfileIndicators()), 
                        System.Windows.Threading.DispatcherPriority.Loaded);
                    return;
                }
            }
            
            // Priority 2: Try to maintain the current selection if it still exists
            if (_selectedProfile != null)
            {
                var stillExists = profiles.Find(p => p.Id == _selectedProfile.Id);
                if (stillExists != null)
                {
                    ProfilesListBox.SelectedItem = stillExists;
                    
                    // Update active indicators after UI has rendered
                    Dispatcher.BeginInvoke(new Action(() => UpdateActiveProfileIndicators()), 
                        System.Windows.Threading.DispatcherPriority.Loaded);
                    return;
                }
            }
            
            // Priority 3: Fall back to first item
            if (ProfilesListBox.Items.Count > 0)
            {
                ProfilesListBox.SelectedIndex = 0;
            }
            
            // Update active indicators after UI has rendered
            Dispatcher.BeginInvoke(new Action(() => UpdateActiveProfileIndicators()), 
                System.Windows.Threading.DispatcherPriority.Loaded);
        }
        
        public ModProfile? GetSelectedProfile()
        {
            return _selectedProfile;
        }
        
        private void ProfilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfilesListBox.SelectedItem is ModProfile profile)
            {
                _selectedProfile = profile;
                ProfileDetailsPanel.DataContext = profile;
                ProfileDetailsPanel.Visibility = Visibility.Visible;
                LoadModPackagesForProfile(profile);
            }
        }
        
        private void LoadModPackagesForProfile(ModProfile profile)
        {
            // Get unique package IDs from the profile's file modifications
            var packageIds = new System.Collections.Generic.HashSet<string>();
            
            foreach (var modification in profile.FileModifications)
            {
                var file = _warehouseManager.GetFile(modification.WarehouseFileId);
                if (file != null && !string.IsNullOrEmpty(file.ModPackageId))
                {
                    packageIds.Add(file.ModPackageId);
                }
            }
            
            // Get the actual package objects
            var packages = _warehouseManager.GetAllPackages()
                .Where(p => packageIds.Contains(p.Id))
                .ToList();
            
            ModPackagesListBox.ItemsSource = packages;
        }
        
        private void UpdateActiveProfileIndicators()
        {
            var activeProfile = _profileManager.GetActiveProfile();
            
            // Update visibility of checkmarks in the list
            for (int i = 0; i < ProfilesListBox.Items.Count; i++)
            {
                if (ProfilesListBox.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem item)
                {
                    var profile = ProfilesListBox.Items[i] as ModProfile;
                    var checkmark = FindVisualChild<System.Windows.Controls.TextBlock>(item, "ActiveCheckmark");
                    
                    if (checkmark != null)
                    {
                        checkmark.Visibility = (profile != null && activeProfile != null && profile.Id == activeProfile.Id) 
                            ? Visibility.Visible 
                            : Visibility.Collapsed;
                    }
                }
            }
        }
        
        private static T? FindVisualChild<T>(System.Windows.DependencyObject parent, string name) where T : System.Windows.FrameworkElement
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild && typedChild.Name == name)
                {
                    return typedChild;
                }
                
                var result = FindVisualChild<T>(child, name);
                if (result != null)
                    return result;
            }
            
            return null;
        }
        
        private async void NewProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new NewProfileDialog();
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true)
                {
                    var profile = await _profileManager.CreateProfileAsync(dialog.ProfileName, dialog.ProfileDescription);
                    LoadProfiles();
                    
                    // Select the newly created profile
                    ProfilesListBox.SelectedItem = profile;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating profile: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void CloneProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedProfile == null)
                {
                    MessageBox.Show("Please select a profile to clone.", "Info", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                var dialog = new NewProfileDialog($"{_selectedProfile.Name} (Copy)");
                if (dialog.ShowDialog() == true)
                {
                    await _profileManager.CloneProfileAsync(_selectedProfile.Id, dialog.ProfileName);
                    LoadProfiles();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cloning profile: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProfile == null)
            {
                MessageBox.Show("Please select a profile to delete.", "Info", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var result = MessageBox.Show(
                $"Are you sure you want to delete profile '{_selectedProfile.Name}'?", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _profileManager.DeleteProfileAsync(_selectedProfile.Id);
                    LoadProfiles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting profile: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        

    }
}
