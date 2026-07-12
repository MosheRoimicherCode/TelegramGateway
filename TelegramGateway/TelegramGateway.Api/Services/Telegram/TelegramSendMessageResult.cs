using TelegramGateway.Api.Models.Internal;

namespace TelegramGateway.Api.Services.Telegram;

public sealed class TelegramSendMessageResult
{
    public long TelegramChatId { get; init; }
    public long TelegramMessageId { get; init; }
    public string Text { get; init; } = string.Empty;
    public IReadOnlyList<GatewayFileMetadata> Files { get; init; } = [];
}
