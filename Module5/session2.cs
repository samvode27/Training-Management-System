// GroupBy  

// How many students are enrolled in each course?

// var report =
//     await context.Enrollments
//         .GroupBy(e => e.CourseId)
//         .Select(g => new
//         {
//             CourseId = g.Key,
//             Count = g.Count()
//         })
//         .ToListAsync();




// Pagination
// int pageSize = 25;
// int pageNumber = 2;    // Show second page

// var students =
//     await context.Students
//         .OrderBy(s => s.Name)
//         .Skip((pageNumber - 1) * pageSize)
//         .Take(pageSize)
//         .ToListAsync();



// Relationship

// One-to-One Relationship (Student ↔ StudentCard)

// public class Student
// {
//     public int Id { get; set; }

//     public string FullName { get; set; }

//     public StudentCard StudentCard { get; set; }
// }

// public class StudentCard
// {
//     public int Id { get; set; }

//     public string CardNumber { get; set; }

//     public int StudentId { get; set; }

//     public Student Student { get; set; }
// }




// One-to-Many Relationship

// public class Trainer
// {
//     public int Id { get; set; }

//     public string Name { get; set; }

//     public ICollection<Course> Courses { get; set; }
// }

// public class Course
// {
//     public int Id { get; set; }

//     public string Title { get; set; }

//     public int TrainerId { get; set; }

//     public Trainer Trainer { get; set; }
// }



// Many-to-Many Relationship

// public class Student
// {
//     public int Id { get; set; }

//     public string FullName { get; set; }

//     public ICollection<Enrollment> Enrollments { get; set; }
// }

// public class Course
// {
//     public int Id { get; set; }

//     public string Title { get; set; }

//     public ICollection<Enrollment> Enrollments { get; set; }
// }

// public class Enrollment
// {
//     public int StudentId { get; set; }

//     public Student Student { get; set; }

//     public int CourseId { get; set; }

//     public Course Course { get; set; }
// }



// Fluent API (Configure relationships)

// company rule:
// Student Name:
// Required
// Maximum 100 characters
// Unique

// modelBuilder.Entity<Student>()
//     .Property(s => s.FullName)
//     .IsRequired()
//     .HasMaxLength(100);


// // One-To-Many (Trainer → Courses)

// modelBuilder.Entity<Course>()
//     .HasOne(c => c.Trainer)
//     .WithMany(t => t.Courses)
//     .HasForeignKey(c => c.TrainerId);

// // Table Name (change)
// modelBuilder.Entity<Student>().ToTable("Students");
// modelBuilder.Entity<StudentCard>().ToTable("StudentCards");



// Hard Delete
// context.Students.Remove(student);
// await context.SaveChangesAsync();

// // Soft Delete
// public bool IsDeleted { get; set; }   // add property

// student.IsDeleted = true;
// await context.SaveChangesAsync();



// Migrations

// first
// public class Student
// {
//     public int Id { get; set; }

//     public string FullName { get; set; }
// }

// // modify(add email)
// public class Student
// {
//     public int Id { get; set; }

//     public string FullName { get; set; }

//     public string Email { get; set; }
// }

// // now databse Need migration

// // Create Migration 
// dotnet ef migrations add AddEmailToStudent

// // Creates Migrations folder
// // Generate file
// migrationBuilder.AddColumn<string>(
//     name: "Email",
//     table: "Students");

// // Apply Migration
// dotnet ef database update  (Database updated)