// Console.WriteLine("=== Module 1 session 1 ===");

// string studentName = "Kidane";

// int age = 22;

// decimal courseFee = 1999.99m;

// bool isEnrolled = true;

// Console.WriteLine($"Student: {studentName}");
// Console.WriteLine($"Age: {age}");
// Console.WriteLine($"Fee: {courseFee}");
// Console.WriteLine($"Enrolled: {isEnrolled}");




// Console.WriteLine("=== Module 1 session 2 ===");

// List<Student> students =
// [
//     new() { Id="S1", Name="Abeba", Age=20, Email="abeba@example.com", GPA=3.8m },
//     new() { Id="S2", Name="Kidane", Age=22, Email="kidane@example.com", GPA=2.9m },
//     new() { Id="S3", Name="Sara", Age=19, Email="sara@example.com", GPA=1.8m }
// ];

// var honorsStudents =
//     students.Where(s => s.GPA >= 3.0m);

// foreach (var student in honorsStudents)
// {
//     Console.WriteLine($"Name: {student.Name}, GPA: {student.GPA}");
// }

// var ranked =
//     students.OrderByDescending(s => s.GPA);

// foreach (var student in ranked)
// {
//     Console.WriteLine($"Name: {student.Name}, GPA: {student.GPA}");
// }

// decimal avg = students.Average(s => s.GPA);
// Console.WriteLine($"Average GPA: {avg}");

// string GetStatus(decimal gpa) =>
// gpa switch
// {
//     >= 3.5m => "Honors",
//     >= 2.5m => "Good Standing",
//     >= 2.0m => "Probation",
//     _ => "Warning"
// };

// foreach (var student in students)
// {
//     string status = GetStatus(student.GPA);
//     Console.WriteLine($"Name: {student.Name}, GPA: {student.GPA}, Status: {status}");
// }




// Console.WriteLine("=== Module 1 session 3 ===");

// async Task<Student> FetchStudentAsync(string id)
// {
//     await Task.Delay(500);

//     return new Student
//     {
//         Id = id,
//         Name = $"Student-{id}"
//     };
// }

// var student =
//     await FetchStudentAsync("S1");

// Console.WriteLine(student.Name);




