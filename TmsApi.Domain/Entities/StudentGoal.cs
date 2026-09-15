using System;

namespace TmsApi.Domain.Entities;

public class StudentGoal
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TargetTerm { get; set; } = string.Empty;
    public decimal TargetCredits { get; set; } = 3.0m;
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
