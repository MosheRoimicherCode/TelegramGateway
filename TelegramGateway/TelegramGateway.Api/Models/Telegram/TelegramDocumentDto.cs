using System.Text.Json.Serialization;

namespace TelegramGateway.Api.Models.Telegram;

public sealed class TelegramDocumentDto
{
    [JsonPropertyName("file_id")]
    public string FileId { get; init; } = string.Empty;

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; init; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }

    [JsonPropertyName("mime_type")]
    public string? MimeType { get; init; }

    [JsonPropertyName("file_size")]
    public long? FileSize { get; init; }

    [JsonPropertyName("thumbnail")]
    public TelegramThumbnailDto? Thumbnail { get; init; }
}
