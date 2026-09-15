using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[Route("api/courses")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public class CourseController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICachedCourseService _cachedCourseService;

    public CourseController(TmsDbContext context, IAuthorizationService authorizationService, ICachedCourseService cachedCourseService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _cachedCourseService = cachedCourseService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseFullDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest(new { message = "Course code and title are required." });
        }

        var exists = await _context.Courses.AnyAsync(c => c.Code.ToUpper() == dto.Code.ToUpper());
        if (exists)
        {
            return Conflict(new { message = $"A course with code {dto.Code} already exists." });
        }

        var course = new Course
        {
            Code = dto.Code.Trim().ToUpper(),
            Title = dto.Title.Trim(),
            MaxCapacity = dto.MaxCapacity > 0 ? dto.MaxCapacity : 30,
            Department = dto.Department,
            Credits = dto.Credits > 0 ? dto.Credits : 3.0m,
            Summary = dto.Summary,
            Description = dto.Description,
            Prerequisites = dto.Prerequisites,
            LearningOutcomesJson = dto.LearningOutcomesJson,
            SyllabusJson = dto.SyllabusJson,
            IndustrySkillsJson = dto.IndustrySkillsJson,
            InstructorId = dto.InstructorId
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
        await _cachedCourseService.InvalidateCourseCacheAsync();

        return Created($"/api/courses/{course.Id}", course);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseFullDto dto)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound(new { message = $"Course with ID {id} was not found." });
        }

        // Resource-based authorization check
        var authResult = await _authorizationService.AuthorizeAsync(
            User, course, "CanEditCourse");

        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        // Update the core properties
        if (!string.IsNullOrWhiteSpace(dto.Title)) course.Title = dto.Title;
        if (!string.IsNullOrWhiteSpace(dto.Code)) course.Code = dto.Code;
        if (dto.MaxCapacity > 0) course.MaxCapacity = dto.MaxCapacity;

        // Update curriculum properties
        if (dto.Department != null) course.Department = dto.Department;
        if (dto.Credits.HasValue && dto.Credits.Value > 0) course.Credits = dto.Credits.Value;
        if (dto.Summary != null) course.Summary = dto.Summary;
        if (dto.Description != null) course.Description = dto.Description;
        if (dto.Prerequisites != null) course.Prerequisites = dto.Prerequisites;
        if (dto.LearningOutcomesJson != null) course.LearningOutcomesJson = dto.LearningOutcomesJson;
        if (dto.SyllabusJson != null) course.SyllabusJson = dto.SyllabusJson;
        if (dto.IndustrySkillsJson != null) course.IndustrySkillsJson = dto.IndustrySkillsJson;
        if (dto.InstructorId != null) course.InstructorId = dto.InstructorId;

        await _context.SaveChangesAsync();
        await _cachedCourseService.InvalidateCourseCacheAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound(new { message = $"Course with ID {id} was not found." });
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
        await _cachedCourseService.InvalidateCourseCacheAsync();

        return NoContent();
    }
}

public record CreateCourseFullDto(
    string Code,
    string Title,
    int MaxCapacity,
    string? Department = null,
    decimal Credits = 3.0m,
    string? Summary = null,
    string? Description = null,
    string? Prerequisites = null,
    string? LearningOutcomesJson = null,
    string? SyllabusJson = null,
    string? IndustrySkillsJson = null,
    string? InstructorId = null
);

public record UpdateCourseFullDto(
    string Title,
    string? Code,
    int MaxCapacity,
    string? Department = null,
    decimal? Credits = null,
    string? Summary = null,
    string? Description = null,
    string? Prerequisites = null,
    string? LearningOutcomesJson = null,
    string? SyllabusJson = null,
    string? IndustrySkillsJson = null,
    string? InstructorId = null
);