using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManager.App.Data;
using OrderManager.App.DTOs;
using OrderManager.App.Models;

namespace OrderManager.App.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager,Developer")]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TasksController(AppDbContext db)
        {
            _db = db;
        }

        // Создать задачу (Manager)
        [HttpPost]
        public async Task<ActionResult<TaskDto>> Create(CreateTaskDto dto)
        {
            var user = await _db.Users.FindAsync(GetUserId());
            if (user == null || user.Role != "Manager")
                return Forbid();

            var order = await _db.Orders.FindAsync(dto.OrderId);
            if (order == null) return NotFound("Заказ не найден");

            var task = new DevTask
            {
                OrderId = dto.OrderId,
                Title = dto.Title,
                AssignedToId = dto.AssignedToId,
                Deadline = dto.Deadline,
                Status = "В ожидании"
            };
            if (dto.Deadline.HasValue)
            {
                // Насильно говорим драйверу PostgreSQL, что эта дата — UTC
                task.Deadline = DateTime.SpecifyKind(dto.Deadline.Value, DateTimeKind.Utc);
            }
            else
            {
                // Если дедлайн не передан, можно поставить текущее время в UTC
                task.Deadline = DateTime.UtcNow;
            }
            _db.Tasks.Add(task);

            if (order.Status == "New")
                order.Status = "InProgress";
         
            await _db.SaveChangesAsync();

            return Ok(MapTask(task));
        }

        // Получить задачи разработчика (Developer) или все (Manager)
  
        [HttpGet]
        public async Task<ActionResult<List<TaskDto>>> GetAll()
        {
            var userId = GetUserId();
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            IQueryable<DevTask> query = _db.Tasks
                .Include(t => t.Order)
                    .ThenInclude(o => o.Messages) // 🔥 КРИТИЧЕСКИ ВАЖНО: Подгружаем сообщения из заказа!
                .Include(t => t.AssignedTo);

            if (user.Role == "Developer")
                query = query.Where(t => t.AssignedToId == userId);
            else if (user.Role == "Manager")
                // Менеджер видит все задачи
                query = query;
            else
                return Forbid();

            var tasks = await query.OrderByDescending(t => t.Order.CreatedAt).ToListAsync();
            return Ok(tasks.Select(MapTask).ToList());
        }

        // Обновить статус задачи или назначить разработчика
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskDto>> Update(Guid id, UpdateTaskDto dto)
        {
            var user = await _db.Users.FindAsync(GetUserId());
            if (user == null) return Unauthorized();

            var task = await _db.Tasks
                .Include(t => t.Order)
                .ThenInclude(o => o.Messages)
                .Include(t => t.AssignedTo)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            if (dto.AssignedToId.HasValue && user.Role == "Manager")
                task.AssignedToId = dto.AssignedToId;

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                task.Status = dto.Status;

                // Если задача выполнена — проверить все ли задачи заказа готовы
                if (dto.Status == "Done")
                {
                    var order = await _db.Orders
                        .Include(o => o.Tasks)
                        .FirstOrDefaultAsync(o => o.Id == task.OrderId);

                    if (order != null && order.Tasks.All(t => t.Status == "Done"))
                        order.Status = "Ready";
                }
            }

            await _db.SaveChangesAsync();
            return Ok(MapTask(task));
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return Guid.Parse(claim!.Value);
        }

        private TaskDto MapTask(DevTask t)
        {
            string russianStatus = t.Status switch
            {
                "Backlog" => "В ожидании",
                "InProgress" => "В работе",
                "Done" => "Готово",
                _ => t.Status // Если вдруг прилетит что-то другое, оставим как есть
            };
            return new TaskDto
            {
                Id = t.Id,
                OrderId = t.OrderId,
                Title = t.Title,
                Status = t.Status,
                StatusDisplay = russianStatus,
                Deadline = t.Deadline,
                DeveloperName = t.AssignedTo != null ? t.AssignedTo.FullName : "Не назначен",

                // ИСПРАВЛЕННЫЙ МАППИНГ: явно указываем тип, который прописан в твоем TaskDto
                // Если статус задачи "Done", мы принудительно возвращаем пустой список, 
                // и красное уведомление мгновенно исчезает с экрана!
                Messages = (t.Status != "Done" && t.Status != "Готово" && t.Order.Messages != null)
    ? t.Order.Messages
        .Where(m => m.Direction == "ManagerToDeveloper")
        .Select(m => new OrderManager.App.Services.MessageDto
        {
            Id = m.Id,
            Text = m.Text,
            CreatedAt = m.CreatedAt
        })
        .OrderByDescending(m => m.CreatedAt)
        .ToList()
    : new List<OrderManager.App.Services.MessageDto>()// Если задача выполнена, отдаем пустой список (блок скроется)

                // Передаем статус (если статус лежит в самой задаче строкой или объектом)
                // Если у тебя статус - это строка прямо в DevTask, пиши: task.Status
                // If статус - это объект связи, то: task.Status != null ? task.Status.Name : "Новая"

            };
        }
    }
}