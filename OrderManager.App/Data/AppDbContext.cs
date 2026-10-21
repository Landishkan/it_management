using Microsoft.EntityFrameworkCore;
using OrderManager.App.Models;

namespace OrderManager.App.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<DevTask> Tasks => Set<DevTask>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<Order>(e =>
            {
                e.HasOne(o => o.Client)
                 .WithMany(u => u.Orders)
                 .HasForeignKey(o => o.ClientId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DevTask>(e =>
            {
                e.HasOne(t => t.Order)
                 .WithMany(o => o.Tasks)
                 .HasForeignKey(t => t.OrderId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(t => t.AssignedTo)
                 .WithMany(u => u.Tasks)
                 .HasForeignKey(t => t.AssignedToId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Message>(e =>
            {
                e.HasOne(m => m.Order)
                 .WithMany(o => o.Messages)
                 .HasForeignKey(m => m.OrderId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(m => m.Sender)
                 .WithMany(u => u.Messages)
                 .HasForeignKey(m => m.SenderId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}