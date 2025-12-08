using System.Windows;

namespace XvTHydrospanner.Views
{
    public partial class NewProfileDialog : Window
    {
        public string ProfileName => ProfileNameTextBox.Text;
        public string ProfileDescription => ProfileDescriptionTextBox.Text;
        
        public NewProfileDialog(string defaultName = "")
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(defaultName) == false)
            {
                ProfileNameTextBox.Text = defaultName;
            }
        }
        
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProfileNameTextBox.Text))
            {
                MessageBox.Show("Please enter a profile name.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            DialogResult = true;
            Close();
        }
    }
}
