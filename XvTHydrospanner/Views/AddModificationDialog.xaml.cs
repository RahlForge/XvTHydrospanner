using System.Windows;
using System.Windows.Controls;
using XvTHydrospanner.Models;
using XvTHydrospanner.Services;

namespace XvTHydrospanner.Views
{
    public partial class AddModificationDialog : Window
    {
        private readonly WarehouseManager _warehouseManager;
        public WarehouseFile? SelectedWarehouseFile { get; private set; }
        public string TargetPath => TargetPathTextBox.Text;
        public string ModDescription { get; private set; } = string.Empty;
        
        public AddModificationDialog(WarehouseManager warehouseManager)
        {
            InitializeComponent();
            _warehouseManager = warehouseManager;
            LoadWarehouseFiles();
        }
        
        private void LoadWarehouseFiles()
        {
            WarehouseFilesGrid.ItemsSource = _warehouseManager.GetAllFiles();
        }
        
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTerm = SearchBox.Text;
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                LoadWarehouseFiles();
            }
            else
            {
                WarehouseFilesGrid.ItemsSource = _warehouseManager.Search(searchTerm);
            }
        }
        
        private void WarehouseFilesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WarehouseFilesGrid.SelectedItem is WarehouseFile file)
            {
                SelectedWarehouseFile = file;
                TargetPathTextBox.Text = file.TargetRelativePath;
                ModDescription = file.Description;
            }
        }
        
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedWarehouseFile == null)
            {
                MessageBox.Show("Please select a file from the warehouse.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(TargetPathTextBox.Text))
            {
                MessageBox.Show("Please specify the target path.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            DialogResult = true;
            Close();
        }
    }
}
