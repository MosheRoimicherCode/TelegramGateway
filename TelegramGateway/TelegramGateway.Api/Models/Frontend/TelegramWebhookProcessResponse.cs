namespace TelegramGateway.Api.Models.Frontend;

public sealed class TelegramWebhookProcessResponse
{
    public bool Ok { get; init; }
    public bool Ignored { get; init; }
    public string? Error { get; init; }
}
