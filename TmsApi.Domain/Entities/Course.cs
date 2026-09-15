using System.Collections.Generic;

namespace TmsApi.Domain.Entities;

public class Course
{
    public int Id { get; set; }
    public required string Code { get; set; }        // natural key
    public required string Title { get; set; }
    public int MaxCapacity { get; set; }

    public string? Department { get; set; }
    public decimal Credits { get; set; } = 3.0m;
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? Prerequisites { get; set; }
    public string? LearningOutcomesJson { get; set; }
    public string? SyllabusJson { get; set; }
    public string? IndustrySkillsJson { get; set; }

    public string? InstructorId { get; set; }        // Foreign key to TmsUser
    public TmsUser? Instructor { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
