using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace XvTHydrospanner.Views
{
    /// <summary>
    /// Shown on first run (or when storage paths are missing) to let the user
    /// choose where XvT Hydrospanner stores its data.
    /// All fields are pre-populated with sensible defaults.
    /// </summary>
    public partial class ConfigureStoragePathsDialog : Window
    {
        public string WarehousePath => WarehousePathTextBox.Text.Trim();
        public string ProfilesPath  => ProfilesPathTextBox.Text.Trim();
        public string BackupPath    => BackupPathTextBox.Text.Trim();
        public string BaseGameBackupPath => BaseGameBackupPathTextBox.Text.Trim();

        public ConfigureStoragePathsDialog(
            string defaultWarehousePath,
            string defaultProfilesPath,
            string defaultBackupPath,
            string defaultBaseGameBackupPath)
        {
            InitializeComponent();
            WarehousePathTextBox.Text     = defaultWarehousePath;
            ProfilesPathTextBox.Text      = defaultProfilesPath;
            BackupPathTextBox.Text        = defaultBackupPath;
            BaseGameBackupPathTextBox.Text = defaultBaseGameBackupPath;
        }

        // -------------------------------------------------------------------------
        // Browse buttons
        // -------------------------------------------------------------------------

        private void BrowseWarehouse_Click(object sender, RoutedEventArgs e)
            => Browse("Select Mod Warehouse folder", WarehousePathTextBox);

        private void BrowseProfiles_Click(object sender, RoutedEventArgs e)
            => Browse("Select Profiles folder", ProfilesPathTextBox);

        private void BrowseBackup_Click(object sender, RoutedEventArgs e)
            => Browse("Select Mod Backups folder", BackupPathTextBox);

        private void BrowseBaseGameBackup_Click(object sender, RoutedEventArgs e)
            => Browse("Select Base Game Backup folder", BaseGameBackupPathTextBox);

        private void Browse(string description, System.Windows.Controls.TextBox target)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = description,
                ShowNewFolderButton = true,
            };

            var current = target.Text.Trim();
            if (!string.IsNullOrEmpty(current))
                dialog.InitialDirectory = current;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                target.Text = dialog.SelectedPath;
        }

        // -------------------------------------------------------------------------
        // Confirm / Cancel
        // -------------------------------------------------------------------------

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate all paths are non-empty
            if (string.IsNullOrWhiteSpace(WarehousePath) ||
                string.IsNullOrWhiteSpace(ProfilesPath)  ||
                string.IsNullOrWhiteSpace(BackupPath)    ||
                string.IsNullOrWhiteSpace(BaseGameBackupPath))
            {
                System.Windows.MessageBox.Show(
                    "All storage paths must be specified.",
                    "Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Try to create each directory — validates the paths are writable
            if (!TryEnsureDirectory(WarehousePath)        ||
                !TryEnsureDirectory(ProfilesPath)          ||
                !TryEnsureDirectory(BackupPath)            ||
                !TryEnsureDirectory(BaseGameBackupPath))
                return; // TryEnsureDirectory already showed the error

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to cancel?\n\n" +
                "Storage paths must be configured before XvT Hydrospanner can run. " +
                "The application will close.",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static bool TryEnsureDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not create directory:\n{path}\n\n{ex.Message}\n\n" +
                    "Please choose a different location.",
                    "Path Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }
    }
}
