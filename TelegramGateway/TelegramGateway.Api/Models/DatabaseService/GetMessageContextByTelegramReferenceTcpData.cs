namespace TelegramGateway.Api.Models.DatabaseService;

public sealed class GetMessageContextByTelegramReferenceTcpData
{
    public long TelegramChatId { get; init; }
    public long TelegramMessageId { get; init; }
}

public sealed class TelegramMessageContextTcpResponse
{
    public long UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
}
