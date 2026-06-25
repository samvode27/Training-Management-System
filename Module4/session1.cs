// important ASP.NET Core concepts

// Request Pipeline
// Middleware Ordering
// Custom Middleware & Logging




// var builder = WebApplication.CreateBuilder(args);
// var app = builder.Build();

// app.UseRouting();

// app.MapGet("/api/assessments/results", () =>
//     Results.Ok(new
//     {
//         courseCode = "CS-101",
//         studentId = "S-001",
//         letterGrade = "A"
//     }));

// app.UseAuthentication();
// app.UseAuthorization();

// app.Run();

// the above problem The endpoint is mapped BEFORE authentication and authorization.




// Topic 2: Authentication vs Authorization

// Topic 3: Services vs Middleware

// Service Without Middleware  

// GET /api/grades     // student wants to see grades.
// builder.Services.AddAuthentication();   // add Service but no app.UseAuthentication();


// // Middleware Without Service
// app.UseAuthentication();      // but forget builder.Services.AddAuthentication();

// //example (installs Authentication system, Authorization system)
// builder.Services
// .AddAuthentication("Training")
// .AddScheme<AuthenticationSchemeOptions,
// TrainingAuthHandler>("Training", null);

// builder.Services.AddAuthorization();

// // use
// app.UseAuthentication();
// app.UseAuthorization();




// Topic 4: Correct Pipeline Order

// correct flow
// app.UseRouting();
// app.UseAuthentication();
// app.UseAuthorization();
// app.MapGet(...);


// Topic 5: Protecting an Endpoint

// Unprotected Endpoint (Anyone can access it)
// app.MapGet("/api/assessments/results", () =>
// {
//     return Results.Ok(new
//     {
//         student = "Ali",
//         grade = "A"
//     });
// });

// // Protected Endpoint (Only authenticated users can access it
// app.MapGet("/api/assessments/results", () =>
// {
//     return Results.Ok(new
//     {
//         student = "Ali",
//         grade = "A"
//     });
// })
// .RequireAuthorization();



// Topic 6: Training Authentication Handler

// Request
// GET /api/assessments/results    

// // handler checks:
// if (!Request.Headers.ContainsKey("X-Training-User"))
// {
//     return AuthenticateResult.Fail();
// }



// Topic 7: Middleware Flow
// public async Task InvokeAsync(HttpContext context)
// {
//     // Before

//     await _next(context);

//     // After
// }

// // example
// Console.WriteLine("Before");
// await _next(context);
// Console.WriteLine("After");




// Topic 8: Custom Request Logging Middleware

// Without logging (don't know)

// Which request?
// Which user?
// Which endpoint?



// Topic 9: Correlation ID

// Every request gets a unique ID

// Request Log
// [AB12CD34]
// GET /api/assessments/results

// Response Log
// [AB12CD34]
// 401 Unauthorized



// Topic 10: Adding Response Headers

// Middleware adds
// context.Response.Headers["X-Correlation-Id"] = correlationId;


// Topic 11: Stopwatch Timing (Measure request duration)
// var stopwatch = Stopwatch.StartNew();

// // After request
// stopwatch.Stop();

// // Read elapsed time
// stopwatch.ElapsedMilliseconds

// e.g Request completed in 23ms




// Topic 12: Logging Entry and Exit

// // before request
// _logger.LogInformation(
//     "Request {Method} {Path}",
//     context.Request.Method,
//     context.Request.Path);

// // after request
// _logger.LogInformation(
//     "Completed {StatusCode}",
//     context.Response.StatusCode);



// Topic 13: UseExceptionHandler
app.UseExceptionHandler();   // Catch unhandled exceptions.
