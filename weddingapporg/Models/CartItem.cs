namespace weddingapporg.Models
{
    public class CartItem
    {
        public Guid CartItemId { get; set; } = Guid.NewGuid();
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = "";
        public int ServiceType { get; set; }
        public decimal UnitPrice { get; set; }
        public string? MainImage { get; set; }
        public string? City { get; set; }
        public DateTime BookingDate { get; set; }
        public int? StartHour { get; set; }
        public int? EndHour { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public bool IsVenue => ServiceType == 1;
    }
}
