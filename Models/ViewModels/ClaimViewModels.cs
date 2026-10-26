using BackToYou.Models.Entities;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BackToYou.Models.ViewModels
{
    public class ClaimSubmitViewModel
    {
        [Required]
        public int ItemId { get; set; }

        public string ItemTitle { get; set; } = string.Empty;
        public string? ItemImagePath { get; set; }
        public string ItemLocation { get; set; } = string.Empty;
        public DateTime ItemEventDate { get; set; }

        [Required(ErrorMessage = "Please describe proof of ownership in detail.")]
        [StringLength(1000, MinimumLength = 15, ErrorMessage = "Proof description must be between 15 and 1000 characters.")]
        [Display(Name = "Proof of Ownership / Identifying Features")]
        public string DescriptionProof { get; set; } = string.Empty;

        [Display(Name = "Supporting Document / Bill / Photo (Optional)")]
        public IFormFile? ProofFile { get; set; }
    }

    public class ClaimReviewViewModel
    {
        [Required]
        public int ClaimId { get; set; }

        [Required]
        public ClaimStatus Status { get; set; }

        [StringLength(500)]
        [Display(Name = "Admin Remarks / Instructions for Claimer")]
        public string? AdminRemarks { get; set; }
    }
}
