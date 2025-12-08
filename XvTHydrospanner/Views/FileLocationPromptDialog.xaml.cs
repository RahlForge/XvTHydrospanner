using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace XvTHydrospanner.Views
{
    public partial class FileLocationPromptDialog : Window
    {
        public class FileLocationItem
        {
            public string FileName { get; set; } = "";
            public ObservableCollection<string> AvailableLocations { get; set; } = new();
            public ObservableCollection<string> SelectedLocations { get; set; } = new();
        }
        
        private ObservableCollection<FileLocationItem> _items = new();
        
        public Dictionary<string, List<string>> FileLocations { get; private set; } = new();
        
        public FileLocationPromptDialog(List<string> fileNames)
        {
            InitializeComponent();
            
            var locations = GetAvailableLocations();
            
            foreach (var fileName in fileNames)
            {
                var item = new FileLocationItem
                {
                    FileName = fileName,
                    AvailableLocations = new ObservableCollection<string>(locations)
                };
                item.SelectedLocations.Add(locations.FirstOrDefault() ?? "");
                _items.Add(item);
            }
            
            FilesListControl.ItemsSource = _items;
        }
        
        private List<string> GetAvailableLocations()
        {
            return new List<string>
            {
                // BoP Root first
                "BalanceOfPower/ (BoP Root - Default)",
                
                // BoP folders alphabetically
                "BalanceOfPower/AMOVIE/ (BoP Animated Movies)",
                "BalanceOfPower/BATTLE/ (BoP Battles)",
                "BalanceOfPower/BMOVIE/ (BoP Bitmap Movies)",
                "BalanceOfPower/CAMPAIGN/ (BoP Campaign)",
                "BalanceOfPower/COMBAT/ (BoP Combat Missions)",
                "BalanceOfPower/CP320/ (BoP 320x200 Graphics)",
                "BalanceOfPower/CP480/ (BoP 480 Graphics)",
                "BalanceOfPower/CP640/ (BoP 640x480 Graphics)",
                "BalanceOfPower/FRONTRES/ (BoP Front Resources)",
                "BalanceOfPower/IVFILES/ (BoP IV Files)",
                "BalanceOfPower/MELEE/ (BoP Melee)",
                "BalanceOfPower/MOVIES/ (BoP Movies)",
                "BalanceOfPower/MUSIC/ (BoP Music)",
                "BalanceOfPower/RESOURCE/ (BoP Resources)",
                "BalanceOfPower/TOURN/ (BoP Tournament)",
                "BalanceOfPower/TRAIN/ (BoP Training)",
                "BalanceOfPower/WAVE/ (BoP Sound Files)",
                
                // Game Root
                "(Game Root)",
                
                // XvT game root folders alphabetically
                "Amovie/ (XvT Animated Movies)",
                "Battle/ (XvT Battles)",
                "Bmovie/ (XvT Bitmap Movies)",
                "Combat/ (XvT Combat Missions)",
                "cp320/ (XvT 320x200 Graphics)",
                "cp480/ (XvT 480 Graphics)",
                "cp640/ (XvT 640x480 Graphics)",
                "frontres/ (XvT Front Resources)",
                "ivfiles/ (XvT IV Files)",
                "Melee/ (XvT Melee)",
                "Music/ (XvT Music)",
                "resource/ (XvT Resources)",
                "Sfx/ (XvT Sound Effects)",
                "Tourn/ (XvT Tournament)",
                "Train/ (XvT Training)",
                "wave/ (XvT Sound Files)"
            };
        }
        
        private string ExtractPathFromLocation(string location)
        {
            if (location == "(Game Root)")
            {
                return "";
            }
            
            var idx = location.IndexOf('(');
            if (idx > 0)
            {
                return location.Substring(0, idx).Trim();
            }
            return location;
        }
        
        private void AddLocationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileLocationItem item)
            {
                var locations = GetAvailableLocations();
                item.SelectedLocations.Add(locations.FirstOrDefault() ?? "");
            }
        }
        
        private void RemoveLocationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileLocationItem item)
            {
                if (item.SelectedLocations.Count > 1)
                {
                    var selectedLocation = ((FrameworkElement)sender).DataContext as string;
                    if (selectedLocation != null)
                    {
                        item.SelectedLocations.Remove(selectedLocation);
                    }
                }
                else
                {
                    MessageBox.Show("At least one location must be specified for each file.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
        
        private void LocationComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox comboBox && comboBox.DataContext is string oldLocation)
            {
                var newLocation = comboBox.SelectedItem as string;
                if (newLocation != null && newLocation != oldLocation)
                {
                    // Find the parent FileLocationItem
                    var border = FindAncestor<Border>(comboBox);
                    if (border?.DataContext is FileLocationItem item)
                    {
                        var index = item.SelectedLocations.IndexOf(oldLocation);
                        if (index >= 0)
                        {
                            item.SelectedLocations[index] = newLocation;
                        }
                    }
                }
            }
        }
        
        private T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }
        
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items)
            {
                var validLocations = item.SelectedLocations.Where(l => !string.IsNullOrEmpty(l)).ToList();
                
                if (validLocations.Count == 0)
                {
                    MessageBox.Show($"Please select at least one location for '{item.FileName}'.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var paths = new List<string>();
                foreach (var location in validLocations)
                {
                    var path = ExtractPathFromLocation(location);
                    paths.Add(path + item.FileName);
                }
                
                FileLocations[item.FileName] = paths;
            }
            
            DialogResult = true;
            Close();
        }
    }
}
