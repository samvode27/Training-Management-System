using Microsoft.AspNetCore.Identity;

namespace TmsApi.Domain.Entities;

public class TmsUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsApproved { get; set; } = false;
    public string ApprovalStatus { get; set; } = "Pending";
}
