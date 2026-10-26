using BackToYou.Data;
using BackToYou.Models.Entities;
using BackToYou.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BackToYou.Controllers
{
    public class ItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public ItemsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // GET: /Items
        public async Task<IActionResult> Index(string? searchTerm, int? categoryId, ItemType? type, ItemStatus? status)
        {
            var query = _context.Items
                .Include(i => i.Category)
                .Include(i => i.Reporter)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(i => i.Title.ToLower().Contains(term) 
                                      || i.Description.ToLower().Contains(term)
                                      || i.Location.ToLower().Contains(term));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(i => i.CategoryId == categoryId.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(i => i.Type == type.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(i => i.Status == status.Value);
            }

            var items = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();

            var categories = await _context.Categories.ToListAsync();
            var categorySelectList = new SelectList(categories, "Id", "Name", categoryId);

            var viewModel = new ItemFilterViewModel
            {
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                Type = type,
                Status = status,
                Items = items,
                Categories = categorySelectList
            };

            return View(viewModel);
        }

        // GET: /Items/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Reporter)
                .Include(i => i.Claims)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            ViewBag.IsReporter = currentUserId != null && currentUserId == item.ReporterUserId;
            ViewBag.HasClaimed = currentUserId != null && item.Claims.Any(c => c.ClaimerUserId == currentUserId);

            return View(item);
        }

        // GET: /Items/Create
        [Authorize]
        public async Task<IActionResult> Create(ItemType? type = null)
        {
            var categories = await _context.Categories.ToListAsync();
            var viewModel = new ItemCreateViewModel
            {
                Type = type ?? ItemType.Lost,
                EventDate = DateTime.Today,
                Categories = new SelectList(categories, "Id", "Name")
            };

            return View(viewModel);
        }

        // POST: /Items/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (ModelState.IsValid)
            {
                string? imagePath = null;

                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    // Validate image extension
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var ext = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("ImageFile", "Please upload a valid image file (.jpg, .png, .webp).");
                    }
                    else if (model.ImageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ImageFile", "Image size must not exceed 5MB.");
                    }
                    else
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "items");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ImageFile.CopyToAsync(fileStream);
                        }

                        imagePath = $"/uploads/items/{uniqueFileName}";
                    }
                }

                if (ModelState.IsValid)
                {
                    var item = new Item
                    {
                        Title = model.Title,
                        Description = model.Description,
                        Type = model.Type,
                        CategoryId = model.CategoryId,
                        Location = model.Location,
                        EventDate = model.EventDate,
                        CreatedAt = DateTime.UtcNow,
                        ImagePath = imagePath,
                        Status = ItemStatus.Reported,
                        ReporterUserId = user.Id
                    };

                    _context.Add(item);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Your {(item.Type == ItemType.Lost ? "Lost" : "Found")} item report has been published successfully!";
                    return RedirectToAction(nameof(Details), new { id = item.Id });
                }
            }

            // Reload categories if model state invalid
            var categories = await _context.Categories.ToListAsync();
            model.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
            return View(model);
        }

        // GET: /Items/MyItems
        [Authorize]
        public async Task<IActionResult> MyItems()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var items = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Claims)
                .Where(i => i.ReporterUserId == user.Id)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(items);
        }

        // POST: /Items/CloseStatus/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseStatus(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            // Check permission: owner or admin
            var isAdmin = User.IsInRole("Admin");
            if (item.ReporterUserId != user?.Id && !isAdmin)
            {
                return Forbid();
            }

            item.Status = ItemStatus.Closed;
            _context.Update(item);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "The item status has been marked as Closed/Resolved.";
            return RedirectToAction(nameof(MyItems));
        }
    }
}
