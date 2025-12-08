using System.Windows.Controls;
using XvTHydrospanner.Models;
using XvTHydrospanner.Services;

namespace XvTHydrospanner.Views
{
    public partial class ActiveModsPage : Page
    {
        public ActiveModsPage(ModProfile profile, WarehouseManager warehouseManager)
        {
            InitializeComponent();
            HeaderText.Text = $"Active Modifications - {profile.Name}";
            SubheaderText.Text = $"{profile.FileModifications.Count} modifications in this profile";
            ModsListBox.ItemsSource = profile.FileModifications;
        }
    }
}
