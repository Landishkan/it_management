using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using OrderManager.App.Services;

namespace OrderManager.App.Views
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadUsers();
        }

        private async System.Threading.Tasks.Task LoadUsers()
        {
            try
            {
                var users = await ApiService.GetUsers();
                // Подставляем полное пространство имен для AuditLogDto
             
                LstUsers.ItemsSource = new ObservableCollection<OrderManager.App.Services.UserDto>(users);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пользователей: " + ex.Message);
            }
        }

        private async void BtnCreateUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CreateUserDialog();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Явно пишем полное имя типа перед new
                    await ApiService.CreateUser(new OrderManager.App.Services.CreateUserDto
                    {
                        FullName = dialog.FullName,
                        Email = dialog.Email,
                        Password = dialog.Password,
                        Role = dialog.Role
                    });
                    await LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }

        private async void BtnBlockUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UserDto user)
            {
                try
                {
                    // Формируем текст вопроса
                    string action = user.IsBlocked ? "разблокировать" : "заблокировать";
                    string message = $"Вы уверены, что хотите {action} пользователя:\n{user.FullName}?";

                    // Показываем подтверждение
                    var result = MessageBox.Show(
                        message,
                        "Подтверждение действия",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result != MessageBoxResult.Yes)
                        return;

                    // Выполняем действие
                    await ApiService.ToggleBlock(user.Id);

                    // Обновляем список
                    await LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }


        private async void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UserDto user)
            {
                if (MessageBox.Show("Удалить пользователя " + user.Email + "?",
                    "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        await ApiService.DeleteUser(user.Id);
                        await LoadUsers();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка: " + ex.Message);
                    }
                }
            }
        }

        private async void BtnLogs_Click(object sender, RoutedEventArgs e)
        {
            PanelUsers.Visibility = Visibility.Collapsed;
            PanelLogs.Visibility = Visibility.Visible;
            PanelStats.Visibility = Visibility.Collapsed;
            try
            {
                var logs = await ApiService.GetLogs(50);
                // Подставляем полное пространство имен для AuditLogDto
                LstLogs.ItemsSource = new ObservableCollection<OrderManager.App.Services.AuditLogDto>(logs);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private async void BtnStats_Click(object sender, RoutedEventArgs e)
        {
            PanelUsers.Visibility = Visibility.Collapsed;
            PanelLogs.Visibility = Visibility.Collapsed;
            PanelStats.Visibility = Visibility.Visible;
            try
            {
                var stats = await ApiService.GetStats();
                TxtStats.Text = string.Format(
                    "Всего заказов: {0}\n" +
                    "  Новых: {1}\n" +
                    "  В работе: {2}\n" +
                    "  Готовых: {3}\n" +
                    "  Завершённых: {4}\n" +
                    "  На доработке: {5}\n\n" +
                    "Всего пользователей: {6}\n" +
                    "Разработчиков: {7}",
                    stats.TotalOrders, stats.NewOrders, stats.InProgressOrders,
                    stats.ReadyOrders, stats.CompletedOrders, stats.RevisionOrders,
                    stats.TotalUsers, stats.TotalDevelopers);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void BtnUsers_Click(object sender, RoutedEventArgs e)
        {
            PanelUsers.Visibility = Visibility.Visible;
            PanelLogs.Visibility = Visibility.Collapsed;
            PanelStats.Visibility = Visibility.Collapsed;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            this.Close();
        }
    }
}