namespace TelegramGateway.Api.Models.DatabaseService;

public sealed class SaveSupportMessageTcpData
{
    public required string SessionId { get; init; }
    public required string UserName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string ProjectName { get; init; }
    public required string Text { get; init; }
    public long TelegramChatId { get; init; }
    public long TelegramMessageId { get; init; }
    public required string Direction { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
