using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Hubs;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Transcripts;

namespace TmsApi.Infrastructure.Workers;

public class TranscriptWorker : BackgroundService
{
    private readonly Channel<TranscriptRequest> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITranscriptStatusStore _statusStore;
    private readonly IHubContext<TmsHub, ITmsHubClient> _hubContext;
    private readonly ILogger<TranscriptWorker> _logger;

    public TranscriptWorker(
        Channel<TranscriptRequest> channel,
        IServiceScopeFactory scopeFactory,
        ITranscriptStatusStore statusStore,
        IHubContext<TmsHub, ITmsHubClient> hubContext,
        ILogger<TranscriptWorker> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _statusStore = statusStore;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Transcript worker started.");

        await foreach (var request in _channel.Reader.ReadAllAsync(ct))
        {
            var reportId = request.ReportId!;
            try
            {
                await _statusStore.MarkProcessingAsync(reportId, ct);
                _logger.LogInformation("Generating transcript {ReportId}", reportId);
                
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                
                var downloadUrl = $"/api/v2/transcripts/{reportId}/download";
                await _statusStore.MarkReadyAsync(reportId, downloadUrl, ct);

                await _hubContext.Clients
                    .Group(GroupNames.Student(request.StudentId.ToString()))
                    .ReceiveTranscriptReady(reportId, downloadUrl);

                _logger.LogInformation("Transcript ready + notification sent: {ReportId}", reportId);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed: {ReportId}", reportId);
                await _statusStore.MarkFailedAsync(reportId, ex.Message, CancellationToken.None);
            }
        }
    }
}