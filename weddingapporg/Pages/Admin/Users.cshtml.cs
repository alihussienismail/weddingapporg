using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using weddingapporg.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace weddingapporg.Pages.Admin
{
    [Authorize]
    public class UsersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UsersModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<ApplicationUser> Users { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !currentUser.IsAdmin)
            {
                return RedirectToPage("/Index");
            }

            Users = await _context.Users.ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostToggleAdminAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsAdmin = !user.IsAdmin;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToPage();
        }
    }
}
