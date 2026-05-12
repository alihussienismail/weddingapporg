using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using weddingapporg.Data;

namespace weddingapporg.Models
{
    public class Booking
    {
        [Key]
        public Guid BookingId { get; set; } = Guid.NewGuid();

        /// <summary>The user who made this booking</summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        /// <summary>The service being booked</summary>
        [Required]
        public Guid ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public Service? Service { get; set; }

        /// <summary>The date of the booking (date portion only)</summary>
        [Required]
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }

        /// <summary>
        /// Start hour (0-23). NULL for Venue (type=1) bookings — they occupy the whole day.
        /// </summary>
        public int? StartHour { get; set; }

        /// <summary>
        /// End hour (1-24, exclusive). NULL for Venue bookings.
        /// Example: StartHour=14, EndHour=17 → 2:00 PM – 5:00 PM (3 hours).
        /// </summary>
        public int? EndHour { get; set; }

        /// <summary>
        /// Total price. For venues: Service.Price. For additional: Service.Price × (EndHour - StartHour).
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        /// <summary>PendingPayment | Confirmed | Cancelled</summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "PendingPayment";

        /// <summary>Stripe Checkout Session ID — set when a session is created</summary>
        [StringLength(200)]
        public string? StripeSessionId { get; set; }

        /// <summary>Unpaid | Paid | Refunded</summary>
        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Unpaid";

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
