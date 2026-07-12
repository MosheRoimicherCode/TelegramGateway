using TelegramGateway.Api.Models.Frontend;
using TelegramGateway.Api.Services.Orchestration;

namespace TelegramGateway.Api.Services;

public sealed class SupportMessageService : ISupportMessageService
{
    private readonly ISupportMessageOrchestrator _orchestrator;

    public SupportMessageService(ISupportMessageOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public Task<SupportHistoryPageResponse> GetHistoryAsync(
        string phoneNumber,
        string projectName,
        string? afterCursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return _orchestrator.GetHistoryAsync(
            phoneNumber,
            projectName,
            afterCursor,
            limit,
            cancellationToken);
    }

    public Task<SendSupportMessageResponse> SendFromFrontendAsync(
        FrontendSendSupportMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        return _orchestrator.SendFromFrontendAsync(request, cancellationToken);
    }
}
