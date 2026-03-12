using System;
using System.Threading;
using System.Windows;
using XvTHydrospanner.Models;
using XvTHydrospanner.Services;

namespace XvTHydrospanner.Views
{
    /// <summary>
    /// Shows progress while the base game backup is being created or restored.
    /// </summary>
    public partial class BackupProgressDialog : Window
    {
        private readonly BaseGameBackupManager _backupManager;
        private readonly string _sourceGamePath;
        private readonly CancellationTokenSource _cts = new();
        private bool _operationCompleted;

        public BackupProgressDialog(BaseGameBackupManager backupManager, string sourceGamePath)
        {
            InitializeComponent();
            _backupManager = backupManager;
            _sourceGamePath = sourceGamePath;
            _backupManager.ProgressChanged += OnProgressChanged;
            Loaded += BackupProgressDialog_Loaded;
        }

        private async void BackupProgressDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _backupManager.CreateBackupAsync(_sourceGamePath, _cts.Token);
                _operationCompleted = true;
                DialogResult = true;
                Close();
            }
            catch (OperationCanceledException)
            {
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }

        private void OnProgressChanged(object? sender, BackupProgress progress)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = progress.Percentage;
                StatusLabel.Text = progress.StatusMessage;
                if (!string.IsNullOrEmpty(progress.CurrentFile))
                    FileLabel.Text = progress.CurrentFile;
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            CancelButton.IsEnabled = false;
            StatusLabel.Text = "Cancelling...";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_operationCompleted && !_cts.IsCancellationRequested)
            {
                // Prevent closing via title bar X during active operation; user must use Cancel button
                e.Cancel = true;
            }
        }
    }
}
