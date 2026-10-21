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
    /// <summary>
    /// Логика взаимодействия для DevRevisionDialog.xaml
    /// </summary>
    public partial class DevRevisionDialog : Window
    {
        // Сюда запишем то, что ввел менеджер
        public string Comment { get; private set; }

        public DevRevisionDialog()
        {
            InitializeComponent();
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtDevReason.Text))
            {
                MessageBox.Show("Пожалуйста, введите текст замечаний для разработчика.", "Предупреждение");
                return;
            }

            Comment = TxtDevReason.Text;
            this.DialogResult = true; // Закрываем окно с успехом
        }
    }
}