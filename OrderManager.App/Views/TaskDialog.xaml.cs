using OrderManager.App.Models;
using OrderManager.App.Services;
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

namespace OrderManager.App.Views
{
    public partial class TaskDialog : Window
    {
        public string TaskTitle { get; private set; }
        public string SelectedDeveloper { get; private set; }
        public DateTime? Deadline { get; private set; }

        public Guid? SelectedDeveloperId { get; private set; }

        public TaskDialog()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadDevelopers();
        }
        // Измени тип свойства, чтобы оно возвращало ID (Guid или int - посмотри, какой тип у тебя в базе)


        private async System.Threading.Tasks.Task LoadDevelopers()
        {
            try
            {
                // Переводим ItemsSource в null, чтобы сбросить старую привязку без ошибок
                CmbDeveloper.ItemsSource = null;

                var users = await ApiService.GetUsers();
                var developers = users.Where(u => u.Role != null && u.Role.ToLower() == "developer").ToList();

                CmbDeveloper.DisplayMemberPath = "FullName";
                CmbDeveloper.SelectedValuePath = "Id";
                CmbDeveloper.ItemsSource = developers;

                // Проверяем количество элементов напрямую в списке данных
                if (developers.Count > 0)
                    CmbDeveloper.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки исполнителей: {ex.Message}");
            }
        }
        // 1. Вместо имени-строки теперь храним ID (Guid? или int? — выбери тип своей БД)


        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            TaskTitle = TxtTitle.Text.Trim();
            Deadline = DpDeadline.SelectedDate;

            // 2. Вытаскиваем скрытый ID из выбранного элемента ComboBox
            if (CmbDeveloper.SelectedValue != null)
            {
                SelectedDeveloperId = (Guid)CmbDeveloper.SelectedValue;
            }

            // Валидация названия задачи
            if (string.IsNullOrWhiteSpace(TaskTitle))
            {
                MessageBox.Show("Введите название задачи", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Проверяем, выудили ли мы ID разработчика
            if (SelectedDeveloperId == null)
            {   
                MessageBox.Show("Выберите исполнителя", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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