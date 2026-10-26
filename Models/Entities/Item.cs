using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackToYou.Models.Entities
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        [Display(Name = "Item Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Report Type")]
        public ItemType Type { get; set; } // Lost or Found

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Location (e.g., Library 2nd Floor, Cafeteria)")]
        public string Location { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Date Lost / Found")]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; } = DateTime.Today;

        [Display(Name = "Date Reported")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(255)]
        [Display(Name = "Item Photo")]
        public string? ImagePath { get; set; }

        [Display(Name = "Status")]
        public ItemStatus Status { get; set; } = ItemStatus.Reported;

        [Required]
        public string ReporterUserId { get; set; } = string.Empty;

        [ForeignKey("ReporterUserId")]
        public virtual ApplicationUser? Reporter { get; set; }

        // Claims submitted for this item
        public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
    }
}
