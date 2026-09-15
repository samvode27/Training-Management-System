using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Caching;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class CachedCourseService : ICachedCourseService
{
    private readonly HybridCache _cache;
    private readonly ILogger<CachedCourseService> _logger;
    private readonly TmsDbContext _context;

    public CachedCourseService(HybridCache cache, ILogger<CachedCourseService> logger, TmsDbContext context)
    {
        _cache = cache;
        _logger = logger;
        _context = context;
    }

    public async Task<List<CourseDto>> GetAllCoursesAsync(CancellationToken ct)
    {
        var key = CacheKeys.CoursesAll;
        var dbHit = false;

        var courses = await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                dbHit = true;
                _logger.LogInformation("Cache MISS for {Key}", key);
                
                // Record metric
                TmsMeters.CacheMisses.Add(1, new KeyValuePair<string, object?>("key.kind", "course"));

                return await _context.Courses
                    .AsNoTracking()
                    .Include(c => c.Instructor)
                    .Select(c => new CourseDto(
                        c.Id,
                        c.Code,
                        c.Title,
                        c.MaxCapacity,
                        c.Enrollments.Count,
                        c.Department,
                        c.Credits,
                        c.Summary,
                        c.Description,
                        c.Prerequisites,
                        c.LearningOutcomesJson,
                        c.SyllabusJson,
                        c.IndustrySkillsJson,
                        c.InstructorId,
                        c.Instructor != null ? $"{c.Instructor.FirstName} {c.Instructor.LastName}" : null))
                    .ToListAsync(token);
            },
            cancellationToken: ct);

        if (!dbHit)
        {
            _logger.LogInformation("Cache HIT for {Key}", key);
            // Record metric
            TmsMeters.CacheHits.Add(1, new KeyValuePair<string, object?>("key.kind", "course"));
        }

        return courses;
    }

    public async Task InvalidateCourseCacheAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Invalidating cache tag {Tag}", CacheKeys.CoursesTag);
        await _cache.RemoveByTagAsync(CacheKeys.CoursesTag, ct);
    }
}
