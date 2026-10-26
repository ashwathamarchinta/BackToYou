using BackToYou.Data;
using BackToYou.Models.Entities;
using BackToYou.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackToYou.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalItems = await _context.Items.CountAsync(),
                TotalLost = await _context.Items.CountAsync(i => i.Type == ItemType.Lost),
                TotalFound = await _context.Items.CountAsync(i => i.Type == ItemType.Found),
                TotalPendingClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Pending),
                TotalApprovedClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Approved),
                TotalRegisteredUsers = await _userManager.Users.CountAsync(),
                RecentItems = await _context.Items
                    .Include(i => i.Category)
                    .Include(i => i.Reporter)
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(5)
                    .ToListAsync(),
                PendingClaims = await _context.Claims
                    .Include(c => c.Item)
                    .Include(c => c.Claimer)
                    .Where(c => c.Status == ClaimStatus.Pending)
                    .OrderByDescending(c => c.SubmittedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // GET: /Admin/ManageClaims
        public async Task<IActionResult> ManageClaims(ClaimStatus? status = null)
        {
            var query = _context.Claims
                .Include(c => c.Item)
                    .ThenInclude(i => i!.Category)
                .Include(c => c.Item)
                    .ThenInclude(i => i!.Reporter)
                .Include(c => c.Claimer)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            ViewBag.SelectedStatus = status;
            var claims = await query.OrderByDescending(c => c.SubmittedAt).ToListAsync();
            return View(claims);
        }

        // POST: /Admin/ReviewClaim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewClaim(int claimId, ClaimStatus status, string? adminRemarks)
        {
            var claim = await _context.Claims
                .Include(c => c.Item)
                .FirstOrDefaultAsync(c => c.Id == claimId);

            if (claim == null)
            {
                return NotFound();
            }

            claim.Status = status;
            claim.AdminRemarks = adminRemarks;
            claim.ReviewedAt = DateTime.UtcNow;

            // Update item status based on claim decision
            if (claim.Item != null)
            {
                if (status == ClaimStatus.Approved)
                {
                    claim.Item.Status = ItemStatus.Claimed;
                }
                else if (status == ClaimStatus.Rejected)
                {
                    // Check if other pending claims exist for this item
                    var otherPendingClaims = await _context.Claims
                        .AnyAsync(c => c.ItemId == claim.ItemId && c.Id != claim.Id && c.Status == ClaimStatus.Pending);

                    if (!otherPendingClaims && claim.Item.Status == ItemStatus.UnderClaim)
                    {
                        claim.Item.Status = ItemStatus.Reported;
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Claim #{claimId} has been marked as {status}.";
            return RedirectToAction(nameof(ManageClaims));
        }

        // GET: /Admin/ManageItems
        public async Task<IActionResult> ManageItems(ItemType? type = null, ItemStatus? status = null)
        {
            var query = _context.Items
                .Include(i => i.Category)
                .Include(i => i.Reporter)
                .Include(i => i.Claims)
                .AsQueryable();

            if (type.HasValue)
            {
                query = query.Where(i => i.Type == type.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(i => i.Status == status.Value);
            }

            ViewBag.SelectedType = type;
            ViewBag.SelectedStatus = status;

            var items = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
            return View(items);
        }

        // POST: /Admin/DeleteItem/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Item '{item.Title}' was deleted successfully.";
            }

            return RedirectToAction(nameof(ManageItems));
        }

        // GET: /Admin/ManageUsers
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var userViewModels = new List<UserManagementViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var reportedCount = await _context.Items.CountAsync(i => i.ReporterUserId == user.Id);
                var claimsCount = await _context.Claims.CountAsync(c => c.ClaimerUserId == user.Id);

                userViewModels.Add(new UserManagementViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    StudentRollNumber = user.StudentRollNumber,
                    Department = user.Department,
                    PhoneNumber = user.PhoneNumber,
                    CreatedAt = user.CreatedAt,
                    Roles = string.Join(", ", roles),
                    ReportedCount = reportedCount,
                    ClaimsCount = claimsCount
                });
            }

            return View(userViewModels);
        }
    }
}
