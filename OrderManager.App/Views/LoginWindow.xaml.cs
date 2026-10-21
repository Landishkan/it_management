using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using OrderManager.App.Models;
using OrderManager.App.Services;
namespace OrderManager.App.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Позволяет перетаскивать окно левой кнопкой мыши за любую область фона
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var email = TxtEmail.Text.Trim();
            var password = TxtPassword.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Заполните все поля");
                return;
            }
            if (!email.Contains("@") || !email.Contains("."))
            {
                ShowError("Введите email в формате name@domain.com");
                return;
            }
            try
            {
                BtnLogin.IsEnabled = false;
                BtnLogin.Content = "Вход...";

                var auth = await ApiService.Login(email, password);
                ApiService.SetToken(auth.Token);

                if (auth.Role == "Admin")
                {
                    new AdminWindow().Show();
                    this.Close();
                }
                else if (auth.Role == "Manager")
                {
                    new ManagerWindow().Show();
                    this.Close();
                }
                else if (auth.Role == "Developer")
                {
                    new DeveloperWindow().Show();
                    this.Close();
                }
                else
                {
                    ShowError("Доступ запрещён. Ожидайте клиентскую версию.");
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка входа: " + ex.Message);
            }
            finally
            {
                BtnLogin.IsEnabled = true;
                BtnLogin.Content = "LOGIN";
            }
        }
        private void ShowError(string message)
        {
            TxtError.Text = message;
            TxtError.Visibility = Visibility.Visible;
        }

        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}