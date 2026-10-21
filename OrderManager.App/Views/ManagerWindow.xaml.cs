using OrderManager.App.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OrderManager.App.Views
{
    public partial class ManagerWindow : Window
    {
        private OrderDto _selectedOrder;
        private List<OrderDto> _allOrders = new List<OrderDto>();

        public ManagerWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadOrders();
        }

        private async Task LoadOrders()
        {
            try
            {
                _allOrders = await ApiService.GetOrders();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                // Выведет подробности: например, "401 Unauthorized" или "400 Bad Request"
                MessageBox.Show($"Ошибка загрузки: {ex.Message}\n{ex.InnerException?.Message}");
            }

        }

        private void ApplyFilter()
        {
            // Отладочное сообщение: проверим, пришло ли вообще хоть что-то из сети
            System.Diagnostics.Debug.WriteLine($"[WPF] Всего заказов скачано из API: {_allOrders.Count}");

            if (CmbFilterStatus.SelectedItem == null)
            {
                LstOrders.ItemsSource = new ObservableCollection<OrderDto>(_allOrders);
                return;
            }

            var filter = (CmbFilterStatus.SelectedItem as ComboBoxItem)?.Content.ToString();

            // Если в комбобоксе написано "Все" или фильтр пустой — отдаем всё
            if (string.IsNullOrEmpty(filter) || filter == "Все")
            {
                LstOrders.ItemsSource = new ObservableCollection<OrderDto>(_allOrders);
                return;
            }

            var filtered = _allOrders.AsEnumerable();

            switch (filter)
            {
                case "Новый": filtered = filtered.Where(o => o.Status == "New"); break;
                case "В работе": filtered = filtered.Where(o => o.Status == "InProgress"); break;
                case "На проверке": filtered = filtered.Where(o => o.Status == "Ready"); break; // Было: case "Готов"
                case "Завершён": filtered = filtered.Where(o => o.Status == "Completed"); break;
                case "Доработка": filtered = filtered.Where(o => o.Status == "Revision"); break;
                default: break; // На случай непредвиденных строк
            }

            var finalResult = filtered.ToList();
            System.Diagnostics.Debug.WriteLine($"[WPF] После фильтрации '{filter}' осталось заказов: {finalResult.Count}");

            LstOrders.ItemsSource = new ObservableCollection<OrderDto>(finalResult);
        }
        private async void BtnAddTask_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null)
            {
                MessageBox.Show("Сначала выберите заказ/проект из списка!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new TaskDialog();
            if (dialog.ShowDialog() == true)
            {
                var newTask = new CreateTaskDto
                {
                    OrderId = _selectedOrder.Id,               // КРИТИЧЕСКИ ВАЖНО: связываем задачу с текущим заказом
                    Title = dialog.TaskTitle,
                    Deadline = dialog.Deadline,
                    AssignedToId = dialog.SelectedDeveloperId  // Наш новый ID разработчика
                };

                try
                {
                    await ApiService.CreateTask(newTask);
                    MessageBox.Show("Задача успешно создана!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Если у тебя метод обновления списка называется по-другому, поправь имя:
                    await LoadOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при создании задачи: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void CmbFilterStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyFilter();
        }

        private void LstOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedOrder = LstOrders.SelectedItem as OrderDto;

            if (_selectedOrder != null)
            {
                TxtOrderTitle.Text = _selectedOrder.Title;
                TxtClient.Text = _selectedOrder.ClientName;
                TxtStatus.Text = StatusDisplay(_selectedOrder.Status);
                TxtCreated.Text = _selectedOrder.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                TxtDescription.Text = _selectedOrder.Description;
                LstTasks.ItemsSource = new ObservableCollection<TaskDto>(_selectedOrder.Tasks);
                // Подставляем полное пространство имен внутрь < >
                LstMessages.ItemsSource = new ObservableCollection<OrderManager.App.Services.MessageDto>(_selectedOrder.Messages);

                var expander = this.FindName("MessagesExpander") as Expander;
                if (expander != null)
                {
                    expander.Visibility = (_selectedOrder.Messages != null && _selectedOrder.Messages.Count > 0)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
                // После того, как данные проекта/сообщений загружены:
                // Было: LstMessages.ItemsSource = new ObservableCollection<...>(_selectedOrder.Messages);
                // Стало: добавляем .Reverse() в конце, чтобы перевернуть список

                if (_selectedOrder.Messages != null)
                {
                    var reversedMessages = _selectedOrder.Messages.AsEnumerable().Reverse();
                    LstMessages.ItemsSource = new ObservableCollection<OrderManager.App.Services.MessageDto>(reversedMessages);
                }
                else
                {
                    LstMessages.ItemsSource = null;
                }

                BtnAddTask.Visibility = Visibility.Visible;
                // Приводим выбранный элемент к твоему классу OrderDto
                if (LstOrders.SelectedItem is not OrderDto selected) return;
                _selectedOrder = selected;

                // Менять оформление кнопки будем в зависимости от статуса проекта

                // Проверяем ОБА статуса. Замени "ReadyForReview" на точное системное имя твоего статуса из базы!
                if (_selectedOrder.Status == "Revision" || _selectedOrder.Status == "Ready")
                {
                    BtnRevision1.Content = "Передать правки разработчику";
                    BtnRevision1.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3182CE")); // Голубая
                }
                else
                {
                    BtnRevision1.Content = "На доработку клиенту";
                    BtnRevision1.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E53E3E")); // Красная
                }
                // Автоматически выставляем текущий статус проекта в ComboBox
                foreach (ComboBoxItem item in CmbOrderSelectionStatus.Items)
                {
                    if (item.Tag?.ToString() == _selectedOrder.Status)
                    {
                        CmbOrderSelectionStatus.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        // 2. НОВЫЙ МЕТОД: Сохранение любого выбранного статуса
        private async void BtnSaveStatus_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null) return;

            if (CmbOrderSelectionStatus.SelectedItem is not ComboBoxItem selectedItem || selectedItem.Tag is not string chosenStatus)
            {
                MessageBox.Show("Выберите статус из списка!");
                return;
            }

            try
            {
                BtnSaveStatus.IsEnabled = false;

                // 1. Запоминаем ID текущего заказа перед обновлением
                Guid savedId = _selectedOrder.Id;

                // 2. Отправляем в API
                await ApiService.UpdateOrderStatus(savedId, chosenStatus);

                // 3. Загружаем свежие данные из базы (тут создается новый список)
                await LoadOrders();

                // 4. Ищем этот же заказ в новом списке по ID и выделяем его в ListBox!
                var orderToSelect = _allOrders.FirstOrDefault(o => o.Id == savedId);
                if (orderToSelect != null)
                {
                    LstOrders.SelectedItem = orderToSelect;
                    LstOrders.ScrollIntoView(orderToSelect); // Скроллим к нему, если список длинный
                }

                MessageBox.Show("Статус успешно изменен!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
            finally
            {
                BtnSaveStatus.IsEnabled = true;
            }
        }

        // 1. КНОПКА: НА ДОРАБОТКУ / ВЕРНУТЬ В РАБОТУ
        private async void BtnRevision_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null) return;

            // СЦЕНАРИЙ А: Проект на доработке у КЛИЕНТА (Status == "Revision")
            // Менеджер просто возвращает его в обычный рабочий процесс
            if (_selectedOrder.Status == "Revision")
            {
                try
                {
                    await ApiService.SendMessage(_selectedOrder.Id, "Проект возвращен менеджером в статус выполнения.", "ManagerToClient");
                    await ApiService.UpdateOrderStatus(_selectedOrder.Id, "InProgress");
                    await LoadOrders();
                    MessageBox.Show("Проект успешно возвращен в работу!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
                return;
            }

            // СЦЕНАРИЙ Б: Клиент ответил (Status == "Ready"). 
            // Нужно отправить правки и задачи РАЗРАБОТЧИКУ через отдельное окно!
            if (_selectedOrder.Status == "Ready")
            {
                // В ИДЕАЛЕ: Создай окно DeveloperRevisionDialog по аналогии со старым
                // Пока для теста можно использовать обычный InputBox, чтобы ввести текст правки:
                string developerComment = Microsoft.VisualBasic.Interaction.InputBox(
                    "Введите замечания и инструкции для разработчика:",
                    "Передача правок разработчику",
                    "Исправьте, пожалуйста, шрифты согласно пожеланиям клиента.");

                if (string.IsNullOrWhiteSpace(developerComment)) return; // Если менеджер нажал отмену

                try
                {
                    // Отправляем сообщение именно РАЗРАБОТЧИКУ (ManagerToDeveloper)
                    await ApiService.SendMessage(_selectedOrder.Id, $"🔄 Замечания менеджера: {developerComment}", "ManagerToDeveloper");

                    // Возвращаем проект в работу
                    await ApiService.UpdateOrderStatus(_selectedOrder.Id, "InProgress");

                    // Оживляем задачи разработчика (переводим из Backlog в InProgress)
                    if (_selectedOrder.Tasks != null)
                    {
                        var backlogTasks = _selectedOrder.Tasks.Where(t => t.Status == "Backlog");
                        foreach (var task in backlogTasks)
                        {
                            await ApiService.UpdateTask(task.Id, new { status = "InProgress" });
                        }
                    }

                    await LoadOrders();
                    MessageBox.Show("Правки успешно переданы разработчику, задачи возвращены в работу.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при передаче разработчику: " + ex.Message);
                }
                return;
            }

            // СЦЕНАРИЙ В: Проект в обычном статусе. Открываем стандартное окно для КЛИЕНТА
            var dialog = new RevisionDialog();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await ApiService.SendMessage(_selectedOrder.Id, dialog.Reason, "ManagerToClient");
                    await ApiService.UpdateOrderStatus(_selectedOrder.Id, "Revision");
                    await LoadOrders();
                    MessageBox.Show("Проект успешно отправлен на доработку клиенту.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }

        // 2. КНОПКА: СМЕНИТЬ СТАТУС (Прямой пошаговый перевод)
        private async void BtnChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null) return;

            string nextStatus = null;
            switch (_selectedOrder.Status)
            {
                case "New": nextStatus = "InProgress"; break;
                case "InProgress": nextStatus = "Ready"; break;
                case "Ready": nextStatus = "Completed"; break;
                case "Revision": nextStatus = "InProgress"; break; // Выводим из доработки по стандартной кнопке
            }

            if (nextStatus != null)
            {
                try
                {
                    await ApiService.UpdateOrderStatus(_selectedOrder.Id, nextStatus);
                    await LoadOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }

        // 1. КНОПКА: НА ДОРАБОТКУ / ВЕРНУТЬ В РАБОТУ
        //private async void BtnRevision_Click(object sender, RoutedEventArgs e)
        //{
        //    if (_selectedOrder == null) return;

        //    // СЦЕНАРИЙ А: Проект уже на доработке, возвращаем его Менеджером в работу
        //    if (_selectedOrder.Status == "Revision")
        //    {
        //        try
        //        {
        //            // Отправляем системное сообщение, что проект возвращен
        //            await ApiService.SendMessage(_selectedOrder.Id, "Проект возвращен менеджером в статус выполнения.", "ManagerToClient");
        //            await ApiService.UpdateOrderStatus(_selectedOrder.Id, "InProgress");
        //            await LoadOrders();
        //            MessageBox.Show("Проект успешно возвращен в работу!");
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show("Ошибка: " + ex.Message);
        //        }
        //        return;
        //    }

        //    // СЦЕНАРИЙ Б: Проект в обычном статусе, отправляем НА доработку
        //    var dialog = new RevisionDialog();
        //    dialog.Owner = this;
        //    if (dialog.ShowDialog() == true)
        //    {
        //        try
        //        {
        //            // Здесь у тебя в ApiService.SendMessage третьим параметром должна передаваться строка (Direction)
        //            // Убедись, что твой метод в WPF принимает CreateMessageDto или строку текста.
        //            await ApiService.SendMessage(_selectedOrder.Id, dialog.Reason, "ManagerToClient");
        //            await ApiService.UpdateOrderStatus(_selectedOrder.Id, "Revision");
        //            await LoadOrders();
        //            MessageBox.Show("Проект отправлен на доработку клиента.");
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show("Ошибка: " + ex.Message);
        //        }
        //    }
        //}

        //// 2. КНОПКА: СМЕНИТЬ СТАТУС (Исправленная цепочка)
        //private async void BtnChangeStatus_Click(object sender, RoutedEventArgs e)
        //{
        //    if (_selectedOrder == null) return;

        //    string nextStatus = null;
        //    switch (_selectedOrder.Status)
        //    {
        //        case "New": nextStatus = "InProgress"; break;
        //        case "InProgress": nextStatus = "Ready"; break;
        //        case "Ready": nextStatus = "Completed"; break;
        //        case "Revision": nextStatus = "InProgress"; break; // Если нажать "Сменить статус" на доработке, тоже вернет в работу
        //    }

        //    if (nextStatus != null)
        //    {
        //        try
        //        {
        //            await ApiService.UpdateOrderStatus(_selectedOrder.Id, nextStatus);
        //            await LoadOrders();
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show("Ошибка: " + ex.Message);
        //        }
        //    }
        //}


        //private async void BtnRevision_Click(object sender, RoutedEventArgs e)
        //{
        //    if (_selectedOrder == null) return;

        //    var dialog = new RevisionDialog();
        //    dialog.Owner = this;
        //    if (dialog.ShowDialog() == true)
        //    {
        //        try
        //        {
        //            await ApiService.SendMessage(_selectedOrder.Id, dialog.Reason, "ManagerToClient");
        //            await ApiService.UpdateOrderStatus(_selectedOrder.Id, "Revision");
        //            await LoadOrders();
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show("Ошибка: " + ex.Message);
        //        }
        //    }
        //}

        //private async void BtnChangeStatus_Click(object sender, RoutedEventArgs e)
        //{
        //    if (_selectedOrder == null) return;

        //    string nextStatus = null;
        //    switch (_selectedOrder.Status)
        //    {
        //        case "New": nextStatus = "InProgress"; break;
        //        case "InProgress": nextStatus = "Ready"; break;
        //        case "Ready": nextStatus = "Completed"; break;
        //    }

        //    if (nextStatus != null)
        //    {
        //        try
        //        {
        //            await ApiService.UpdateOrderStatus(_selectedOrder.Id, nextStatus);
        //            await LoadOrders();
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show("Ошибка: " + ex.Message);
        //        }
        //    }
        //}

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadOrders();
        }

        private string StatusDisplay(string status)
        {
            switch (status)
            {
                case "New": return "🟡 Новый";
                case "InProgress": return "🔵 В работе";
                case "Ready": return "🟢 Готов";
                case "Completed": return "✅ Завершён";
                case "Revision": return "🔴 Доработка";
                default: return status;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            this.Close();
        }
    }
}
/*  var expander = this.FindName("MessagesExpander") as System.Windows.Controls.Expander;
                if (expander != null)
                {
                    expander.Visibility = (_selectedOrder.Messages != null && _selectedOrder.Messages.Count > 0)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
*/