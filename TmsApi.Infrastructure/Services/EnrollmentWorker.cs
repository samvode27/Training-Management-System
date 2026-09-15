//using TmsApi.Infrastructure.Services; 
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Interfaces;

namespace TmsApi.Infrastructure.Services;

public class EnrollmentWorker
{
    private readonly IServiceScopeFactory _scopeFactory;

    // Constructor now takes IServiceScopeFactory, NOT IEnrollmentService
    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void ProcessBatch()
    {
        // Create a short-lived scope using the injected factory
        using var scope = _scopeFactory.CreateScope();

        // Resolve the scoped service from the new scope's provider
        var enrollmentService = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

        // Use the service
        // For now, just demonstrate it works
        // In real code, you'd call enrollmentService.EnrollAsync() etc.
        
        // Example: Log that we processed a batch
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EnrollmentWorker>>();
        logger.LogInformation("Processed enrollment batch in a scoped context");
    }
}