namespace BackToYou.Models.Entities
{
    public enum ItemType
    {
        Lost = 1,
        Found = 2
    }

    public enum ItemStatus
    {
        Reported = 1,
        UnderClaim = 2,
        Claimed = 3,
        Closed = 4
    }

    public enum ClaimStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }
}
