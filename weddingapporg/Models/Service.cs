using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace weddingapporg.Models
{
    public class Service
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "Service Type is required")]
        [Display(Name = "Service Type")]
        public int ServiceType { get; set; }

        [Required(ErrorMessage = "Service Name is required")]
        [StringLength(200)]
        [Display(Name = "Service Name")]
        public string Service_Name { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, 1000000)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Display(Name = "Capacity")]
        public int? Capacity { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        [Display(Name = "City")]
        public string City { get; set; }

        [StringLength(500)]
        [Display(Name = "Address")]
        public string Address { get; set; }

        // رابط الصورة الرئيسية
        [StringLength(500)]
        [Display(Name = "Main Image")]
        public string MainImage { get; set; }

        // وصف الخدمة
        [StringLength(2000)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        // معرف المنظم (مؤقتاً)
        public string? OrganizerId { get; set; } = "temp-organizer";
    }
}