// Entity
// public class Student
// {
//     public string Id { get; set; }
//     public string Name { get; set; }
// }



// DbContext (managing entity objects, Manages database connections)

// public class TrainingDbContext : DbContext
// {
//     public DbSet<Student> Students { get; set; }
//}




// EF Core (Entity Framework Core)

// // Add student
// var student = new Student
// {
//     Id = "S001",
//     Name = "Ahmed"
// };

// context.Students.Add(student);

// // sends it to the database.
// context.SaveChanges();



// // DbSet

// public DbSet<Student> Students { get; set; }      // represents students table in the database



// Repository Pattern (data-access logic behind a dedicated interface)

// public interface IStudentRepository
// {
//     Task<Student?> GetByIdAsync(string id);
//     Task AddAsync(Student student);
// }

// public class StudentRepository
// {
//     private readonly TrainingDbContext _context;

//     public StudentRepository(
//         TrainingDbContext context)
//     {
//         _context = context;
//     }
// }




// // LINQ (sorting, searching,...)
// var students =
//     context.Students
//            .Where(
//                s => s.Name.StartsWith("A"))
//            .ToList();




// Migrations
// public class Student
// {
//     public string Id { get; set; }
//     public string Name { get; set; }
// }

// // after
// public class Student
// {
//     public string Id { get; set; }
//     public string Name { get; set; }
//     public string Email { get; set; }
// }

// // Generate migration:
// Add-Migration AddStudentEmail

// // Apply migration:
// Update-Database


