using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using weddingapporg.Data;
using weddingapporg.Models;

namespace weddingapporg.Pages
{
    public class PaymentCancelModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public PaymentCancelModel(ApplicationDbContext db) => _db = db;

        public Booking? Booking { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? booking_id)
        {
            if (booking_id == null) return RedirectToPage("/Index");

            Booking = await _db.Bookings
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.BookingId == booking_id);

            if (Booking != null && Booking.Status == "PendingPayment")
            {
                Booking.Status        = "Cancelled";
                Booking.PaymentStatus = "Cancelled";
                await _db.SaveChangesAsync();
            }

            return Page();
        }
    }
}
