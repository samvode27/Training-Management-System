using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using TmsApi.Api.Hubs;
using TmsApi.Application.Hubs;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

public record CreateEnrollmentDto(
    string? StudentId,
    string? StudentName,
    int? CourseId,
    string? Term,
    string? Notes,
    List<string>? BackupCourses);

[ApiController]
[Route("api/v{version:apiVersion}/enrollments")]
[Route("api/enrollments")]
[ApiVersion("2.0")]
[ApiVersion("1.0")]
[Route("api/v1/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly TmsDbContext _db;
    private readonly IHubContext<TmsApi.Api.Hubs.TmsHub, ITmsHubClient> _hubContext;

    public EnrollmentsController(
        TmsDbContext db,
        IHubContext<TmsApi.Api.Hubs.TmsHub, ITmsHubClient> hubContext)
    {
        _db = db;
        _hubContext = hubContext;
    }

    public static string GetEnrollmentStatus(Enrollment e)
    {
        if (!string.IsNullOrWhiteSpace(e.Status))
        {
            return e.Status;
        }
        if (e.IsArchived)
        {
            return "Rejected";
        }
        if (e.Grade.HasValue)
        {
            return "Approved";
        }
        return "Pending";
    }

    public static bool IsEnrollmentApproved(Enrollment e)
    {
        return GetEnrollmentStatus(e) == "Approved";
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var rawEnrollments = await _db.Enrollments
            .Include(e => e.Course)
                .ThenInclude(c => c.Instructor)
            .Include(e => e.Student)
            .OrderByDescending(e => e.Id)
            .ToListAsync(ct);

        var list = rawEnrollments.Select(e =>
        {
            string status = GetEnrollmentStatus(e);

            return new
            {
                id = "ENR-" + e.Id,
                studentId = e.StudentId,
                studentName = e.Student != null ? e.Student.Name : "Student #" + e.StudentId,
                courseId = e.CourseId,
                courseName = e.Course != null ? e.Course.Code + " - " + e.Course.Title : "Course #" + e.CourseId,
                courseInstructorId = e.Course != null ? e.Course.InstructorId : null,
                courseInstructorName = e.Course != null && e.Course.Instructor != null ? (e.Course.Instructor.FirstName + " " + e.Course.Instructor.LastName).Trim() : (e.Course != null && !string.IsNullOrWhiteSpace(e.Course.InstructorId) ? e.Course.InstructorId : "Dr. Alex Taylor"),
                status = status,
                enrolledAt = e.EnrolledAt.ToString("O"),
                grade = e.Grade.HasValue ? (double)Math.Round(e.Grade.Value > 4.0m ? (decimal)e.Grade.Value : (e.Grade.Value / 4.0m) * 100m, 1) : (double?)null,
                letterGrade = e.Grade.HasValue ? (e.Grade.Value >= 3.6m ? "A" : e.Grade.Value >= 3.0m ? "B" : e.Grade.Value >= 2.0m ? "C" : "D") : null,
                notes = e.Notes,
                backupCourses = e.BackupCourses
            };
        });

        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] CreateEnrollmentDto dto, CancellationToken ct)
    {
        string studentName = !string.IsNullOrWhiteSpace(dto.StudentName)
            ? dto.StudentName.Trim()
            : (!string.IsNullOrWhiteSpace(dto.StudentId) ? dto.StudentId.Trim() : "Student User");

        var student = await _db.Students.FirstOrDefaultAsync(s => s.Name.ToLower() == studentName.ToLower(), ct);
        if (student == null)
        {
            var studentCount = await _db.Students.CountAsync(ct);
            student = new Student
            {
                Name = studentName,
                RegistrationNumber = $"TMS-2026-{(studentCount + 1):D4}",
                GPA = 3.5m,
                IsActive = true
            };
            _db.Students.Add(student);
            await _db.SaveChangesAsync(ct);
        }

        int courseId = dto.CourseId ?? 1;
        var course = await _db.Courses.FindAsync(new object[] { courseId }, ct);
        if (course == null)
        {
            course = await _db.Courses.FirstOrDefaultAsync(ct) ?? new Course
            {
                Code = "CS-101",
                Title = "Introduction to Computer Science",
                MaxCapacity = 30
            };
            if (course.Id == 0)
            {
                _db.Courses.Add(course);
                await _db.SaveChangesAsync(ct);
            }
        }

        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow,
            IsArchived = false,
            Status = "Pending",
            Notes = dto.Notes,
            BackupCourses = dto.BackupCourses != null && dto.BackupCourses.Count > 0 ? string.Join(", ", dto.BackupCourses) : null
        };

        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync(ct);

        var enrollmentId = "ENR-" + enrollment.Id;
        await _hubContext.Clients.All.ReceiveEnrollmentStatusUpdated(enrollmentId, "Pending");

        var response = new
        {
            id = enrollmentId,
            studentId = student.Id,
            studentName = student.Name,
            courseId = course.Id,
            courseName = $"{course.Code} - {course.Title}",
            status = "Pending",
            enrolledAt = enrollment.EnrolledAt.ToString("O"),
            notes = enrollment.Notes,
            backupCourses = enrollment.BackupCourses
        };

        return Ok(response);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(string id, CancellationToken ct)
    {
        var rawId = id.Replace("ENR-", "");
        if (int.TryParse(rawId, out var parsedId))
        {
            var enrollment = await _db.Enrollments.FindAsync(new object[] { parsedId }, ct);
            if (enrollment != null)
            {
                enrollment.Status = "Approved";
                enrollment.IsArchived = false;
                await _db.SaveChangesAsync(ct);
            }
        }

        await _hubContext.Clients.All.ReceiveEnrollmentStatusUpdated(id, "Approved");
        return NoContent();
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(string id, CancellationToken ct)
    {
        var rawId = id.Replace("ENR-", "");
        if (int.TryParse(rawId, out var parsedId))
        {
            var enrollment = await _db.Enrollments.FindAsync(new object[] { parsedId }, ct);
            if (enrollment != null)
            {
                enrollment.Status = "Rejected";
                enrollment.IsArchived = true;
                await _db.SaveChangesAsync(ct);
            }
        }

        await _hubContext.Clients.All.ReceiveEnrollmentStatusUpdated(id, "Rejected");
        return NoContent();
    }
}
