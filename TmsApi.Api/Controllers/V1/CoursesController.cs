using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("1.0")]
public class CoursesController : ControllerBase
{
    private readonly TmsDbContext _context;

    public CoursesController(TmsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? instructorId = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var baseQuery = _context.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Enrollments)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(instructorId))
        {
            baseQuery = baseQuery.Where(c => c.InstructorId == instructorId);
        }

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderBy(c => c.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.InstructorId,
                InstructorName = c.Instructor != null 
                    ? (c.Instructor.FirstName + " " + c.Instructor.LastName).Trim() 
                    : (!string.IsNullOrWhiteSpace(c.InstructorId) ? c.InstructorId : "Dr. Alex Taylor"),
                EnrollmentCount = c.Enrollments.Count
            })
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize,
            totalPages,
            hasNext = page < totalPages,
            hasPrevious = page > 1
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseV1Request request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Code and Title are required." });
        }

        var exists = await _context.Courses.AnyAsync(c => c.Code.ToLower() == request.Code.ToLower(), ct);
        if (exists)
        {
            return Conflict(new { message = $"Course code {request.Code} already exists." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? request.InstructorId ?? "admin";

        var course = new Course
        {
            Code = request.Code.ToUpper().Trim(),
            Title = request.Title.Trim(),
            MaxCapacity = request.MaxCapacity > 0 ? request.MaxCapacity : 30,
            InstructorId = userId
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetCourses), new { id = course.Id }, new
        {
            course.Id,
            course.Code,
            course.Title,
            course.MaxCapacity,
            course.InstructorId,
            InstructorName = userId,
            EnrollmentCount = 0
        });
    }
}

public record CreateCourseV1Request(string Code, string Title, int MaxCapacity, string? InstructorId);
