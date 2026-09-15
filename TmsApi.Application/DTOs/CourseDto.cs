using System.Text.Json.Serialization;

namespace TmsApi.Application.DTOs;

public record CourseDto(
    int Id,
    string Code,
    string Title,
    int MaxCapacity,
    int EnrollmentCount,
    string? Department = null,
    decimal Credits = 3.0m,
    string? Summary = null,
    string? Description = null,
    string? Prerequisites = null,
    string? LearningOutcomesJson = null,
    string? SyllabusJson = null,
    string? IndustrySkillsJson = null,
    string? InstructorId = null,
    string? InstructorName = null
)
{
    // Example of a sensitive field that should never be exposed
    [JsonIgnore]
    public string? InternalNotes { get; init; }
}

public static class CourseDtoFields
{
    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(CourseDto.Id),
        nameof(CourseDto.Code),
        nameof(CourseDto.Title),
        nameof(CourseDto.MaxCapacity),
        nameof(CourseDto.EnrollmentCount),
        nameof(CourseDto.Department),
        nameof(CourseDto.Credits),
        nameof(CourseDto.Summary),
        nameof(CourseDto.Description),
        nameof(CourseDto.Prerequisites),
        nameof(CourseDto.LearningOutcomesJson),
        nameof(CourseDto.SyllabusJson),
        nameof(CourseDto.IndustrySkillsJson),
        nameof(CourseDto.InstructorId),
        nameof(CourseDto.InstructorName)
    };
}
