using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using weddingapporg.Data;
using weddingapporg.Models;
using weddingapporg.Services;

namespace weddingapporg.Pages
{
    public class ServiceDetailModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CartService _cart;
        private readonly IConfiguration _config;

        public ServiceDetailModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
                                  CartService cart, IConfiguration config)
        {
            _db = db;
            _userManager = userManager;
            _cart = cart;
            _config = config;
        }

        public Service? Service { get; set; }
        public bool IsVenue => Service?.ServiceType == 1;
        public List<Service> RecommendedServices { get; set; } = new();
        public List<DateTime> BookedDates { get; set; } = new();
        public List<(int Start, int End)> BookedSlots { get; set; } = new();

        // Form inputs
        [BindProperty] public DateTime BookingDate { get; set; } = DateTime.Today.AddDays(1);
        [BindProperty] public int StartHour { get; set; } = 9;
        [BindProperty] public int EndHour { get; set; } = 11;
        [BindProperty] public string? Notes { get; set; }

        public string? PageMessage { get; set; }

        // Type display helpers
        private static readonly Dictionary<int, string[]> TypeInfo = new()
        {
            { 2, new[] { "Catering",              "restaurant_menu" } },
            { 3, new[] { "Photography",           "photo_camera"    } },
            { 4, new[] { "Decoration",            "local_florist"   } },
            { 5, new[] { "Music & Entertainment", "music_note"      } },
            { 6, new[] { "Transportation",        "directions_car"  } },
            { 7, new[] { "Other",                 "room_service"    } },
        };
        public string TypeLabel(int t) => TypeInfo.ContainsKey(t) ? TypeInfo[t][0] : "Service";
        public string TypeIcon(int t)  => TypeInfo.ContainsKey(t) ? TypeInfo[t][1] : "room_service";

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Service = await _db.Services.FindAsync(id);
            if (Service == null) return NotFound();

            await LoadBookedInfo(id);
            if (IsVenue) await LoadRecommended();

            if (TempData["CartAdded"] is string added)
                PageMessage = $"✅ \"{added}\" added to cart!";

            return Page();
        }

        // Add THIS service to cart → redirect back (if venue) or to cart
        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            Service = await _db.Services.FindAsync(id);
            if (Service == null) return NotFound();

            var bookingDate = BookingDate.Date;
            if (bookingDate < DateTime.Today) bookingDate = DateTime.Today.AddDays(1);

            decimal total;
            int? startH = null, endH = null;

            if (IsVenue)
            {
                total = Service.Price;
            }
            else
            {
                if (EndHour <= StartHour)
                {
                    await LoadBookedInfo(id);
                    TempData["DetailError"] = "End hour must be after start hour.";
                    return RedirectToPage(new { id });
                }
                startH = StartHour;
                endH   = EndHour;
                total  = Service.Price * (EndHour - StartHour);
            }

            var item = new CartItem
            {
                ServiceId   = Service.Id,
                ServiceName = Service.Service_Name ?? "",
                ServiceType = Service.ServiceType,
                UnitPrice   = Service.Price,
                MainImage   = Service.MainImage,
                City        = Service.City,
                BookingDate = bookingDate,
                StartHour   = startH,
                EndHour     = endH,
                TotalPrice  = total,
                Notes       = Notes
            };

            _cart.Add(HttpContext.Session, item);
            TempData["CartAdded"] = Service.Service_Name;

            // Venue page: stay so user can add add-ons; otherwise go to cart
            return IsVenue
                ? RedirectToPage(new { id })
                : RedirectToPage("/Cart");
        }

        private async Task LoadBookedInfo(Guid id)
        {
            if (IsVenue)
            {
                BookedDates = await _db.Bookings
                    .Where(b => b.ServiceId == id && b.Status == "Confirmed" && b.PaymentStatus == "Paid")
                    .Select(b => b.BookingDate.Date).Distinct().ToListAsync();
            }
            else
            {
                var raw = await _db.Bookings
                    .Where(b => b.ServiceId == id && b.Status == "Confirmed" && b.PaymentStatus == "Paid")
                    .Select(b => new { b.StartHour, b.EndHour }).ToListAsync();
                BookedSlots = raw.Where(x => x.StartHour.HasValue && x.EndHour.HasValue)
                    .Select(x => (x.StartHour!.Value, x.EndHour!.Value)).ToList();
            }
        }

        private async Task LoadRecommended()
        {
            RecommendedServices = await _db.Services
                .Where(s => s.ServiceType >= 2 && s.ServiceType <= 6)
                .GroupBy(s => s.ServiceType)
                .Select(g => g.First())
                .ToListAsync();
        }
    }
}
