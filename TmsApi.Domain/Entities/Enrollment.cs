namespace TmsApi.Domain.Entities;

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public decimal? Grade { get; set; }            // nullable
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string? Notes { get; set; }
    public string? BackupCourses { get; set; }

    // Navigation properties
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public bool IsArchived { get; set; } = false;
}
