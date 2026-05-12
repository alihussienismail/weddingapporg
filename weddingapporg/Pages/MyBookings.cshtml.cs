using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using weddingapporg.Data;
using weddingapporg.Models;

namespace weddingapporg.Pages
{
    [Authorize]
    public class MyBookingsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public MyBookingsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public List<Booking> Bookings { get; set; } = new();
        public bool IsAdmin { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            IsAdmin = user.IsAdmin;

            var query = _db.Bookings
                .Include(b => b.Service)
                .Include(b => b.User)
                .AsQueryable();

            // Admins see all bookings; regular users see only theirs
            if (!IsAdmin)
                query = query.Where(b => b.UserId == user.Id);

            Bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCancelAsync(Guid bookingId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var booking = await _db.Bookings.Include(b => b.Service).FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null) return NotFound();

            // Only admin or booking owner can cancel
            if (!user.IsAdmin && booking.UserId != user.Id)
                return Forbid();

            // Only allow cancelling confirmed, paid, future bookings
            if (booking.BookingDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Cannot cancel a past booking.";
                return RedirectToPage();
            }
            if (booking.Status != "Confirmed")
            {
                TempData["ErrorMessage"] = "Only confirmed bookings can be cancelled here.";
                return RedirectToPage();
            }

            booking.Status        = "Cancelled";
            booking.PaymentStatus = "Cancelled";
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Booking for \"{booking.Service?.Service_Name}\" on {booking.BookingDate:MMMM d, yyyy} has been cancelled.";
            return RedirectToPage();
        }
    }
}
