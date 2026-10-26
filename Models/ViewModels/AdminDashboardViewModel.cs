using BackToYou.Models.Entities;

namespace BackToYou.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalItems { get; set; }
        public int TotalLost { get; set; }
        public int TotalFound { get; set; }
        public int TotalPendingClaims { get; set; }
        public int TotalApprovedClaims { get; set; }
        public int TotalRegisteredUsers { get; set; }

        public IEnumerable<Item> RecentItems { get; set; } = new List<Item>();
        public IEnumerable<Claim> PendingClaims { get; set; } = new List<Claim>();
    }

    public class UserManagementViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StudentRollNumber { get; set; }
        public string? Department { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Roles { get; set; } = string.Empty;
        public int ReportedCount { get; set; }
        public int ClaimsCount { get; set; }
    }
}
