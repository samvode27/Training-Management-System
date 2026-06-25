// Topic 1: IQueryable vs List

// // IQueryable
// var query = context.Students
//                 .Where(s => s.GPA >= 3.0);

// IQueryable<Student>      // Instructions(not data)

// // List
// var students = query.ToList();

// List<Student>    // Actual student objects




// Materialization
// bad
// context.Students
//        .ToList()
//        .Where(s => s.GPA >= 3.5);

// // good
// context.Students
//        .Where(s => s.GPA >= 3.5)
//        .ToList();




// Deferred Execution

// // Building SQL Not executing SQL
// Where()
// OrderBy()
// Select()
// GroupBy()

// // Execute SQL NOW
// ToList()
// FirstOrDefault()
// Count()
// Any()




// Server-Side vs Client-Side Processing

// Good Way (Server-Side)
// context.Students
//        .Where(s => s.GPA >= 3.5)
//        .ToList();

// Bad Way (Client-Side)
// context.Students
//        .AsEnumerable()
//        .Where(s => IsHonorRoll(s.GPA))
//        .ToList();