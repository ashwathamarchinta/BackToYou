using BackToYou.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BackToYou.Models.ViewModels
{
    public class ItemCreateViewModel
    {
        [Required(ErrorMessage = "Item title is required")]
        [StringLength(120, ErrorMessage = "Title cannot exceed 120 characters")]
        [Display(Name = "Item Name / Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Detailed Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select report type")]
        [Display(Name = "Report Type")]
        public ItemType Type { get; set; } // Lost or Found

        [Required(ErrorMessage = "Please select a category")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(150, ErrorMessage = "Location cannot exceed 150 characters")]
        [Display(Name = "Location (e.g. Block B 2nd floor, Library, Sports Ground)")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date is required")]
        [Display(Name = "Date Lost / Found")]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; } = DateTime.Today;

        [Display(Name = "Upload Photo (Optional for Lost, Recommended for Found)")]
        public IFormFile? ImageFile { get; set; }

        // For category dropdown in form
        public IEnumerable<SelectListItem>? Categories { get; set; }
    }

    public class ItemFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public ItemType? Type { get; set; }
        public ItemStatus? Status { get; set; }

        public IEnumerable<Item> Items { get; set; } = new List<Item>();
        public IEnumerable<SelectListItem>? Categories { get; set; }
    }

    public class CategorySummaryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }
}
