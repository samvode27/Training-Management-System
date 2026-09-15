using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence;

public static class DataSeeder
{
    public static readonly (string Code, string Title, int MaxCapacity)[] CourseList =
    [
        ("CSE-101", "Web Development Fundamentals", 30),
        ("CSE-102", "TypeScript Essentials", 30),
        ("CSE-103", "Git and Collaborative Workflows", 25),
        ("CSE-201", "ASP.NET Core Fundamentals", 28),
        ("CSE-202", "Entity Framework Core and PostgreSQL", 28),
        ("CSE-203", "Building RESTful Web APIs", 28),
        ("CSE-301", "Advanced Web API Patterns", 24),
        ("CSE-302", "Angular Fundamentals", 26),
        ("CSE-303", "Angular Advanced", 24),
        ("CSE-304", "Full-Stack Integration", 22),
        ("CSE-305", "Testing and Quality Assurance", 22),
        ("CSE-306", "Security and Authentication", 20),
        ("UX-101", "User Experience Design Fundamentals", 25),
        ("UX-201", "Interaction Design", 20),
        ("UX-301", "Usability Testing", 18),
        ("DATA-101", "Data Science Foundations", 30),
        ("DATA-201", "Database Design", 25),
        ("DATA-301", "Data Visualization", 20),
        ("MATH-101", "Discrete Mathematics", 30),
        ("MATH-201", "Statistics for Developers", 25),
        ("BUS-101", "Business Fundamentals", 30),
        ("BUS-201", "Project Management", 25),
        ("PHIL-101", "Critical Thinking", 30),
        ("PHIL-201", "Ethics in Technology", 25),
        ("ENG-101", "Technical Writing", 20)
    ];

    public static async Task SeedAsync(TmsDbContext context, CancellationToken ct = default)
    {
        await context.Database.MigrateAsync(ct);

        var existingCodes = await context.Courses.Select(c => c.Code).ToListAsync(ct);

        foreach (var (code, title, maxCapacity) in CourseList)
        {
            if (!existingCodes.Contains(code))
            {
                context.Courses.Add(new Course
                {
                    Code = code,
                    Title = title,
                    MaxCapacity = maxCapacity
                });
            }
        }

        await context.SaveChangesAsync(ct);
    }
}
