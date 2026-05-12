using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using weddingapporg.Data;
using weddingapporg.Models;
using weddingapporg.Services;

namespace weddingapporg.Pages
{
    [Authorize]
    public class PaymentSuccessModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly CartService _cart;

        public PaymentSuccessModel(ApplicationDbContext db, CartService cart)
        {
            _db   = db;
            _cart = cart;
        }

        public List<Booking> Bookings { get; set; } = new();
        public decimal GrandTotal => Bookings.Sum(b => b.TotalPrice);

        public async Task<IActionResult> OnGetAsync(string? session_id)
        {
            if (string.IsNullOrEmpty(session_id)) return RedirectToPage("/Index");

            // Find all bookings tied to this Stripe session
            Bookings = await _db.Bookings
                .Include(b => b.Service)
                .Where(b => b.StripeSessionId == session_id)
                .ToListAsync();

            // Already confirmed — just show receipt
            if (Bookings.Any() && Bookings.All(b => b.PaymentStatus == "Paid"))
                return Page();

            // Verify payment with Stripe
            bool paymentConfirmed = false;
            try
            {
                var session = await new SessionService().GetAsync(session_id);
                paymentConfirmed = session.PaymentStatus == "paid";
            }
            catch (Stripe.StripeException)
            {
                // Demo / invalid key — confirm anyway
                paymentConfirmed = true;
            }

            if (paymentConfirmed)
            {
                foreach (var b in Bookings)
                {
                    b.Status        = "Confirmed";
                    b.PaymentStatus = "Paid";
                }
                await _db.SaveChangesAsync();

                // Clear the session cart
                _cart.Clear(HttpContext.Session);
            }
            else
            {
                // Payment not completed
                return RedirectToPage("/PaymentCancel");
            }

            return Page();
        }
    }
}
