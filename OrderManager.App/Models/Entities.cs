using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManager.App.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Role { get; set; } = string.Empty; // Client, Manager, Developer
        public bool IsBlocked { get; set; } = false;
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<DevTask> Tasks { get; set; } = new List<DevTask>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
      
    }

    public class Order
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ClientId { get; set; }

        [ForeignKey("ClientId")]
        public User Client { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Type { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Status { get; set; } = "New";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? Deadline { get; set; }

        public ICollection<DevTask> Tasks { get; set; } = new List<DevTask>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }

    public class DevTask
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order Order { get; set; } = null!;

        public Guid? AssignedToId { get; set; }

        [ForeignKey("AssignedToId")]
        public User? AssignedTo { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Backlog";

        public DateTime? Deadline { get; set; }
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }

    public class Message
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order Order { get; set; } = null!;

        [Required]
        public Guid SenderId { get; set; }

        [ForeignKey("SenderId")]
        public User Sender { get; set; } = null!;

        [Required]
        public string Text { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Direction { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}