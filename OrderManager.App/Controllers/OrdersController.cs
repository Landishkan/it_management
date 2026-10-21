using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManager.App.Data;
using OrderManager.App.DTOs;
using OrderManager.App.Models;

namespace OrderManager.App.Controllers
{
    [ApiController]
    [Route("api/orders")] // Теперь совпадает с JS (/api/orders)
    [Authorize(Roles = "Admin,Manager,Client")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public OrdersController(AppDbContext db)
        {
            _db = db;
        }

        // Создать заказ (Client)
        [HttpPost]
        public async Task<ActionResult<OrderDto>> Create(CreateOrderDto dto)
        {
            var userId = GetUserId();
            var user = await _db.Users.FindAsync(userId);
            if (user == null || user.Role != "Client")
                return Forbid();

            var order = new Order
            {
                ClientId = userId,
              
                Title = dto.Title,
                Description = dto.Description,
                Type = dto.Type,
                Deadline = dto.Deadline,
                Status = "New"
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            order.Client = user;
            return Ok(MapOrder(order));
        }

        // Получить заказы (Client — свои, Manager — все, Developer — свои через задачи)
        [HttpGet]
        public async Task<ActionResult<List<OrderDto>>> GetAll()
        {
            var provider = _db.Database.ProviderName; // Узнаем имя провайдера (Npgsql или Sqlite)
            var totalOrdersInDb = await _db.Orders.CountAsync(); // Считаем ВСЕ строки в таблице заказов

            Console.WriteLine($"[DEBUG] Контроллер использует провайдер: {provider}");
            Console.WriteLine($"[DEBUG] Всего заказов в этой базе данных: {totalOrdersInDb}");
            var userId = GetUserId();
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            // Временно убираем .Include(o => o.Client), чтобы EF Core перестал делать жесткий INNER JOIN
            IQueryable<Order> query = _db.Orders
              .Include(o => o.Client) // ТЕПЕРЬ ИМЯ БУДЕТ ЗАГРУЖАТЬСЯ!
              .Include(o => o.Tasks).ThenInclude(t => t.AssignedTo)
              .Include(o => o.Messages).ThenInclude(m => m.Sender);

            if (user.Role == "Client")
                query = query.Where(o => o.ClientId == userId);

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            // Передаем в маппинг
            return Ok(orders.Select(MapOrder).ToList());
        }

        // Получить один заказ
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetById(Guid id)
        {
            var userId = GetUserId();
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            var order = await _db.Orders
                .Include(o => o.Client)
                .Include(o => o.Tasks).ThenInclude(t => t.AssignedTo)
                .Include(o => o.Messages).ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            if (user.Role == "Client" && order.ClientId != userId)
                return Forbid();

            return Ok(MapOrder(order));
        }

        // Сменить статус (Manager)
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Manager,Client")]
        public async Task<ActionResult<OrderDto>> UpdateStatus(Guid id, UpdateOrderStatusDto dto)
        {
            // 1. Ищем заказ в базе один раз
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound("Заказ не найден");

            // 2. Ищем текущего пользователя
            var user = await _db.Users.FindAsync(GetUserId());
            if (user == null) return Unauthorized();

            // 3. Проверяем права доступа (клиент может менять только СВОЙ заказ)
            if (user.Role == "Client" && order.ClientId != user.Id)
            {
                return Forbid(); // Чужой заказ клиент менять не имеет права!
            }

            // 4. Защита от других неавторизованных ролей
            if (user.Role != "Manager" && user.Role != "Developer" && user.Role != "Client")
            {
                return Forbid();
            }

            // 5. Меняем статус (используем уже найденный выше объект order)
            order.Status = dto.Status;
            await _db.SaveChangesAsync();

            return Ok(MapOrder(order));
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return Guid.Parse(claim!.Value);
        }

        private OrderDto MapOrder(Order o)
        {
            string russianStatus = o.Status switch
            {
                "New" => "Новый",
                "InProgress" => "В работе",
                "Ready" => "На проверке", // <-- ВМЕСТО "ГОТОВ" ТЕПЕРЬ "НА ПРОВЕРКЕ"!
                "Revision" => "Доработка",
                "Completed" => "Завершён",
                _ => o.Status
            };
            return new OrderDto
            {
                Id = o.Id,
                Title = o.Title,
                Description = o.Description,
                Type = o.Type,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                Deadline = o.Deadline,
                ClientName = o.Client?.FullName ?? "Неизвестный клиент (Ошибка ID)",
                Tasks = o.Tasks.Select(t => new TaskDto
                {
                    Id = t.Id,
                    OrderId = o.Id,
                    Title = t.Title,
                    Status = t.Status,
                    Deadline = t.Deadline,
                    DeveloperName = t.AssignedTo?.FullName
                }).ToList(),
                Messages = o.Messages.OrderBy(m => m.CreatedAt).Select(m => new MessageDto
                {
                    Id = m.Id,
                    SenderName = m.Sender?.FullName ?? "",
                    Text = m.Text,
                    Direction = m.Direction,
                    CreatedAt = m.CreatedAt
                }).ToList()
            };
        }
    }
}