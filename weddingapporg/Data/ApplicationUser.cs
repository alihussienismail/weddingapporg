using Microsoft.AspNetCore.Identity;

namespace weddingapporg.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? FName { get; set; }
        public string? LName { get; set; }
        public bool? gender { get; set; }
    }
}
