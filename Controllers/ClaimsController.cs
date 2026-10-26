using BackToYou.Data;
using BackToYou.Models.Entities;
using BackToYou.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackToYou.Controllers
{
    [Authorize]
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public ClaimsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // GET: /Claims/SubmitClaim?itemId=5
        [HttpGet]
        public async Task<IActionResult> SubmitClaim(int itemId)
        {
            var item = await _context.Items
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);

            // Cannot claim own item
            if (item.ReporterUserId == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot submit a claim for an item you reported yourself.";
                return RedirectToAction("Details", "Items", new { id = itemId });
            }

            // Cannot claim already claimed/closed item
            if (item.Status == ItemStatus.Claimed || item.Status == ItemStatus.Closed)
            {
                TempData["ErrorMessage"] = "This item has already been claimed or closed.";
                return RedirectToAction("Details", "Items", new { id = itemId });
            }

            // Check if already claimed by this user
            var existingClaim = await _context.Claims
                .FirstOrDefaultAsync(c => c.ItemId == itemId && c.ClaimerUserId == currentUserId);

            if (existingClaim != null)
            {
                TempData["InfoMessage"] = "You have already submitted a claim for this item. Check its status below.";
                return RedirectToAction(nameof(MyClaims));
            }

            var viewModel = new ClaimSubmitViewModel
            {
                ItemId = item.Id,
                ItemTitle = item.Title,
                ItemImagePath = item.ImagePath,
                ItemLocation = item.Location,
                ItemEventDate = item.EventDate
            };

            return View(viewModel);
        }

        // POST: /Claims/SubmitClaim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitClaim(ClaimSubmitViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var item = await _context.Items.FindAsync(model.ItemId);
            if (item == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                string? proofPath = null;

                if (model.ProofFile != null && model.ProofFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
                    var ext = Path.GetExtension(model.ProofFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("ProofFile", "Please upload a valid image or PDF document.");
                    }
                    else if (model.ProofFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ProofFile", "File size must not exceed 5MB.");
                    }
                    else
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "proofs");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ProofFile.CopyToAsync(fileStream);
                        }

                        proofPath = $"/uploads/proofs/{uniqueFileName}";
                    }
                }

                if (ModelState.IsValid)
                {
                    var claim = new Claim
                    {
                        ItemId = model.ItemId,
                        ClaimerUserId = user.Id,
                        DescriptionProof = model.DescriptionProof,
                        ProofImagePath = proofPath,
                        SubmittedAt = DateTime.UtcNow,
                        Status = ClaimStatus.Pending
                    };

                    _context.Claims.Add(claim);

                    // Mark item as UnderClaim if it was Reported
                    if (item.Status == ItemStatus.Reported)
                    {
                        item.Status = ItemStatus.UnderClaim;
                        _context.Items.Update(item);
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Your claim request has been submitted for verification. Track its status below.";
                    return RedirectToAction(nameof(MyClaims));
                }
            }

            return View(model);
        }

        // GET: /Claims/MyClaims
        [HttpGet]
        public async Task<IActionResult> MyClaims()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var claims = await _context.Claims
                .Include(c => c.Item)
                    .ThenInclude(i => i!.Category)
                .Include(c => c.Item)
                    .ThenInclude(i => i!.Reporter)
                .Where(c => c.ClaimerUserId == user.Id)
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync();

            return View(claims);
        }
    }
}
