using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Queries;

public class GetStudentScheduleHandler : IRequestHandler<GetStudentScheduleQuery, ScheduleDto>
{
    private readonly IEnrollmentRepository _repo;

    public GetStudentScheduleHandler(IEnrollmentRepository repo)
    {
        _repo = repo;
    }

    public async Task<ScheduleDto> Handle(GetStudentScheduleQuery query, CancellationToken ct)
    {
        var enrollments = await _repo.GetByStudentIdAsync(query.StudentId, ct);

        var courses = enrollments
            .Select(e => new ScheduleItemDto(
                e.Course.Code,
                e.Course.Title,
                "Schedule placeholder"
            ))
            .ToList();

        return new ScheduleDto(query.StudentId, courses);
    }
}