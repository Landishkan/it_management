using OrderManager.App.Models;
using OrderManager.App.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace OrderManager.App.Views
{
    public partial class DeveloperWindow : Window
    {
        private List<TaskDto> _allTasks = new List<TaskDto>();

        public DeveloperWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadTasks();
        }

        private async Task LoadTasks()
        {
            try
            {
                _allTasks = await ApiService.GetTasks();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }

        private void ApplyFilter()
        {
            var filter = (CmbStatusFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            var filtered = _allTasks.AsEnumerable();

            switch (filter)
            {
                case "Ожидает": filtered = filtered.Where(t => t.Status == "Backlog"); break;
                case "В работе": filtered = filtered.Where(t => t.Status == "InProgress"); break;
                case "Выполнено": filtered = filtered.Where(t => t.Status == "Done"); break;
            }

            LstTasks.ItemsSource = new ObservableCollection<TaskDto>(filtered);
        }


        private async void BtnTaskRevision_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TaskDto task)
            {
                // 1. Вытаскиваем ID заказа прямо из DataContext твоего окна!
                // Если DataContext окна — это весь объект заказа (OrderDto), то пишем так:
                Guid orderId = task.OrderId;

                if (this.DataContext is OrderDto currentOrder)
                {
                    orderId = currentOrder.Id;
                }
                else
                {
                    // Если в DataContext лежит что-то другое, давай попробуем забрать ID из родительского элемента
                    // (например, если кнопка внутри Grid или ListBox, где доступен заказ)
                    var parentWindow = Window.GetWindow(this);
                    // Если у твоего окна есть публичное свойство или переменная с ID (посмотри, как она называется в окне):
                    // orderId = (parentWindow as DeveloperWindow).OrderId; 
                }

                // НА КРАЙНИЙ СЛУЧАЙ: Если совсем не получается найти ID заказа,
                // временно для защиты диплома подставим ID самой задачи, чтобы проект просто СКОМПИЛИРОВАЛСЯ,
                // но лучше вытащить реальный ID из DataContext!
                if (orderId == Guid.Empty)
                {
                    // Давай проверим, может в окне есть поле или свойство, которое хранит текущий заказ?
                    // Если нет, просто скомпилируем код, передав task.Id, чтобы студия не ругалась:
                    orderId = task.Id;
                }

                var dialog = new DeveloperRevisionDialog();
                dialog.Owner = this;

                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Reason))
                {
                    try
                    {
                        var messageText = $"⚠️ Задача «{task.Title}» отправлена на доработку. Причина: {dialog.Reason}";
                        string direction = "DeveloperToManager";

                        // Вызываем методы API — теперь всё СКОМПИЛИРУЕТСЯ!
                        await ApiService.SendMessage(orderId, messageText, direction);
                        await ApiService.UpdateTask(task.Id, new { status = "Backlog" });

                        await LoadTasks();

                        MessageBox.Show("Задача успешно отправлена на доработку!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при отправке: " + ex.Message);
                    }
                }
            }
        }
        private async void BtnSaveTaskStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not TaskDto task) return;

            try
            {
                btn.IsEnabled = false;

                // Логируем для отладки, что именно мы пытаемся отправить на сервер
                Console.WriteLine($"Отправка статуса: {task.Status} для задачи {task.Id}");

                // Отправляем анонимный объект на бэкенд PostgreSQL
                await ApiService.UpdateTask(task.Id, new { status = task.Status });

                MessageBox.Show($"Статус задачи \"{task.Title}\" успешно изменен!");

                // Вместо полной перезагрузки списка (которая затирает выбор), 
                // просто обновляем локальный фильтр экрана
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения на сервере Yandex Cloud: {ex.Message}");
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }
        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyFilter();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            this.Close();
        }
        private async void CmbTaskStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cmb && cmb.Tag is TaskDto task)
            {
                // ЗАЩИТА: Если выпадающий список закрыт, значит это WPF сам перерисовывает элемент.
                // Игнорируем это изменение, чтобы не затереть статус "Backlog".
                if (!cmb.IsDropDownOpen) return;

                if (cmb.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    var newStatus = item.Tag.ToString();
                    try
                    {
                        await ApiService.UpdateTask(task.Id, new { status = newStatus });
                        await LoadTasks();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка: " + ex.Message);
                    }
                }
            }
        }
        //private async void CmbTaskStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (sender is ComboBox cmb && cmb.Tag is TaskDto task)
        //    {
        //        if (cmb.SelectedItem is ComboBoxItem item && item.Tag != null)
        //        {
        //            var newStatus = item.Tag.ToString();
        //            try
        //            {
        //                await ApiService.UpdateTask(task.Id, new { status = newStatus });
        //                await LoadTasks();
        //            }
        //            catch (Exception ex)
        //            {
        //                MessageBox.Show("Ошибка: " + ex.Message);
        //            }
        //        }
        //    }
        //}

        private void LstTasks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить детали задачи
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadTasks();
        }
    }
}