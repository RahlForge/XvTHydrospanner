using System.Windows;

namespace XvTHydrospanner.Views
{
    public partial class InputDialog : Window
    {
        public string InputText => InputTextBox.Text;
        
        public InputDialog(string title, string prompt)
        {
            InitializeComponent();
            Title = title;
            PromptTextBlock.Text = prompt;
            InputTextBox.Focus();
        }
        
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

