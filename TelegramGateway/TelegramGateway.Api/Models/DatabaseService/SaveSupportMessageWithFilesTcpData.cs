namespace TelegramGateway.Api.Models.DatabaseService;

public sealed class SaveSupportMessageWithFilesTcpData
{
    public required string SessionId { get; init; }
    public required string UserName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string ProjectName { get; init; }
    public string Text { get; init; } = string.Empty;
    public long TelegramChatId { get; init; }
    public long TelegramMessageId { get; init; }
    public required string Direction { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    public IReadOnlyList<SaveSupportMessageFileTcpData> Files { get; init; } = [];
}
