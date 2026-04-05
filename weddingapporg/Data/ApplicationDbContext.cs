using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using weddingapporg.Data;
using weddingapporg.Models;

namespace weddingapporg.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)

    {
        public DbSet<Service> Services { get; set; }
    }

}



