using System.Text.Json.Serialization;

namespace TelegramGateway.Api.Models.Telegram;

public sealed class TelegramChatDto
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
}
