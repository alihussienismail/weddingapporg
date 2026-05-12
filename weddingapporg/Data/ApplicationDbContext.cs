using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using weddingapporg.Data;
using weddingapporg.Models;

namespace weddingapporg.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Service> Services { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Booking>(entity =>
            {
                entity.HasOne(b => b.Service)
                      .WithMany()
                      .HasForeignKey(b => b.ServiceId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.User)
                      .WithMany()
                      .HasForeignKey(b => b.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}




