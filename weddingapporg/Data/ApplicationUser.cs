using Microsoft.AspNetCore.Identity;

namespace weddingapporg.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? FName { get; set; }
        public string? LName { get; set; }
        public bool? gender { get; set; }
        public string? ProfilePicturePath { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}
