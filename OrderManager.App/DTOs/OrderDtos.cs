using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Windows;

namespace OrderManager.App.DTOs
{
    public class CreateOrderDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Type { get; set; } = string.Empty;

        public DateTime? Deadline { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }

    public class OrderDto : INotifyPropertyChanged
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string StatusDisplay { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? Deadline { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public List<TaskDto> Tasks { get; set; } = new();
        public List<MessageDto> Messages { get; set; } = new();
        public bool HasDeveloperRevision
        {
            get
            {
                // Если среди сообщений есть хоть одно от разработчика — возвращаем true
                return Messages != null && Messages.Any(m => m.Direction == "DeveloperToManager");
            }
        }

        // Упрощаем свойство Status (теперь оно просто хранит строку)
        public string Status { get; set; }

        // Вся магия вычисления теперь происходит в геттере
        public Visibility ProblemMarkerVisibility
        {
            get
            {
                // Если статус пустой или null - скрываем точку сразу
                if (string.IsNullOrEmpty(Status))
                    return Visibility.Collapsed;

                // Приводим к нижнему регистру и убираем пробелы для надежности
                string currentStatus = Status.Trim().ToLower();

                // Показываем точку ТОЛЬКО если статус равен ready
                if (currentStatus == "ready")
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
        }

        // Реализация интерфейса (теперь она будет работать легально!)
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class TaskDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; }
        public string StatusDisplay { get; set; }
        public DateTime? Deadline { get; set; }
        public string? DeveloperName { get; set; }
     
        public List<OrderManager.App.Services.MessageDto> Messages { get; set; } = new List<OrderManager.App.Services.MessageDto>();
    }

    public class CreateTaskDto
    {
        [Required]
        public Guid OrderId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public Guid? AssignedToId { get; set; }
        public DateTime? Deadline { get; set; }
    }


    public class UpdateTaskDto
    {
        public string? Status { get; set; }
        public Guid? AssignedToId { get; set; }
    }

    public class MessageDto
    {
        public Guid Id { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateMessageDto
    {
        [Required]
        public string Text { get; set; } = string.Empty;

        [Required]
        public string Direction { get; set; } = string.Empty;
    }
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
    }

    public class CreateUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class StatsDto
    {
        public int TotalOrders { get; set; }
        public int NewOrders { get; set; }
        public int InProgressOrders { get; set; }
        public int ReadyOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int RevisionOrders { get; set; }
        public int TotalUsers { get; set; }
        public int TotalDevelopers { get; set; }
        public List<DeveloperStatsDto> Developers { get; set; } = new();
    }

    public class DeveloperStatsDto
    {
        public string DeveloperName { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int DoneTasks { get; set; }
    }
}
