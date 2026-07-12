namespace TelegramGateway.Api.Services.Telegram;

public sealed class TelegramFileDownload
{
    public required Stream Stream { get; init; }
    public string ContentType { get; init; } = "application/octet-stream";
}
