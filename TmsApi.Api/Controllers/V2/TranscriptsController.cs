using System.Threading.Channels;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Transcripts;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v2/transcripts")]
[ApiVersion("2.0")]
public class TranscriptsController : ControllerBase
{
    private readonly Channel<TranscriptRequest> _channel;
    private readonly ITranscriptStatusStore _statusStore;

    public TranscriptsController(
        Channel<TranscriptRequest> channel,
        ITranscriptStatusStore statusStore)
    {
        _channel = channel;
        _statusStore = statusStore;
    }

    [HttpPost]
    [EnableRateLimiting("transcripts")]
    public async Task<IActionResult> RequestTranscript(
        TranscriptRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await _statusStore.GetReportIdForIdempotencyKeyAsync(idempotencyKey, ct);
            if (existing is not null)
            {
                var existingStatus = await _statusStore.GetAsync(existing, ct);
                return Accepted(Url.Action(nameof(GetStatus), new { id = existing }), existingStatus);
            }
        }

        var reportId = Guid.NewGuid().ToString("N")[..12];
        var status = await _statusStore.CreateAsync(reportId, request.StudentId, ct);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            await _statusStore.LinkIdempotencyKeyAsync(idempotencyKey, reportId, ct);

        await _channel.Writer.WriteAsync(request.WithReportId(reportId), ct);

        Response.Headers.RetryAfter = "5";
        return Accepted(Url.Action(nameof(GetStatus), new { id = reportId }), status);
    }

    [HttpGet("{id}/status")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    public async Task<IActionResult> GetStatus(string id, CancellationToken ct)
    {
        var status = await _statusStore.GetAsync(id, ct);
        return status is null
            ? NotFound(new ProblemDetails
            {
                Title = "Transcript not found",
                Detail = $"No transcript request with id '{id}'.",
                Status = StatusCodes.Status404NotFound
            })
            : Ok(status);
    }
}
