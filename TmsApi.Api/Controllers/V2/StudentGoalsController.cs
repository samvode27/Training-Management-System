using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

public record CreateStudentGoalDto(
    string StudentId,
    string Title,
    string TargetTerm,
    decimal TargetCredits
);

public record UpdateStudentGoalDto(
    bool? IsCompleted,
    string? Title,
    string? TargetTerm,
    decimal? TargetCredits
);

[ApiController]
[Route("api/v{version:apiVersion}/student-goals")]
[Route("api/student-goals")]
[ApiVersion("2.0")]
[ApiVersion("1.0")]
public class StudentGoalsController : ControllerBase
{
    private readonly TmsDbContext _db;

    public StudentGoalsController(TmsDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetGoals([FromQuery] string? studentId, CancellationToken ct)
    {
        IQueryable<StudentGoal> query = _db.StudentGoals.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(studentId))
        {
            query = query.Where(g => g.StudentId.ToLower() == studentId.Trim().ToLower());
        }

        var goals = await query.OrderByDescending(g => g.CreatedAt).ToListAsync(ct);
        return Ok(goals);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGoal([FromBody] CreateStudentGoalDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest(new { message = "Goal title is required." });
        }

        var goal = new StudentGoal
        {
            StudentId = !string.IsNullOrWhiteSpace(dto.StudentId) ? dto.StudentId.Trim() : "Student",
            Title = dto.Title.Trim(),
            TargetTerm = !string.IsNullOrWhiteSpace(dto.TargetTerm) ? dto.TargetTerm.Trim() : "Fall 2026",
            TargetCredits = dto.TargetCredits > 0 ? dto.TargetCredits : 3.0m,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.StudentGoals.Add(goal);
        await _db.SaveChangesAsync(ct);

        return Ok(goal);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateGoal(int id, [FromBody] UpdateStudentGoalDto dto, CancellationToken ct)
    {
        var goal = await _db.StudentGoals.FindAsync(new object[] { id }, ct);
        if (goal == null)
        {
            return NotFound(new { message = $"Goal with ID {id} not found." });
        }

        if (dto.IsCompleted.HasValue) goal.IsCompleted = dto.IsCompleted.Value;
        if (!string.IsNullOrWhiteSpace(dto.Title)) goal.Title = dto.Title.Trim();
        if (!string.IsNullOrWhiteSpace(dto.TargetTerm)) goal.TargetTerm = dto.TargetTerm.Trim();
        if (dto.TargetCredits.HasValue && dto.TargetCredits.Value > 0) goal.TargetCredits = dto.TargetCredits.Value;

        await _db.SaveChangesAsync(ct);
        return Ok(goal);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteGoal(int id, CancellationToken ct)
    {
        var goal = await _db.StudentGoals.FindAsync(new object[] { id }, ct);
        if (goal == null)
        {
            return NotFound(new { message = $"Goal with ID {id} not found." });
        }

        _db.StudentGoals.Remove(goal);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
