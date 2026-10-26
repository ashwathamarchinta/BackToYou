using System.ComponentModel.DataAnnotations;

namespace BackToYou.Models.Entities
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(50)]
        [Display(Name = "Bootstrap Icon")]
        public string IconClass { get; set; } = "bi-box";

        public virtual ICollection<Item> Items { get; set; } = new List<Item>();
    }
}
