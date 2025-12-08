using System;
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
            LoadProfiles();
        }
        
        private void LoadProfiles()
        {
            ProfilesListBox.ItemsSource = _profileManager.GetAllProfiles();
            if (ProfilesListBox.Items.Count > 0)
            {
                ProfilesListBox.SelectedIndex = 0;
            }
        }
        
        private void ProfilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfilesListBox.SelectedItem is ModProfile profile)
            {
                _selectedProfile = profile;
                ProfileDetailsPanel.DataContext = profile;
                ProfileDetailsPanel.Visibility = Visibility.Visible;
                ModificationsListBox.ItemsSource = profile.FileModifications;
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
        
        private void AddModButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProfile == null) return;
            
            var dialog = new AddModificationDialog(_warehouseManager);
            if (dialog.ShowDialog() == true && dialog.SelectedWarehouseFile != null)
            {
                var modification = new FileModification
                {
                    RelativeGamePath = dialog.TargetPath,
                    WarehouseFileId = dialog.SelectedWarehouseFile.Id,
                    Category = dialog.SelectedWarehouseFile.Category,
                    Description = dialog.ModDescription
                };
                
                _selectedProfile.FileModifications.Add(modification);
                _ = _profileManager.SaveProfileAsync(_selectedProfile);
                ModificationsListBox.ItemsSource = null;
                ModificationsListBox.ItemsSource = _selectedProfile.FileModifications;
            }
        }
        
        private async void RemoveModButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedProfile == null) return;
                
                if (sender is Button button && button.Tag is FileModification modification)
                {
                    _selectedProfile.FileModifications.Remove(modification);
                    await _profileManager.SaveProfileAsync(_selectedProfile);
                    ModificationsListBox.ItemsSource = null;
                    ModificationsListBox.ItemsSource = _selectedProfile.FileModifications;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing modification: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
