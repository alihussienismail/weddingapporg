using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace weddingapporg.Pages.Admin
{
    [Authorize]  // يمنع الوصول غير المسجلين
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}