using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using A2_TechFest_2026.Models;

namespace A2_TechFest_2026.Data
{
    // CHANGED: Inherit from IdentityDbContext instead of DbContext
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Booking> Bookings { get; set; }
        public DbSet<EmailVerification> EmailVerifications { get; set; }
        public DbSet<PaymentVerification> PaymentVerifications { get; set; }
        public DbSet<Inquiry> Inquiries { get; set; }


        // KEEP this method - it has important Booking configuration
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.BookingId);
                entity.Property(e => e.BookingId).ValueGeneratedOnAdd();
                entity.Property(e => e.EventName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ClientName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ClientEmail).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ContactNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.TicketType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Charges).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PaymentStatus).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            });
        }
    }
}