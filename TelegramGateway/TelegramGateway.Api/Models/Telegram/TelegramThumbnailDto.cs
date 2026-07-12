using System.Text.Json.Serialization;

namespace TelegramGateway.Api.Models.Telegram;

public sealed class TelegramThumbnailDto
{
    [JsonPropertyName("file_id")]
    public string FileId { get; init; } = string.Empty;

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; init; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("file_size")]
    public long? FileSize { get; init; }
}
