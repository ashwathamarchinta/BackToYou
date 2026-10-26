using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BackToYou.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [PersonalData]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [PersonalData]
        [StringLength(50)]
        [Display(Name = "Student Roll Number / Staff ID")]
        public string? StudentRollNumber { get; set; }

        [PersonalData]
        [StringLength(100)]
        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Display(Name = "Registration Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Item> ReportedItems { get; set; } = new List<Item>();
        public virtual ICollection<Claim> SubmittedClaims { get; set; } = new List<Claim>();
    }
}
