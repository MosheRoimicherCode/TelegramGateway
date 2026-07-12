using System.Text.Json.Serialization;

namespace TelegramGateway.Api.Models.Telegram;

public sealed class TelegramFileDto
{
    [JsonPropertyName("file_id")]
    public string FileId { get; init; } = string.Empty;

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; init; } = string.Empty;

    [JsonPropertyName("file_size")]
    public long? FileSize { get; init; }

    [JsonPropertyName("file_path")]
    public string? FilePath { get; init; }
}
