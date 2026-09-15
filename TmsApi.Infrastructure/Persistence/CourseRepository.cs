using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence;

public class CourseRepository : ICourseRepository
{
    private readonly TmsDbContext _context;

    public CourseRepository(TmsDbContext context)
    {
        _context = context;
    }

    public Task<Course?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return _context.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Code == code, ct);
    }

    public Task<Course?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return _context.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}