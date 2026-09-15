using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Interfaces;

namespace TmsApi.Infrastructure.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly TmsDbContext _context;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct)
    {
        return await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt
            ))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct)
{
    return await _context.Enrollments
        .AsNoTracking()
        .Where(e => e.CourseId == courseId)
        .Select(e => new EnrollmentResponseDto(
            e.Id,
            e.CourseId,
            e.StudentId,
            e.EnrolledAt
        ))
        .ToListAsync(ct);
}

    public async Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct)
    {
        // Check if student exists
        var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId, ct);
        if (!studentExists)
        {
            throw new ArgumentException($"Student with ID {request.StudentId} does not exist.");
        }

        // Check if student is already enrolled in this course
        var alreadyEnrolled = await _context.Enrollments
            .AnyAsync(e => e.CourseId == courseId && e.StudentId == request.StudentId, ct);
        
        if (alreadyEnrolled)
        {
            throw new InvalidOperationException($"Student {request.StudentId} is already enrolled in course {courseId}.");
        }

        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Enrolled student {StudentId} in course {CourseId} (EnrollmentId: {EnrollmentId})", 
            request.StudentId, courseId, enrollment.Id);

        // Re-query to get the DTO
        var result = await GetByIdAsync(courseId, enrollment.Id, ct);
        if (result is null)
        {
            throw new InvalidOperationException("Failed to retrieve created enrollment.");
        }

        return result;
    }
}