using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/students")]
[Route("api/students")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public class StudentsController : ControllerBase
{
    private readonly TmsDbContext _db;

    public StudentsController(TmsDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var students = await _db.Students
            .OrderBy(s => s.Id)
            .Select(s => new
            {
                s.Id,
                s.RegistrationNumber,
                s.Name,
                s.GPA,
                s.IsActive
            })
            .ToListAsync(ct);

        return Ok(students);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var student = await _db.Students
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (student == null)
            return NotFound(new { message = $"Student with ID {id} was not found." });

        return Ok(new
        {
            student.Id,
            student.RegistrationNumber,
            student.Name,
            student.GPA,
            student.IsActive,
            Enrollments = student.Enrollments.Select(e => new
            {
                e.Id,
                e.CourseId,
                CourseName = e.Course != null ? e.Course.Title : null,
                e.Grade,
                e.EnrolledAt
            })
        });
    }
}
