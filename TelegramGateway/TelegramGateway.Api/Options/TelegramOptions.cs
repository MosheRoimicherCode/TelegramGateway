namespace TelegramGateway.Api.Options;

public sealed class TelegramOptions
{
    public string BotToken { get; set; } = string.Empty;
    public string TargetChatId { get; set; } = string.Empty;
    public string WebhookSecretToken { get; set; } = string.Empty;
    public string BotApiBaseUrl { get; set; } = "https://api.telegram.org";
}
