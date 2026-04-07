using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using weddingapporg.Data;
using weddingapporg.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace weddingapporg.Controllers.Admin
{
    public class AddNewServicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AddNewServicesController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: AddNewServices
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services.ToListAsync();
            return View(services);
        }

        // GET: AddNewServices/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            var service = await _context.Services
                .FirstOrDefaultAsync(m => m.Id == id);

            if (service == null)
                return NotFound();

            return View(service);
        }

        // GET: AddNewServices/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AddNewServices/Create (دالة واحدة فقط - تم دمج الاثنين)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                service.Id = Guid.NewGuid();

                // معالجة الصورة إذا تم رفعها
                if (imageFile != null && imageFile.Length > 0)
                {
                    // إنشاء مسار مجلد uploads في wwwroot
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    service.MainImage = "/uploads/" + uniqueFileName;  // حفظ المسار
                }

                _context.Add(service);
                await _context.SaveChangesAsync();
                return Redirect("/AddNewServices/Index");
            }
            return View(service);
        }

        // GET: AddNewServices/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var service = await _context.Services.FindAsync(id);

            if (service == null)
                return NotFound();

            return View(service);
        }

        // POST: AddNewServices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Service service, IFormFile? imageFile)
        {
            if (id != service.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // معالجة الصورة الجديدة إذا تم رفعها
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        service.MainImage = "/uploads/" + uniqueFileName;
                    }

                    _context.Update(service);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(service.Id))
                        return NotFound();
                    else
                        throw;
                }
                // العودة إلى صفحة AddNewServices (Index)
                return Redirect("/AddNewServices/Index");
            }
            return View(service);
        }

        // GET: AddNewServices/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var service = await _context.Services
                .FirstOrDefaultAsync(m => m.Id == id);

            if (service == null)
                return NotFound();

            return View(service);
        }

        // POST: AddNewServices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ServiceExists(Guid id)
        {
            return _context.Services.Any(e => e.Id == id);
        }
    }
}