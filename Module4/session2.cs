// Captive Dependency

// public interface IEnrollmentService
// {
//     void EnrollStudent();
// }

// // Implementation
// public class EnrollmentService : IEnrollmentService
// {
//     public void EnrollStudent()
//     {
//         Console.WriteLine("Student enrolled");
//     }
// }

// // AddScoped(one instance per request) Request 1 -> Create EnrollmentService -> Use it -> Destroy it
// builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();


// Singleton Service
// public class EnrollmentWorker
// {
// }

// // one instanse for all applications
// builder.Services.AddSingleton<EnrollmentWorker>();




// Exercise 3: Options Pattern
// public class PaymentService
// {
//     public void Pay()
//     {
//         string gatewayUrl = "https://payment.com";
//         decimal maxDeposit = 10000;
//     }
// }
// if Gateway URL changes (Open code -> Change code -> Rebuild application -> Deploy again)

// so add Configuration File
// {
//   "Payments": {
//     "GatewayUrl": "https://payment.com",
//     "MaxDepositBirr": 10000
//   }
// }

// // then Take Payments section and fill PaymentOptions object
// builder.Services
//     .AddOptions<PaymentOptions>()
//     .BindConfiguration("Payments");


// ValidateOnStart()
// builder.Services
//     .AddOptions<PaymentOptions>()
//     .BindConfiguration("Payments")
//     .ValidateDataAnnotations()
//     .ValidateOnStart();



// Exercise 4: Structured Logging
//  _logger.LogInformation(
//   "Enrolling student {StudentId} in course {CourseCode}",
//   studentId,
//   courseCode);

// // Duplicate Enrollment
// _logger.LogWarning(
//   "Duplicate enrollment attempt
//    {StudentId}
//    already in
//    {CourseCode}",
//    studentId,
//    courseCode);