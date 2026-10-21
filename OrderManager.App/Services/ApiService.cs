using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OrderManager.App.Services
{
    public static class ApiService
    {
        private static readonly HttpClient _client = new HttpClient();
        private static string _baseUrl = "http://localhost:5012/api/";
        private static string _token;

        public static void SetBaseUrl(string url) { _baseUrl = url; }
        public static void SetToken(string token)
        {
            _token = token;
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        public static async Task<AuthResponse> Login(string email, string password)
        {
            var json = JsonConvert.SerializeObject(new { email, password });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(_baseUrl + "auth/login", content);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<AuthResponse>(body);
        }

        public static async Task<List<OrderDto>> GetOrders()
        {
            var response = await _client.GetAsync(_baseUrl + "orders");
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<OrderDto>>(body);
        }

        public static async Task<OrderDto> UpdateOrderStatus(Guid orderId, string status)
        {
            var json = JsonConvert.SerializeObject(new { status });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync(_baseUrl + "orders/" + orderId + "/status", content);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<OrderDto>(body);
        }

        public static async Task<List<TaskDto>> GetTasks()
        {
            var response = await _client.GetAsync(_baseUrl + "tasks");
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<TaskDto>>(body);
        }

        public static async Task<TaskDto> CreateTask(CreateTaskDto dto)
        {
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(_baseUrl + "tasks", content);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TaskDto>(body);
        }

        public static async Task<TaskDto> UpdateTask(Guid taskId, object dto)
        {
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync(_baseUrl + "tasks/" + taskId, content);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TaskDto>(body);
        }

        public static async Task<List<MessageDto>> GetMessages(Guid orderId)
        {
            var response = await _client.GetAsync(_baseUrl + "orders/" + orderId + "/messages");
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<MessageDto>>(body);
        }

        public static async Task<MessageDto> SendMessage(Guid orderId, string text, string direction)
        {
            var json = JsonConvert.SerializeObject(new { text, direction });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(_baseUrl + "orders/" + orderId + "/messages", content);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<MessageDto>(body);
        }
        // Admin: пользователи
        public static async Task<List<UserDto>> GetUsers()
        {
            var response = await _client.GetAsync(_baseUrl + "admin/users");
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<UserDto>>(body);
        }

        public static async Task<UserDto> CreateUser(CreateUserDto dto)
        {
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(_baseUrl + "admin/users", content);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<UserDto>(body);
        }

        public static async Task ToggleBlock(Guid userId)
        {
            var response = await _client.PutAsync(_baseUrl + "admin/users/" + userId + "/block", null);
            response.EnsureSuccessStatusCode();
        }

        public static async Task DeleteUser(Guid userId)
        {
            var response = await _client.DeleteAsync(_baseUrl + "admin/users/" + userId);
            response.EnsureSuccessStatusCode();
        }

        public static async Task DeleteOrder(Guid orderId)
        {
            var response = await _client.DeleteAsync(_baseUrl + "admin/orders/" + orderId);
            response.EnsureSuccessStatusCode();
        }

        public static async Task<List<AuditLogDto>> GetLogs(int count = 50)
        {
            var response = await _client.GetAsync(_baseUrl + "admin/logs?count=" + count);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<AuditLogDto>>(body);
        }

        public static async Task<StatsDto> GetStats()
        {
            var response = await _client.GetAsync(_baseUrl + "admin/stats");
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<StatsDto>(body);
        }
    }

    // DTO
    public class AuthResponse
    {
        public string Token { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    public class OrderDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? Deadline { get; set; }
        public string ClientName { get; set; }
        public List<TaskDto> Tasks { get; set; } = new List<TaskDto>();
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
        public bool HasDeveloperRevision
        {
            get
            {
                // Если среди сообщений есть хоть одно от разработчика — возвращаем true
                return Messages != null && Messages.Any(m => m.Direction == "DeveloperToManager");
            }
        }
    }

    public class TaskDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public DateTime? Deadline { get; set; }
        public string DeveloperName { get; set; }
        public List<OrderManager.App.Services.MessageDto> Messages { get; set; } = new List<OrderManager.App.Services.MessageDto>();

        // Твоё вычисляемое свойство для красивого отображения статуса в ListBox
        public string StatusDisplay
        {
            get
            {
                return Status switch
                {
                    "Backlog" => "В бэклоге",
                    "InProgress" => "В работе",
                    "Done" => "Готово",
                    _ => Status
                };
            }
        }
    }
    public class CreateTaskDto
    {
        public Guid OrderId { get; set; }
        public string Title { get; set; }
        public Guid? AssignedToId { get; set; }
        public DateTime? Deadline { get; set; }
    }

    public class MessageDto
    {
        public Guid Id { get; set; }
        public string SenderName { get; set; }
        public string Text { get; set; }
        public string Direction { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsBlocked { get; set; }
    }

    public class CreateUserDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public string UserEmail { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
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
        public List<DeveloperStatsDto> Developers { get; set; }
    }

    public class DeveloperStatsDto
    {
        public string DeveloperName { get; set; }
        public int TotalTasks { get; set; }
        public int DoneTasks { get; set; }
    }
}