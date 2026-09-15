using System.Collections.Generic;

namespace TmsApi.Application.DTOs;

public record CourseDetailDto(
    int Id,
    string Code,
    string Title,
    int MaxCapacity,
    int EnrollmentCount,
    List<LinkDto> Links
);