using TelegramGateway.Api.Models.Frontend;

namespace TelegramGateway.Api.Services;

public interface ISupportMessageService
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
}
