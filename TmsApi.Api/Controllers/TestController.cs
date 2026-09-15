using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers;
[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly TmsDbContext _context;

    public TestController(TmsDbContext context)
    {
        _context = context;
    }

    // Deferred execution experiment
    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        Console.WriteLine("\n>> STEP 1: Building the query object (no database contact)...");
        var query = _context.Students.Where(s => s.GPA >= 3.0m);

        Console.WriteLine("\n>> STEP 2: Appending a sorting clause...");
        var orderedQuery = query.OrderBy(s => s.Name);

        Console.WriteLine(">> STEP 3: Materializing query into a C# List...");
        var results = orderedQuery.ToList(); // Execution is triggered here

        Console.WriteLine(">> STEP 4: Materialization finished. List populated.\n");
        return Ok(results);
    }

    // Non-translatable helper
    private static bool IsHonorRoll(decimal gpa)
    {
        return gpa >= 3.5m;
    }

    // Translation failure experiment
    [HttpGet("translation-fail")]
    public IActionResult TestTranslationFail()
    {
        Console.WriteLine("\n>> STEP 1: Running non-translatable query...");
        try
        {
            var students = _context.Students
                .Where(s => IsHonorRoll(s.GPA))
                .ToList();
            return Ok(students);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("active-high-gpa-count")]
public async Task<IActionResult> ActiveHighGpaCount()
{
    var count = await _context.Students
        .Where(s => s.IsActive && s.GPA >= 3.0m)
        .CountAsync();
    return Ok(new { Count = count });
}

[HttpGet("course-enrollment-counts")]
public async Task<IActionResult> CourseEnrollmentCounts()
{
    var list = await _context.Courses
        .Select(c => new { c.Title, EnrollmentCount = c.Enrollments.Count })
        .OrderByDescending(x => x.EnrollmentCount)
        .ToListAsync();
    return Ok(list);
}

[HttpGet("average-gpa-per-course")]
public async Task<IActionResult> AverageGpaPerCourse()
{
    var list = await _context.Enrollments
        .GroupBy(e => e.Course.Title)
        .Select(g => new { Course = g.Key, AverageGPA = g.Average(e => e.Student.GPA) })
        .ToListAsync();
    return Ok(list);
}

[HttpGet("students-no-enrollments")]
public async Task<IActionResult> StudentsNoEnrollments()
{
    // Approach A: using Any() (translates to NOT EXISTS)
    var usingAny = await _context.Students
        .Where(s => !s.Enrollments.Any())
        .Select(s => s.Name)
        .ToListAsync();

    // Approach B: using LeftJoin (translates to LEFT JOIN ... WHERE ... IS NULL)
    var usingLeftJoin = await _context.Students
        .GroupJoin(_context.Enrollments,
            s => s.Id,
            e => e.StudentId,
            (s, enrollments) => new { Student = s, Enrollments = enrollments })
        .SelectMany(x => x.Enrollments.DefaultIfEmpty(),
            (x, e) => new { x.Student, Enrollment = e })
        .Where(x => x.Enrollment == null)
        .Select(x => x.Student.Name)
        .ToListAsync();

    return Ok(new { UsingAny = usingAny, UsingLeftJoin = usingLeftJoin });
}

[HttpGet("students-paged")]
public async Task<IActionResult> GetStudentsPaged(int page = 1, int pageSize = 20)
{
    // Stable sort by Name to ensure consistent paging
    var query = _context.Students
        .OrderBy(s => s.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize);

    var students = await query.ToListAsync();
    return Ok(students);
}

[HttpGet("top-courses")]
public async Task<IActionResult> GetTopCoursesByEnrollment()
{
    var topCourses = await _context.Courses
        .Select(c => new
        {
            c.Title,
            EnrollmentCount = c.Enrollments.Count
        })
        .OrderByDescending(x => x.EnrollmentCount)
        .Take(5)
        .ToListAsync();

    return Ok(topCourses);
}

[HttpGet("n-plus-one")]
public async Task<IActionResult> NPlusOneDemo()
{
    Console.WriteLine("\n=== N+1 DEMO START ===");
    var students = await _context.Students.AsNoTracking().ToListAsync();

    foreach (var s in students)
    {
        // This runs a separate Count query per student
        var count = await _context.Enrollments
            .AsNoTracking()
            .CountAsync(e => e.StudentId == s.Id);
        Console.WriteLine($"{s.Name}: {count} enrollments");
    }
    Console.WriteLine("=== N+1 DEMO END ===\n");
    return Ok("Check console logs for SQL statements");
}

[HttpGet("shaped")]
public async Task<IActionResult> ShapedDemo()
{
    Console.WriteLine("\n=== SHAPED QUERY START ===");
    var report = await _context.Students
        .AsNoTracking()
        .Select(s => new { s.Name, EnrollmentCount = s.Enrollments.Count })
        .ToListAsync();

    foreach (var r in report)
    {
        Console.WriteLine($"{r.Name}: {r.EnrollmentCount} enrollments");
    }
    Console.WriteLine("=== SHAPED QUERY END ===\n");
    return Ok(report);
}

[HttpPut("update-student/{id}")]
public async Task<IActionResult> UpdateStudent(int id, string newName)
{
    var student = await _context.Students.FindAsync(id);
    if (student == null) return NotFound();

    // Update the shadow property for audit
    _context.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;

    student.Name = newName;
    
    try
    {
        await _context.SaveChangesAsync();
        return Ok(new { student.Id, student.Name, Message = "Update succeeded" });
    }
    catch (DbUpdateConcurrencyException)
    {
        return Conflict(new { Message = "Concurrency conflict! Someone else edited this record." });
    }
}

[HttpPost("concurrency-test/{id}")]
public async Task<IActionResult> ConcurrencyTest(int id)
{
    // Load student in a SEPARATE context (simulating another user)
    var optionsBuilder = new DbContextOptionsBuilder<TmsDbContext>();
    optionsBuilder.UseNpgsql(_context.Database.GetConnectionString());
    
    using var otherContext = new TmsDbContext(optionsBuilder.Options);
    
    // Other user loads the same student
    var otherStudent = await otherContext.Students.FindAsync(id);
    if (otherStudent == null) return NotFound("Student not found");
    
    // Current user loads the student
    var currentStudent = await _context.Students.FindAsync(id);
    if (currentStudent == null) return NotFound("Student not found");
    
    // Other user changes name and saves FIRST
    otherStudent.Name = "Changed By Other User";
    otherContext.Entry(otherStudent).Property("LastUpdated").CurrentValue = DateTime.UtcNow;
    await otherContext.SaveChangesAsync();
    
    Console.WriteLine("Other user saved successfully.");
    
    // Now current user tries to save (should FAIL)
    currentStudent.Name = "Changed By Current User";
    _context.Entry(currentStudent).Property("LastUpdated").CurrentValue = DateTime.UtcNow;
    
    try
    {
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Saved (no conflict)" });
    }
    catch (DbUpdateConcurrencyException)
    {
        return Conflict(new { Message = "Concurrency conflict detected! Refresh and try again." });
    }
}

[HttpPost("archive-old-enrollments")]
public async Task<IActionResult> ArchiveOldEnrollments([FromQuery] int daysOld = 30)
{
    var cutoff = DateTime.UtcNow.AddDays(-daysOld);

    var rowsAffected = await _context.Enrollments
        .Where(e => e.EnrolledAt < cutoff && !e.IsArchived)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(e => e.IsArchived, true)
        );

    return Ok(new { rowsAffected });
}

[HttpGet("students")]
public async Task<IActionResult> GetStudents()
{
    var students = await _context.Students.ToListAsync();
    return Ok(students);
}

[HttpGet("students-with-deleted")]
public async Task<IActionResult> GetAllStudentsIncludingDeleted()
{
    var students = await _context.Students
        .IgnoreQueryFilters()
        .ToListAsync();
    return Ok(students);
}

[HttpDelete("soft-delete-student/{id}")]
public async Task<IActionResult> SoftDeleteStudent(int id)
{
    var student = await _context.Students.FindAsync(id);
    if (student == null) return NotFound();

    student.IsDeleted = true;
    await _context.SaveChangesAsync();
    
    Console.WriteLine($"Student {student.Name} (ID: {id}) soft-deleted.");
    return Ok(new { Message = $"Student {id} soft-deleted" });
}


}