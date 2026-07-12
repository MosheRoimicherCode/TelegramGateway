namespace TelegramGateway.Api.Models.Internal;

public sealed class GatewayFileMetadata
{
    public required string TelegramFileId { get; init; }
    public string TelegramFileUniqueId { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public long FileSize { get; init; }

    public required string FileKind { get; init; }

    public GatewayThumbnailMetadata? Thumbnail { get; init; }
}
