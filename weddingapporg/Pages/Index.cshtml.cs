using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using weddingapporg.Data;
using weddingapporg.Models;

namespace weddingapporg.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Service> VenueServices { get; set; } = new();
        public List<Service> AdditionalServices { get; set; } = new();

        public async Task OnGetAsync()
        {
            VenueServices = await _db.Services
                .Where(s => s.ServiceType == 1)
                .OrderBy(s => s.Service_Name)
                .Take(3)
                .ToListAsync();

            AdditionalServices = await _db.Services
                .Where(s => s.ServiceType != 1)
                .OrderBy(s => s.ServiceType)
                .Take(6)
                .ToListAsync();
        }
    }
}
