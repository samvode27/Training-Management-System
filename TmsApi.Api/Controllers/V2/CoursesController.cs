using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Utilities;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
public class CoursesController : ControllerBase
{
    private readonly ICachedCourseService _cachedCourseService;

    public CoursesController(ICachedCourseService cachedCourseService)
    {
        _cachedCourseService = cachedCourseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] string? fields,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var allCourses = await _cachedCourseService.GetAllCoursesAsync(ct);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var totalCount = allCourses.Count;
        var items = allCourses
            .OrderBy(c => c.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Apply data shaping with error handling
        IEnumerable<Dictionary<string, object?>> shaped;
        try
        {
            shaped = items.ShapeData(fields, CourseDtoFields.Allowed);
        }
        catch (Exception ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid field(s)",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var hasNext = page < totalPages;
        var hasPrevious = page > 1;

        var links = new List<LinkDto>
        {
            new(Url.Action(nameof(GetCourses), new { page, pageSize, fields })!, "self", "GET")
        };
        if (hasNext)
            links.Add(new(Url.Action(nameof(GetCourses), new { page = page + 1, pageSize, fields })!, "next", "GET"));
        if (hasPrevious)
            links.Add(new(Url.Action(nameof(GetCourses), new { page = page - 1, pageSize, fields })!, "prev", "GET"));

        return Ok(new
        {
            Data = shaped,
            Meta = new { totalCount, page, pageSize, totalPages, hasNext, hasPrevious },
            Links = links
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct = default)
    {
        var allCourses = await _cachedCourseService.GetAllCoursesAsync(ct);
        var course = allCourses.FirstOrDefault(c => c.Id == id);
        if (course is null)
            return NotFound();

        return Ok(course);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromServices] TmsApi.Infrastructure.Persistence.TmsDbContext db, CancellationToken ct)
    {
        var course = await db.Courses.Include(c => c.Enrollments).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Course not found",
                detail: $"Course with ID {id} was not found.",
                type: "https://tms.local/errors/not-found");
        }

        if (course.Enrollments.Any())
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Course deletion failed",
                detail: "Cannot delete course: active student enrollments exist.",
                type: "https://tms.local/errors/active-enrollments-exist");
        }

        db.Courses.Remove(course);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

}
