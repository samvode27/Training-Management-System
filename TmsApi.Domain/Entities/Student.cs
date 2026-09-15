//using System.Collections.Generic;
namespace TmsApi.Domain.Entities;

public class Student
{
    public int Id { get; set; }                     // surrogate key
    public required string RegistrationNumber { get; set; } // natural key
    public required string Name { get; set; }
    public decimal GPA { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation to enrollments
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    public byte[]? Version { get; set; }  // will be mapped to xmin
    public bool IsDeleted { get; set; } = false;
}