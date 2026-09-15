using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly TmsDbContext _context;

    public EnrollmentRepository(TmsDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct = default)
    {
        return _context.Enrollments
            .AnyAsync(e => e.StudentId == studentId && e.Course.Code == courseCode, ct);
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken ct = default)
    {
        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync(ct);
    }

    public Task<List<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken ct = default)
    {
        return _context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
            .ToListAsync(ct);
    }
}