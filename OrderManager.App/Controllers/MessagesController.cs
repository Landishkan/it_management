using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManager.App.Data;
using OrderManager.App.DTOs;
using OrderManager.App.Models;

namespace OrderManager.App.Controllers
{
    [ApiController]
    [Route("api/orders/{orderId}/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MessagesController(AppDbContext db)
        {
            _db = db;
        }

        // Отправить сообщение
        [HttpPost]
        public async Task<ActionResult<MessageDto>> Send(Guid orderId, CreateMessageDto dto)
        {
            var userId = GetUserId();
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return NotFound("Заказ не найден");

            if (user.Role == "Client" && order.ClientId != userId)
                return Forbid();

            var message = new Message
            {
                OrderId = orderId,
                SenderId = userId,
                Text = dto.Text,
                Direction = dto.Direction
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            return Ok(new MessageDto
            {
                Id = message.Id,
                SenderName = user.FullName,
                Text = message.Text,
                Direction = message.Direction,
                CreatedAt = message.CreatedAt
            });
        }

        // Получить сообщения по заказу
        [HttpGet]
        public async Task<ActionResult<List<MessageDto>>> GetByOrder(Guid orderId)
        {
            var userId = GetUserId();
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            if (user.Role == "Client" && order.ClientId != userId)
                return Forbid();

            var messages = await _db.Messages
                .Include(m => m.Sender)
                .Where(m => m.OrderId == orderId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            return Ok(messages.Select(m => new MessageDto
            {
                Id = m.Id,
                SenderName = m.Sender?.FullName ?? "",
                Text = m.Text,
                Direction = m.Direction,
                CreatedAt = m.CreatedAt
            }).ToList());
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return Guid.Parse(claim!.Value);
        }
    }
}