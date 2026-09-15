using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;

public interface ICourseRepository
{
    Task<Course?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Course?> GetByIdAsync(int id, CancellationToken ct = default);
}