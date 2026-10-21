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
    [Authorize(Roles = "Admin,Manager")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        // Получить всех пользователей

        [HttpGet("users")]

        public async Task<ActionResult<List<UserDto>>> GetUsers()
        {
            var users = await _db.Users
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    Role = u.Role,
                    IsBlocked = u.IsBlocked
                })
                .ToListAsync();

            return Ok(users);
        }

        // Создать сотрудника (Manager или Developer)
        [HttpPost("users")]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto dto)
        {
            if (dto.Role != "Manager" && dto.Role != "Developer")
                return BadRequest("Можно создать только Manager или Developer");

            // Приводим к нижнему регистру для надёжности проверки
            var emailNormal = dto.Email.Trim().ToLower();

            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == emailNormal))
                return BadRequest("Пользователь с таким email уже существует");

            var user = new User
            {
                FullName = dto.FullName.Trim(),
                Email = emailNormal, // Сохраняем нормализованный email
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                IsBlocked = false // ЯВНО УКАЗЫВАЕМ, ЧТО ПОЛЬЗОВАТЕЛЬ НЕ ЗАБЛОКИРОВАН
            };

            _db.Users.Add(user);

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = GetUserId(),
                Action = "CreateUser",
                Description = $"Создан пользователь {user.Email} с ролью {user.Role}"
            });

            await _db.SaveChangesAsync();

            return Ok(new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                IsBlocked = user.IsBlocked
            });
        }

        // Заблокировать/разблокировать пользователя
        [HttpPut("users/{id}/block")]
        public async Task<ActionResult> ToggleBlock(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            if (user.Role == "Admin")
                return BadRequest("Нельзя заблокировать администратора");

            user.IsBlocked = !user.IsBlocked;

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = GetUserId(),
                Action = user.IsBlocked ? "BlockUser" : "UnblockUser",
                Description = $"Пользователь {user.Email} {(user.IsBlocked ? "заблокирован" : "разблокирован")}"
            });

            await _db.SaveChangesAsync();
            return Ok();
        }

        // Удалить пользователя
        [HttpDelete("users/{id}")]
        public async Task<ActionResult> DeleteUser(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            if (user.Role == "Admin")
                return BadRequest("Нельзя удалить администратора");

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = GetUserId(),
                Action = "DeleteUser",
                Description = $"Удалён пользователь {user.Email}"
            });

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // Удалить заказ
        [HttpDelete("orders/{id}")]
        public async Task<ActionResult> DeleteOrder(Guid id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = GetUserId(),
                Action = "DeleteOrder",
                Description = $"Удалён заказ «{order.Title}» (ID: {order.Id})"
            });

            _db.Orders.Remove(order);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // Получить логи
        [HttpGet("logs")]
        public async Task<ActionResult<List<AuditLogDto>>> GetLogs(int count = 50)
        {
            var logs = await _db.AuditLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .Select(l => new AuditLogDto
                {
                    Id = l.Id,
                    UserEmail = l.User.Email,
                    Action = l.Action,
                    Description = l.Description,
                    Timestamp = l.Timestamp
                })
                .ToListAsync();

            return Ok(logs);
        }

        // Получить статистику
        [HttpGet("stats")]
        public async Task<ActionResult<StatsDto>> GetStats()
        {
            var stats = new StatsDto
            {
                TotalOrders = await _db.Orders.CountAsync(),
                NewOrders = await _db.Orders.CountAsync(o => o.Status == "New"),
                InProgressOrders = await _db.Orders.CountAsync(o => o.Status == "InProgress"),
                ReadyOrders = await _db.Orders.CountAsync(o => o.Status == "Ready"),
                CompletedOrders = await _db.Orders.CountAsync(o => o.Status == "Completed"),
                RevisionOrders = await _db.Orders.CountAsync(o => o.Status == "Revision"),
                TotalUsers = await _db.Users.CountAsync(),
                TotalDevelopers = await _db.Users.CountAsync(u => u.Role == "Developer")
            };

            var developers = await _db.Users
                .Where(u => u.Role == "Developer")
                .Select(u => new DeveloperStatsDto
                {
                    DeveloperName = u.FullName,
                    TotalTasks = u.Tasks.Count,
                    DoneTasks = u.Tasks.Count(t => t.Status == "Done")
                })
                .ToListAsync();

            stats.Developers = developers;
            return Ok(stats);
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return Guid.Parse(claim!.Value);
        }
    }
}