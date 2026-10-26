using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackToYou.Models.Entities
{
    public class Claim
    {
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }
        public virtual Item? Item { get; set; }

        [Required]
        public string ClaimerUserId { get; set; } = string.Empty;

        [ForeignKey("ClaimerUserId")]
        public virtual ApplicationUser? Claimer { get; set; }

        [Required]
        [StringLength(1000)]
        [Display(Name = "Proof of Ownership / Identifying Details")]
        public string DescriptionProof { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Proof Document / Bill / Photo")]
        public string? ProofImagePath { get; set; }

        [Display(Name = "Submission Date")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Review Date")]
        public DateTime? ReviewedAt { get; set; }

        [Display(Name = "Claim Status")]
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        [StringLength(500)]
        [Display(Name = "Admin Feedback / Handover Instructions")]
        public string? AdminRemarks { get; set; }
    }
}
