using System.Text.Json.Serialization;

namespace TelegramGateway.Api.Models.Telegram;

public sealed class TelegramUserDto
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("first_name")]
    public string FirstName { get; init; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string? LastName { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }
}
