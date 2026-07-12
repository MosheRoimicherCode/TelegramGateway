using TelegramGateway.Api.Models.Frontend;
using TelegramGateway.Api.Models.Internal;
using TelegramGateway.Api.Models.Telegram;

namespace TelegramGateway.Api.Services.Telegram;

public interface ITelegramBotService
{
    Task<TelegramSendMessageResult> SendFrontendMessageAsync(
        FrontendSendSupportMessageRequest request,
        CancellationToken cancellationToken = default);

    Task<TelegramFileDownload> DownloadFileAsync(
        string telegramFileId,
        CancellationToken cancellationToken = default);

    IReadOnlyList<GatewayFileMetadata> ExtractFiles(TelegramMessageDto message);
}
