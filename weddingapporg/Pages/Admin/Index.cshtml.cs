using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using weddingapporg.Data;
using System.Threading.Tasks;

namespace weddingapporg.Pages.Admin
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            // إذا لم يكن المستخدم أدمن، يذهب إلى الصفحة الرئيسية
            if (user == null || !user.IsAdmin)
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }
    }
}