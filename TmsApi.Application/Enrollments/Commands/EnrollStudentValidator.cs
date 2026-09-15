    using FluentValidation;

namespace TmsApi.Application.Enrollments.Commands;

public class EnrollStudentValidator : AbstractValidator<EnrollStudentCommand>
{
    public EnrollStudentValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0)
            .WithMessage("StudentId must be a positive integer.");

        RuleFor(x => x.CourseCode)
            .NotEmpty()
            .WithMessage("CourseCode is required.")
            .Matches(@"^[A-Z]{3}-\d{3}$")
            .WithMessage("Course code must follow the format XXX-000 (e.g., CSE-101).");
    }
}