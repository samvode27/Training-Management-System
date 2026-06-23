// Performance


// N+1 Problem


// Student A
// Student B
// Student C

// Each has enrollments


// Bad Version (Many database calls)
var students = await context.Students.ToListAsync();

foreach(var student in students)
{
    Console.WriteLine(
        context.Enrollments
        .Count(e => e.StudentId == student.Id)
    );
}

// Better Version (Single database call)
var students = await context.Students
    .Include(s => s.Enrollments)
    .ToListAsync();

foreach(var student in students)
{
    Console.WriteLine(student.Enrollments.Count);
}


// AsNoTracking()

// Hard Delete
context.Students.Remove(student);
await context.SaveChangesAsync();

// Soft Delete
public bool IsDeleted { get; set; }   // add property

student.IsDeleted = true;
await context.SaveChangesAsync();

// Automatically Hide Deleted Records
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Student>()
        .HasQueryFilter(s => !s.IsDeleted);
}

var students = await context.Students.ToListAsync();  // Only active students


// ExecuteUpdateAsync()

// Traditional Way
var enrollments = await context.Enrollments.ToListAsync();
foreach(var e in enrollments)
{
    e.IsArchived = true;
}
await context.SaveChangesAsync();

// Better
await context.Enrollments
    .Where(e => e.EnrolledAt < cutoffDate)
    .ExecuteUpdateAsync(
        s => s.SetProperty(
            e => e.IsArchived,
            true));

// example
public class Enrrollment
{
    public int Id { get; set; }

    public bool IsArchived { get; set; }
}

await context.Enrrollment
    .ExecuteUpdateAsync(
        s => s.SetProperty(
            e => e.IsArchived,
            true));                        // Every enrollment becomes archived.