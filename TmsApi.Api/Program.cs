using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using TmsApi.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TmsApi.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;
//using TmsApi.Data;
//using TmsApi.Entities;
using Scalar.AspNetCore;
//using TmsApi.Services;
//using TmsApi.Dtos;
//using TmsApi.Exceptions;
using TmsApi.Api.Exceptions;
using TmsApi.Api.Filters;
using TmsApi.Infrastructure.Persistence;        // for TmsDbContext
using TmsApi.Infrastructure.ExternalServices;
using TmsApi.Domain.Entities;                   // if you use entities directly in Program.cs (seed data)
using TmsApi.Application.Interfaces;            // for ICourseService, IEnrollmentService
using TmsApi.Application.DTOs;     
using TmsApi.Api.Options;             // if you use DTOs in minimal APIs
// using TmsApi.Api.Filters;                       // for AuditLogFilter (now in Api project)
//using TmsApi.Data;
using Asp.Versioning;
using TmsApi.Api.Middleware;

using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Behaviors;
using FluentValidation;
using MediatR;
using TmsApi.Api.ExceptionHandlers;
using Microsoft.Extensions.Caching.Hybrid;
using TmsApi.Infrastructure.Caching;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Api.RateLimiting;
using System.Threading.Channels;
using TmsApi.Infrastructure.Transcripts;
using TmsApi.Application.Transcripts;
using TmsApi.Application.Hubs;
using TmsApi.Infrastructure.Workers;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);

// Authentication/Authorization
// JWT Bearer authentication registered in Authentication Pipeline section
builder.Services.AddAuthorization();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanEditCourse", policy =>
        policy.Requirements.Add(new CourseInstructorRequirement()));

builder.Services.AddSingleton<IAuthorizationHandler, CourseInstructorHandler>();


// ===== Database Context =====
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
        .LogTo(Console.WriteLine, LogLevel.Information)   // prints SQL
        .EnableSensitiveDataLogging());                   // shows parameter values

// ===== Problem Details =====
builder.Services.AddProblemDetails();

/*builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});*/

/* ===== EXERCISE 5: Add Controllers Service =====
builder.Services.AddControllers();*/
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogFilter>();
});

// After builder.Services.AddControllers()
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
});
// Production-only - leave commented for lab
// builder.Services.AddStackExchangeRedisCache(options =>
// {
//     options.Configuration = builder.Configuration.GetConnectionString("Redis");
//     options.InstanceName = "tms:";
// });
// builder.Services.AddHybridCache();




builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthLimiter", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("LoginPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // Global limiter - applies to all requests
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var (partitionKey, tier) = ApiKeyResolver.Resolve(httpContext);

        return tier switch
        {
            ApiKeyTier.Paid => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"paid:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 200,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),
            ApiKeyTier.Free => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"free:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 50,
                    TokensPerPeriod = 25,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),
            _ => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"anon:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 10,
                    TokensPerPeriod = 5,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                })
        };
    });

    // Concurrency limiter for expensive transcript endpoint
    options.AddConcurrencyLimiter("transcripts", opt =>
    {
        opt.PermitLimit = 5;      // 5 in-flight transcripts maximum
        opt.QueueLimit = 20;      // Queue up to 20 more
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Token bucket for search endpoint
    options.AddTokenBucketLimiter("search", opt =>
    {
        opt.TokenLimit = 10;
        opt.TokensPerPeriod = 5;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        opt.QueueLimit = 2;
    });

    // Customize rejection response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";

        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ts)
            ? (int)ts.TotalSeconds
            : 10;

        var problem = new
        {
            type = "https://tms.local/errors/rate-limited",
            title = "Too Many Requests",
            status = 429,
            detail = "Rate limit exceeded. Please try again later.",
            retryAfter = retryAfter
        };

        await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
    };
});


builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(EnrollStudentHandler).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(EnrollStudentValidator).Assembly);

// LoggingBehavior FIRST - it must wrap ValidationBehavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();


builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(2, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("x-api-version"),
        new QueryStringApiVersionReader("api-version")
    );
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});


// ===== OpenAPI =====
builder.Services.AddOpenApi();

// ===== EXERCISE 2: Service Registrations =====
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();  // NEW EnrollmentService
builder.Services.AddScoped<ICourseService, CourseService>();

// In the services section:
builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();

// ===== EXERCISE 3: Options Pattern =====
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Host validation
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

builder.Services.AddSignalR();
// After builder.Services.AddSignalR() (we'll add this in Exercise 6)
builder.Services.AddSingleton<ITranscriptStatusStore, InMemoryTranscriptStatusStore>();

// Create the bounded channel for transcript requests
builder.Services.AddSingleton(Channel.CreateBounded<TranscriptRequest>(
    new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait
    }));


builder.Services.AddHostedService<TranscriptWorker>();

builder.Services.AddResiliencePipeline("certificate-api", pipeline =>
{
    pipeline
        // Outer: per-request hard timeout - protects against hangs
        .AddTimeout(TimeSpan.FromSeconds(5))
        // Middle: circuit breaker - protects against sustained outage
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(15),
            ShouldHandle = new PredicateBuilder()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>(),
            OnOpened = args =>
            {
                Console.WriteLine($"Circuit OPENED - stopping requests to certificate service");
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                Console.WriteLine($"Circuit CLOSED - certificate service recovered");
                return ValueTask.CompletedTask;
            }
        })
        // Inner: retry with jitter - only for transient failures
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>(),
            OnRetry = args =>
            {
                Console.WriteLine(
                    $"Retry #{args.AttemptNumber} after {args.RetryDelay.TotalMilliseconds:F0}ms ({args.Outcome.Exception?.GetType().Name})");
                return ValueTask.CompletedTask;
            }
        });
});


builder.Services.AddHttpClient<ICertificateService, CertificateService>((sp, client) =>
{
    var baseUrl = sp.GetRequiredService<IConfiguration>().GetValue<string>("TmsApi:PublicBaseUrl")
        ?? "http://localhost:5280";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("alive"),
        tags: new[] { "live" })
    .AddNpgSql(
        builder.Configuration.GetConnectionString("TmsDatabase")!,
        tags: new[] { "ready" });

builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.JsonWriterOptions = new() { Indented = false };
});     

const string ServiceName = "tms-api";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: ServiceName,
        serviceVersion: "1.0.0"))
    .WithTracing(t => t
        .AddSource(ServiceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddMeter(ServiceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());


// Load allowed origins from appsettings.Development.json
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:4200"];

// Register the CORS policy in the Dependency Injection container
builder.Services.AddCors(options =>
{
    options.AddPolicy("TmsClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()  // Vital for HttpOnly auth cookies
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});


// Add Antiforgery service with header name matching Angular's default convention
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
});

// Add Identity Core with enterprise password and lockout policies
// Register TokenService
builder.Services.AddScoped<TokenService>();

// Configure JWT Bearer Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["Jwt:Key"] ?? "A-Very-Long-Secret-Key-For-TMS-Auth-Stored-Safely-2026-Minimum-32-Bytes!";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "https://localhost:5001",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "tms-client",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddIdentityCore<TmsUser>(options =>
{
    // Enterprise Password Policy
    options.Password.RequiredLength = 12;
    options.Password.RequireUppercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;

    // Brute-Force Lockout Protection
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<TmsDbContext>()
.AddDefaultTokenProviders();

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
}).DisableRateLimiting();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).DisableRateLimiting();

//app.UseExceptionHandler();

// =============================================
// EXERCISE 1B: Add custom logging middleware FIRST
// =============================================
app.UseMiddleware<RequestLoggingMiddleware>();    // from Session 1

// (Optional) UseExceptionHandler - add it here if you want, 
// but it's fine to leave it out for now as we are just logging.
app.UseExceptionHandler();                        // catches exceptions
app.UseStatusCodePages();                         // adds ProblemDetails for status codes like 404

app.UseRouting();

// Security Response Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';");

    await next();
});

app.UseCors("TmsClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
// Issue XSRF-TOKEN cookie for authenticated users
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true || 
        context.Request.Cookies.ContainsKey("tms_auth"))
    {
        var antiforgery = context.RequestServices
            .GetRequiredService<IAntiforgery>();

        var tokens = antiforgery.GetAndStoreTokens(context);

        context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
{
    HttpOnly = false,      // MUST be false so Angular JavaScript can read it
    Secure = !app.Environment.IsDevelopment(),
    SameSite = SameSiteMode.Strict
});

    }

    await next(context);
});

// Environment-aware configuration
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();   // Scalar UI at /scalar/v1
}
// In production, we do NOT map OpenAPI/Scalar, so they return 404.

app.UseMiddleware<V1DeprecationMiddleware>();
// ===== EXERCISE 5: Map Controllers =====
app.MapControllers();
app.MapHub<TmsHub>("/hubs/tms").RequireCors("TmsClient");

app.MapGet("/", () => Results.Ok(new
{
    message = "TMS API is running",
    hub = "/hubs/tms"
}));

// Session 1 Endpoint
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();








// Grade Submission Endpoint




// Grade Submission Endpoints with Resource-based Instructor Authorization & Approval Validation
app.MapPost("/api/v1/grades", async (
    HttpContext httpContext,
    TmsDbContext db,
    GradeSubmitDto dto) =>
{
    var course = await db.Courses.FindAsync(dto.CourseId);
    if (course == null)
    {
        return Results.NotFound(new { detail = "Course not found." });
    }

    var user = httpContext.User;
    var isAuthenticated = user.Identity?.IsAuthenticated == true;
    var isAdmin = user.IsInRole("Admin");
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    var username = user.Identity?.Name ?? user.FindFirst("name")?.Value ?? user.FindFirst(ClaimTypes.Name)?.Value;
    var email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value;

    if (!isAuthenticated && httpContext.Request.Cookies.TryGetValue("tms_auth", out var cookieVal))
    {
        var parts = cookieVal.Split(':');
        if (parts.Length > 1 && parts[1].Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            isAdmin = true;
            isAuthenticated = true;
        }
        else if (parts.Length > 0)
        {
            username = parts[0];
            isAuthenticated = true;
        }
    }

    // Enforce instructor ownership: only the assigned instructor or Admin can submit grades
    if (!isAdmin)
    {
        var courseInst = (course.InstructorId ?? "").Trim().ToLowerInvariant();
        var matchId = !string.IsNullOrEmpty(userId) && courseInst.Equals(userId.Trim().ToLowerInvariant());
        var matchUser = !string.IsNullOrEmpty(username) && (courseInst.Equals(username.Trim().ToLowerInvariant()) || courseInst.Contains(username.Trim().ToLowerInvariant()) || username.Trim().ToLowerInvariant().Contains(courseInst));
        var matchEmail = !string.IsNullOrEmpty(email) && (courseInst.Equals(email.Trim().ToLowerInvariant()) || email.Trim().ToLowerInvariant().StartsWith(courseInst));

        if (!matchId && !matchUser && !matchEmail && !string.IsNullOrEmpty(courseInst))
        {
            return Results.Problem(
                title: "Forbidden: Grade Editing Restricted",
                detail: $"Access Denied: You are not assigned to instruct '{course.Code} - {course.Title}'. Instructors can only submit or edit grades for their own students.",
                statusCode: StatusCodes.Status403Forbidden);
        }
    }

    var enrollment = await db.Enrollments
        .FirstOrDefaultAsync(e => e.StudentId == dto.StudentId && e.CourseId == dto.CourseId);

    if (enrollment == null)
    {
        return Results.NotFound(new { detail = $"No enrollment record found for Student #{dto.StudentId} in course '{course.Code}'." });
    }

    // Business Rule Validation: Enrollment MUST be Approved before grading!
    var status = TmsApi.Api.Controllers.V2.EnrollmentsController.GetEnrollmentStatus(enrollment);
    if (enrollment.IsArchived || status != "Approved")
    {
        return Results.Problem(
            title: "Cannot Grade Pending Enrollment",
            detail: $"Cannot submit grade: The enrollment request for student #{dto.StudentId} in '{course.Code}' is currently '{status}'. The instructor must officially approve the enrollment request before a grade can be evaluated.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    decimal gradeVal = dto.Score > 4.0 ? (decimal)Math.Min(4.0, (dto.Score / 100.0) * 4.0) : (decimal)dto.Score;
    enrollment.Grade = gradeVal;
    await db.SaveChangesAsync();

    var recordId = "GRD-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    return Results.Ok(new { id = recordId, success = true, grade = gradeVal });
});

app.MapPost("/api/grades", async (
    HttpContext httpContext,
    TmsDbContext db,
    GradeSubmitDto dto) =>
{
    var course = await db.Courses.FindAsync(dto.CourseId);
    if (course == null)
    {
        return Results.NotFound(new { detail = "Course not found." });
    }

    var user = httpContext.User;
    var isAuthenticated = user.Identity?.IsAuthenticated == true;
    var isAdmin = user.IsInRole("Admin");
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    var username = user.Identity?.Name ?? user.FindFirst("name")?.Value ?? user.FindFirst(ClaimTypes.Name)?.Value;
    var email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value;

    if (!isAuthenticated && httpContext.Request.Cookies.TryGetValue("tms_auth", out var cookieVal))
    {
        var parts = cookieVal.Split(':');
        if (parts.Length > 1 && parts[1].Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            isAdmin = true;
            isAuthenticated = true;
        }
        else if (parts.Length > 0)
        {
            username = parts[0];
            isAuthenticated = true;
        }
    }

    if (!isAdmin)
    {
        var courseInst = (course.InstructorId ?? "").Trim().ToLowerInvariant();
        var matchId = !string.IsNullOrEmpty(userId) && courseInst.Equals(userId.Trim().ToLowerInvariant());
        var matchUser = !string.IsNullOrEmpty(username) && (courseInst.Equals(username.Trim().ToLowerInvariant()) || courseInst.Contains(username.Trim().ToLowerInvariant()) || username.Trim().ToLowerInvariant().Contains(courseInst));
        var matchEmail = !string.IsNullOrEmpty(email) && (courseInst.Equals(email.Trim().ToLowerInvariant()) || email.Trim().ToLowerInvariant().StartsWith(courseInst));

        if (!matchId && !matchUser && !matchEmail && !string.IsNullOrEmpty(courseInst))
        {
            return Results.Problem(
                title: "Forbidden: Grade Editing Restricted",
                detail: $"Access Denied: You are not assigned to instruct '{course.Code} - {course.Title}'. Instructors can only submit or edit grades for their own students.",
                statusCode: StatusCodes.Status403Forbidden);
        }
    }

    var enrollment = await db.Enrollments
        .FirstOrDefaultAsync(e => e.StudentId == dto.StudentId && e.CourseId == dto.CourseId);

    if (enrollment == null)
    {
        return Results.NotFound(new { detail = $"No enrollment record found for Student #{dto.StudentId} in course '{course.Code}'." });
    }

    var status = TmsApi.Api.Controllers.V2.EnrollmentsController.GetEnrollmentStatus(enrollment);
    if (enrollment.IsArchived || status != "Approved")
    {
        return Results.Problem(
            title: "Cannot Grade Pending Enrollment",
            detail: $"Cannot submit grade: The enrollment request for student #{dto.StudentId} in '{course.Code}' is currently '{status}'. The instructor must officially approve the enrollment request before a grade can be evaluated.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    decimal gradeVal = dto.Score > 4.0 ? (decimal)Math.Min(4.0, (dto.Score / 100.0) * 4.0) : (decimal)dto.Score;
    enrollment.Grade = gradeVal;
    await db.SaveChangesAsync();

    var recordId = "GRD-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    return Results.Ok(new { id = recordId, success = true, grade = gradeVal });
});

// Session 2 Smoke Test Endpoint
app.MapGet("/api/enrollments/worker-smoke", (EnrollmentWorker worker) =>
{
    worker.ProcessBatch();
    return Results.Ok("processed");
});

/*// ===== TEMPORARY TEST ENDPOINTS - Remove after verifying logging =====
// Changed from MapPost to MapGet for easy browser testing
app.MapGet("/test/enroll", async (IEnrollmentService service, string studentId, string courseCode) =>
{
    var result = await service.EnrollAsync(studentId, courseCode);
    return Results.Ok(result);
});

app.MapGet("/test/enrollment/{id}", async (IEnrollmentService service, string id) =>
{
    var result = await service.GetByIdAsync(id);
    return result is not null ? Results.Ok(result) : Results.NotFound();
});

app.MapDelete("/test/enrollment/{id}", async (IEnrollmentService service, string id) =>
{
    var result = await service.DeleteAsync(id);
    return result ? Results.Ok("Deleted") : Results.NotFound();
});*/

// ===== Test Error Endpoint =====
app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
});



// Seed test data at startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    context.Database.Migrate(); // applies any pending migrations
    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"Courses\" ADD COLUMN IF NOT EXISTS \"InstructorId\" text;");
        context.Database.ExecuteSqlRaw("ALTER TABLE \"AspNetUsers\" ADD COLUMN IF NOT EXISTS \"IsApproved\" boolean DEFAULT false;");
        context.Database.ExecuteSqlRaw("ALTER TABLE \"AspNetUsers\" ADD COLUMN IF NOT EXISTS \"ApprovalStatus\" text DEFAULT 'Pending';");
        // Ensure legacy pre-existing users are approved
        context.Database.ExecuteSqlRaw("UPDATE \"AspNetUsers\" SET \"IsApproved\" = true, \"ApprovalStatus\" = 'Approved' WHERE \"Email\" = 'admin@cotbe.edu.et' OR \"UserName\" = 'admin';");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Schema update: " + ex.Message);

    try
    {
        // Seed Instructor IDs on existing courses if null
        context.Database.ExecuteSqlRaw(@"
            UPDATE ""Courses"" SET ""InstructorId"" = 'admin' WHERE ""Code"" IN ('CS-101', 'CSE-101', 'CSE-301') AND (""InstructorId"" IS NULL OR ""InstructorId"" = '');
            UPDATE ""Courses"" SET ""InstructorId"" = 'instructor' WHERE ""Code"" IN ('CS-201', 'CSE-102', 'CSE-201') AND (""InstructorId"" IS NULL OR ""InstructorId"" = '');
            UPDATE ""Courses"" SET ""InstructorId"" = 'prof.smith' WHERE (""InstructorId"" IS NULL OR ""InstructorId"" = '');
        ");
    }
    catch { }

    }

    if (!context.Students.Any())
    {
        var students = new List<Student>
        {
            new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", GPA = 3.8m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince", GPA = 3.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright", GPA = 2.5m, IsActive = true }
        };
        context.Students.AddRange(students);

        var courses = new List<Course>
        {
            new() { Code = "CS-101", Title = "Introduction to Computer Science", MaxCapacity = 30 },
            new() { Code = "CS-201", Title = "Data Structures and Algorithms", MaxCapacity = 25 },
            new() { Code = "MAT-101", Title = "Calculus I", MaxCapacity = 40 }
        };
        context.Courses.AddRange(courses);

        context.SaveChanges(); // saves Students and Courses

        var enrollments = new List<Enrollment>
        {
            new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
            new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
            new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
            new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
        };
        context.Enrollments.AddRange(enrollments);
        context.SaveChanges();
         
    }
}

// Seed test data at startup (only in Development)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    DataSeeder.SeedAsync(context).GetAwaiter().GetResult();
}

// --- Lab-only fake certificate service ---
var attempts = 0;
app.MapPost("/fake/certificates", async () =>
{
    var n = Interlocked.Increment(ref attempts);

    if (n % 7 == 0)
    {
        // Hang - simulates a downstream that never responds
        await Task.Delay(TimeSpan.FromSeconds(20));
        return Results.Ok(new { Status = "issued", Attempt = n });
    }
    if (n % 3 != 0)
    {
        // Transient: 503 Service Unavailable
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    if (n % 11 == 0)
    {
        // Non-transient: 400 - Polly must NOT retry this
        return Results.BadRequest(new { error = "validation_failed" });
    }
    return Results.Ok(new { Status = "issued", Attempt = n });
}).WithTags("lab-fixtures");

app.Run();
public record GradeSubmitDto(int StudentId, int CourseId, double Score);
