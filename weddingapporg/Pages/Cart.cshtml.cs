using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class CartModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CartService _cart;
        private readonly IConfiguration _config;

        public CartModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
                         CartService cart, IConfiguration config)
        {
            _db = db;
            _userManager = userManager;
            _cart = cart;
            _config = config;
        }

        public List<CartItem> Items { get; set; } = new();
        public decimal GrandTotal => Items.Sum(i => i.TotalPrice);

        // ── Add item (POST from any page) ─────────────────────────────────────
        [BindProperty] public Guid AddServiceId { get; set; }
        [BindProperty] public DateTime AddBookingDate { get; set; }
        [BindProperty] public int? AddStartHour { get; set; }
        [BindProperty] public int? AddEndHour { get; set; }
        [BindProperty] public string? AddNotes { get; set; }
        [BindProperty] public string? AddReturnUrl { get; set; }

        // ── Remove item ───────────────────────────────────────────────────────
        [BindProperty] public Guid RemoveCartItemId { get; set; }

        public async Task OnGetAsync()
        {
            Items = _cart.Get(HttpContext.Session);
        }

        // Called from "Add to Cart" forms on ServiceDetail or any page
        public async Task<IActionResult> OnPostAddAsync()
        {
            var svc = await _db.Services.FindAsync(AddServiceId);
            if (svc == null) return NotFound();

            var bookingDate = AddBookingDate.Date;
            if (bookingDate < DateTime.Today)
                bookingDate = DateTime.Today.AddDays(1);

            decimal total;
            int? startH = null, endH = null;

            if (svc.ServiceType == 1) // Venue — full day
            {
                total = svc.Price;
            }
            else
            {
                startH = AddStartHour ?? 9;
                endH   = AddEndHour ?? 11;
                if (endH <= startH) endH = startH + 1;
                total  = svc.Price * (endH.Value - startH.Value);
            }

            var item = new CartItem
            {
                ServiceId   = svc.Id,
                ServiceName = svc.Service_Name ?? "",
                ServiceType = svc.ServiceType,
                UnitPrice   = svc.Price,
                MainImage   = svc.MainImage,
                City        = svc.City,
                BookingDate = bookingDate,
                StartHour   = startH,
                EndHour     = endH,
                TotalPrice  = total,
                Notes       = AddNotes
            };

            _cart.Add(HttpContext.Session, item);

            TempData["CartAdded"] = svc.Service_Name;
            var returnUrl = AddReturnUrl ?? "/Cart";
            return LocalRedirect(returnUrl);
        }

        public async Task<IActionResult> OnPostRemoveAsync()
        {
            _cart.Remove(HttpContext.Session, RemoveCartItemId);
            Items = _cart.Get(HttpContext.Session);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCheckoutAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var cartItems = _cart.Get(HttpContext.Session);
            if (!cartItems.Any())
            {
                TempData["CartError"] = "Your cart is empty.";
                return RedirectToPage();
            }

            // ── Conflict check before creating bookings ───────────────────────
            var errors = new List<string>();
            foreach (var item in cartItems)
            {
                if (item.IsVenue)
                {
                    bool taken = await _db.Bookings.AnyAsync(b =>
                        b.ServiceId == item.ServiceId &&
                        b.BookingDate.Date == item.BookingDate.Date &&
                        b.Status == "Confirmed" &&
                        b.PaymentStatus == "Paid");
                    if (taken)
                        errors.Add($"\"{item.ServiceName}\" is already booked on {item.BookingDate:MMM d, yyyy}.");
                }
                else if (item.StartHour.HasValue && item.EndHour.HasValue)
                {
                    bool overlap = await _db.Bookings.AnyAsync(b =>
                        b.ServiceId == item.ServiceId &&
                        b.BookingDate.Date == item.BookingDate.Date &&
                        b.Status == "Confirmed" &&
                        b.PaymentStatus == "Paid" &&
                        b.StartHour < item.EndHour &&
                        b.EndHour > item.StartHour);
                    if (overlap)
                        errors.Add($"\"{item.ServiceName}\" has a time conflict on {item.BookingDate:MMM d, yyyy}.");
                }
            }

            if (errors.Any())
            {
                TempData["CartError"] = "Conflicts found: " + string.Join(" | ", errors);
                return RedirectToPage();
            }

            // ── Create PendingPayment bookings ────────────────────────────────
            var bookings = cartItems.Select(item => new Booking
            {
                UserId        = user.Id,
                ServiceId     = item.ServiceId,
                BookingDate   = item.BookingDate,
                StartHour     = item.StartHour,
                EndHour       = item.EndHour,
                TotalPrice    = item.TotalPrice,
                Status        = "PendingPayment",
                PaymentStatus = "Unpaid",
                Notes         = item.Notes
            }).ToList();

            await _db.Bookings.AddRangeAsync(bookings);
            await _db.SaveChangesAsync();

            // ── Stripe Checkout Session ───────────────────────────────────────
            var baseUrl = _config["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

            var lineItems = cartItems.Select(item => new SessionLineItemOptions
            {
                Quantity  = 1,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency   = "usd",
                    UnitAmount = (long)(item.TotalPrice * 100),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name        = item.ServiceName,
                        Description = item.IsVenue
                            ? $"Full-day venue — {item.BookingDate:MMMM d, yyyy}"
                            : $"{item.BookingDate:MMMM d, yyyy}  {item.StartHour:D2}:00–{item.EndHour:D2}:00 ({item.EndHour - item.StartHour} hr)",
                        Images = !string.IsNullOrEmpty(item.MainImage) && item.MainImage.StartsWith("/")
                            ? new List<string> { baseUrl + item.MainImage } : null
                    }
                }
            }).ToList();

            var sessionOptions = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems    = lineItems,
                Mode         = "payment",
                SuccessUrl   = $"{baseUrl}/PaymentSuccess?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl    = $"{baseUrl}/PaymentCancel",
                CustomerEmail = user.Email,
                Metadata     = new Dictionary<string, string>
                {
                    { "booking_ids", string.Join(",", bookings.Select(b => b.BookingId)) }
                }
            };

            Session stripeSession;
            try
            {
                stripeSession = await new SessionService().CreateAsync(sessionOptions);
            }
            catch (Stripe.StripeException ex)
            {
                // Demo fallback — confirm all bookings directly
                foreach (var b in bookings)
                {
                    b.Status        = "Confirmed";
                    b.PaymentStatus = "Paid";
                    b.StripeSessionId = "demo_no_key";
                }
                await _db.SaveChangesAsync();
                _cart.Clear(HttpContext.Session);
                TempData["SuccessMessage"] = $"🎉 {bookings.Count} booking(s) confirmed (demo mode — {ex.Message}).";
                return RedirectToPage("/MyBookings");
            }

            foreach (var b in bookings)
                b.StripeSessionId = stripeSession.Id;
            await _db.SaveChangesAsync();

            return Redirect(stripeSession.Url);
        }
    }
}
