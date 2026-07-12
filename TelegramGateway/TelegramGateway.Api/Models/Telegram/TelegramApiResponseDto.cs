using System.Text.Json.Serialization;

namespace TelegramGateway.Api.Models.Telegram;

public sealed class TelegramApiResponseDto<T>
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    [JsonPropertyName("result")]
    public T? Result { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
