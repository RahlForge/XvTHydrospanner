using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace XvTHydrospanner.Views
{
    /// <summary>
    /// Informs the user that a base game backup is required, lets them select their game
    /// installation folder, and asks them to confirm or exit.
    /// </summary>
    public partial class BackupRequiredDialog : Window
    {
        private const string GameExeName = "Z_XVT__.EXE";

        /// <summary>
        /// True if the user chose to proceed with creating the backup.
        /// </summary>
        public bool UserConfirmedBackup { get; private set; }

        /// <summary>
        /// The validated game installation path chosen by the user.
        /// Only set when <see cref="UserConfirmedBackup"/> is true.
        /// </summary>
        public string? SelectedGamePath { get; private set; }

        public BackupRequiredDialog()
        {
            InitializeComponent();

            // Try to auto-detect a valid game installation on startup
            var detected = TryAutoDetectGamePath();
            if (detected != null)
            {
                GamePathTextBox.Text = detected;
                // TextChanged fires UpdatePathValidationUI automatically
            }
            else
            {
                UpdatePathValidationUI();
            }
        }

        // -------------------------------------------------------------------------
        // Auto-detection
        // -------------------------------------------------------------------------

        /// <summary>
        /// Searches common installation locations for a valid XvT game folder.
        /// Returns the first valid path found, or null if none found.
        /// </summary>
        private string? TryAutoDetectGamePath()
        {
            // Common root directories to search (fast, targeted)
            var searchRoots = new[]
            {
                @"C:\GOG Games",
                @"C:\Program Files",
                @"C:\Program Files (x86)",
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            };

            foreach (var root in searchRoots)
            {
                if (!Directory.Exists(root))
                    continue;

                try
                {
                    // Look for subfolders whose names suggest X-Wing vs TIE Fighter
                    var candidates = Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly)
                        .Where(d => IsXvtFolderName(Path.GetFileName(d)));

                    foreach (var candidate in candidates)
                    {
                        if (IsValidGameInstallation(candidate))
                            return candidate;
                    }
                }
                catch (UnauthorizedAccessException) { /* skip inaccessible roots */ }
                catch (IOException) { /* skip unavailable roots */ }
            }

            return null;
        }

        /// <summary>
        /// Returns true if the folder name suggests an XvT installation.
        /// Matches names like "Star Wars X-Wing vs TIE Fighter", "STAR WARS - XWing vs TIE", etc.
        /// </summary>
        private static bool IsXvtFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var upper = name.ToUpperInvariant();
            return upper.Contains("STAR") && upper.Contains("WARS") &&
                   (upper.Contains("XVT") || upper.Contains("X-WING") || upper.Contains("XWING")) &&
                   (upper.Contains("TIE") || upper.Contains("FIGHTER"));
        }

        // -------------------------------------------------------------------------
        // Validation
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns true if <paramref name="path"/> is a directory containing the
        /// XvT game executable (case-insensitive match).
        /// </summary>
        private static bool IsValidGameInstallation(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return false;

            try
            {
                return Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)
                    .Any(f => string.Equals(Path.GetFileName(f), GameExeName,
                                           StringComparison.OrdinalIgnoreCase));
            }
            catch (UnauthorizedAccessException) { return false; }
            catch (IOException) { return false; }
        }

        // -------------------------------------------------------------------------
        // UI Feedback
        // -------------------------------------------------------------------------

        /// <summary>
        /// Updates the validation status icon/text and the Create Backup button state
        /// based on the current content of <see cref="GamePathTextBox"/>.
        /// </summary>
        private void UpdatePathValidationUI()
        {
            var path = GamePathTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(path))
            {
                // ⚠ No path selected (gray)
                ValidationStatusIcon.Text = "\u26A0";
                ValidationStatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
                ValidationStatusText.Text = "No path selected";
                ValidationStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
                CreateBackupButton.IsEnabled = false;
            }
            else if (IsValidGameInstallation(path))
            {
                // ✓ Valid XvT installation (green)
                ValidationStatusIcon.Text = "\u2713";
                ValidationStatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0));
                ValidationStatusText.Text = "Valid XvT installation";
                ValidationStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0));
                CreateBackupButton.IsEnabled = true;
            }
            else
            {
                // ✗ Not a valid XvT installation (red)
                ValidationStatusIcon.Text = "\u2717";
                ValidationStatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xF4, 0x44, 0x36));
                ValidationStatusText.Text = "Not a valid XvT installation";
                ValidationStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xF4, 0x44, 0x36));
                CreateBackupButton.IsEnabled = false;
            }
        }

        // -------------------------------------------------------------------------
        // Event Handlers
        // -------------------------------------------------------------------------

        private void GamePathTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePathValidationUI();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select your Star Wars X-Wing vs TIE Fighter installation folder",
                ShowNewFolderButton = false,
            };

            // Pre-seed the dialog with any current value
            var current = GamePathTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(current) && Directory.Exists(current))
                dialog.InitialDirectory = current;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                GamePathTextBox.Text = dialog.SelectedPath;
                // TextChanged fires UpdatePathValidationUI automatically
            }
        }

        private void CreateBackupButton_Click(object sender, RoutedEventArgs e)
        {
            var path = GamePathTextBox.Text?.Trim();

            // Guard: should not be reachable with button disabled, but be safe
            if (!IsValidGameInstallation(path))
            {
                System.Windows.MessageBox.Show(
                    "Please select a valid XvT installation folder before creating the backup.\n\n" +
                    $"The folder must contain {GameExeName}.",
                    "Invalid Path",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            SelectedGamePath = path;
            UserConfirmedBackup = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to cancel?\n\n" +
                "The base game backup is required to use XvT Hydrospanner. " +
                "The application will close.",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                UserConfirmedBackup = false;
                DialogResult = false;
                Close();
            }
        }
    }
}
