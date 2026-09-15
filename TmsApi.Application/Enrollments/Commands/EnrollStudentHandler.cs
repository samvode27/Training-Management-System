using MediatR;
using TmsApi.Application.Common;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Enrollments.Commands;

public class EnrollStudentHandler : IRequestHandler<EnrollStudentCommand, Result<EnrollmentCreated, EnrollmentError>>
{
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly ICourseRepository _courseRepo;

    public EnrollStudentHandler(IEnrollmentRepository enrollmentRepo, ICourseRepository courseRepo)
    {
        _enrollmentRepo = enrollmentRepo;
        _courseRepo = courseRepo;
    }

    public async Task<Result<EnrollmentCreated, EnrollmentError>> Handle(
        EnrollStudentCommand command,
        CancellationToken ct)
    {
        // Check if course exists
        var course = await _courseRepo.GetByCodeAsync(command.CourseCode, ct);
        if (course is null)
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.CourseNotFound(command.CourseCode));

        // Check if course is full
        if (course.Enrollments.Count >= course.MaxCapacity)
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.CourseFull(course.Title, course.MaxCapacity));

        // Check if already enrolled
        if (await _enrollmentRepo.ExistsAsync(command.StudentId, command.CourseCode, ct))
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.AlreadyEnrolled(command.StudentId, command.CourseCode));

        var enrollment = new Enrollment
        {
            StudentId = command.StudentId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };

        await _enrollmentRepo.AddAsync(enrollment, ct);

        return Result<EnrollmentCreated, EnrollmentError>.Success(
            new EnrollmentCreated(enrollment.Id, enrollment.StudentId, course.Code));
    }
}