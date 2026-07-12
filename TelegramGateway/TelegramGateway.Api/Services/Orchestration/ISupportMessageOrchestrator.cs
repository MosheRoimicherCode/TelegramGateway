using TelegramGateway.Api.Models.Frontend;
using TelegramGateway.Api.Models.Telegram;

namespace TelegramGateway.Api.Services.Orchestration;

public interface ISupportMessageOrchestrator
{
    Task<SupportHistoryPageResponse> GetHistoryAsync(
        string phoneNumber,
        string projectName,
        string? afterCursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<SendSupportMessageResponse> SendFromFrontendAsync(
        FrontendSendSupportMessageRequest request,
        CancellationToken cancellationToken = default);

    Task<TelegramWebhookProcessResponse> HandleTelegramWebhookAsync(
        TelegramUpdateDto update,
        CancellationToken cancellationToken = default);
}
