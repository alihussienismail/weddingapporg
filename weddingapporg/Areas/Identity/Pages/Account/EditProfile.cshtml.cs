using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using weddingapporg.Data;

namespace weddingapporg.Areas.Identity.Pages.Account
{
    [Authorize] // يسمح فقط للمستخدمين المسجلين
    public class EditProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public EditProfileModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string CurrentProfilePicturePath { get; set; }

        public class InputModel
        {
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Display(Name = "First Name")]
            public string FName { get; set; }

            [Display(Name = "Last Name")]
            public string LName { get; set; }

            [Phone]
            [Display(Name = "Phone Number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Address")]
            public string Address { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Date of Birth")]
            public DateTime? DateOfBirth { get; set; }

            [Display(Name = "Gender")]
            public bool? Gender { get; set; }

            [Display(Name = "Profile Picture")]
            public IFormFile ProfileImage { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Load current data
            Input = new InputModel
            {
                Email = user.Email,
                FName = user.FName,
                LName = user.LName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                DateOfBirth = user.DateOfBirth,
                Gender = user.gender
            };

            CurrentProfilePicturePath = user.ProfilePicturePath;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                // Reload current profile picture path to display
                CurrentProfilePicturePath = user.ProfilePicturePath;
                return Page();
            }

            // Update basic info
            user.Email = Input.Email;
            user.FName = Input.FName;
            user.LName = Input.LName;
            user.PhoneNumber = Input.PhoneNumber;
            user.Address = Input.Address;
            user.DateOfBirth = Input.DateOfBirth;
            user.gender = Input.Gender;

            // Handle profile picture upload
            if (Input.ProfileImage != null && Input.ProfileImage.Length > 0)
            {
                try
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);
                       // Delete old picture if exists
                    if (!string.IsNullOrEmpty(user.ProfilePicturePath))
                    {
                        var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePicturePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }

                    // Save new picture
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(Input.ProfileImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await Input.ProfileImage.CopyToAsync(stream);
                    }

                    user.ProfilePicturePath = "/uploads/" + uniqueFileName;
                }
                catch
                {
                    ModelState.AddModelError(string.Empty, "An error occurred while uploading the profile picture.");
                    CurrentProfilePicturePath = user.ProfilePicturePath;
                    return Page();
                }
            }

            // Update user in database
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                CurrentProfilePicturePath = user.ProfilePicturePath;
                return Page();
            }

            // Refresh sign-in to reflect changes (optional but good practice)
            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Your profile has been updated successfully.";
            return RedirectToPage("/index"); // Stay on same page with success message
        }
    }
}