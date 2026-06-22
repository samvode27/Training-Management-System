
// An Entity represents a table.
public class Student
{
    public int Id { get; set; }

    public string FullName { get; set; }

    public string Email { get; set; }
}


// DbContext (DbContext = Database Connection + Tables)
public class TmsDbContext : DbContext
{
    public DbSet<Student> Students { get; set; }

    public DbSet<Course> Courses { get; set; }
}


// Relationships

// Student Entity
public class Student
{
    public int Id { get; set; }

    public string FullName { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; }
}

// Course Entity
public class Course
{
    public int Id { get; set; }

    public string Title { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; }
}

// Enrollment Entity
public class Enrollment
{
    public int StudentId { get; set; }

    public Student Student { get; set; }

    public int CourseId { get; set; }

    public Course Course { get; set; }
}