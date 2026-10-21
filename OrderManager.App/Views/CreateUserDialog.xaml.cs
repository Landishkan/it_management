using System.Windows;
using System.Windows.Controls;

namespace OrderManager.App.Views
{
    public partial class CreateUserDialog : Window
    {
        public string FullName { get; private set; }
        public string Email { get; private set; }
        public string Password { get; private set; }
        public string Role { get; private set; }

        public CreateUserDialog()
        {
            InitializeComponent();
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            FullName = TxtName.Text.Trim();
            Email = TxtEmail.Text.Trim();
            Password = TxtPassword.Text;
            Role = (CmbRole.SelectedItem as ComboBoxItem)?.Tag?.ToString();

            if (string.IsNullOrWhiteSpace(FullName) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Заполните все поля");
                return;
            }
            if (!Email.Contains("@") || !Email.Contains("."))
            {
                MessageBox.Show("Введите корректный email");
                return;
            }
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}