using BackToYou.Data;
using BackToYou.Models.Entities;
using BackToYou.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BackToYou.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Landing page statistics
            ViewBag.TotalLost = await _context.Items.CountAsync(i => i.Type == ItemType.Lost);
            ViewBag.TotalFound = await _context.Items.CountAsync(i => i.Type == ItemType.Found);
            ViewBag.TotalResolved = await _context.Items.CountAsync(i => i.Status == ItemStatus.Claimed || i.Status == ItemStatus.Closed);
            ViewBag.TotalCategories = await _context.Categories.CountAsync();

            // Fetch categories for quick filter cards
            ViewBag.Categories = await _context.Categories
                .Select(c => new CategorySummaryViewModel {
                    Id = c.Id,
                    Name = c.Name,
                    IconClass = c.IconClass,
                    ItemCount = c.Items.Count(i => i.Status != ItemStatus.Closed)
                })
                .ToListAsync();

            // Fetch recent 6 active items
            var recentItems = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Reporter)
                .Where(i => i.Status != ItemStatus.Closed)
                .OrderByDescending(i => i.CreatedAt)
                .Take(6)
                .ToListAsync();

            return View(recentItems);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new Models.ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
