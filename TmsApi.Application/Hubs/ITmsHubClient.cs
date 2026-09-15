namespace TmsApi.Application.Hubs;

public interface ITmsHubClient
{
    Task ReceiveTranscriptReady(string reportId, string downloadUrl);
    Task ReceiveCourseUpdate(string courseCode, string message);
    Task ReceiveGradePosted(string courseCode, int studentId, decimal grade);
    Task ReceiveEnrollmentStatusUpdated(string enrollmentId, string status);
}

public static class GroupNames
{
    public static string Student(string studentId) => $"student-{studentId}";
    public static string Course(string courseCode) => $"course-{courseCode}";
}