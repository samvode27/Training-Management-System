// Entity Framework

var students = await context.Students.ToListAsync(); // EF Core generates the SQL automatically

context.Students.Add(student); // add student
INSERT INTO Students // EF translates

// DbContext (database manager)

public class TmsDbContext : DbContext
{
    public DbSet<Student> Students { get; set; }

    public DbSet<Course> Courses { get; set; }
}

// DbSet (Represent Table)

public DbSet<Student> Students { get; set; }


// SaveChanges()

var student = new Student
{
    FullName = "Bewketu"
};

context.Students.Add(student);
await context.SaveChangesAsync(); // Database Updated


// CRUD Operations

// Create (Add)
var student = new Student
{
    FullName = "Abebe"
};

context.Students.Add(student);

await context.SaveChangesAsync();  // INSERT INTO Students(sql)
 
// Read (Get)

var students = await context.Students.ToListAsync();  // SELECT * FROM Students(sql)

// Update

var student = await context.Students.FindAsync(1); // find

student.FullName = "Kebede"; // modify

await context.SaveChangesAsync();  // save


// Delete
context.Students.Remove(student);

await context.SaveChangesAsync();  // DELETE FROM Students


// Relationships
