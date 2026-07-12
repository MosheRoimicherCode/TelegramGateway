namespace TelegramGateway.Api.Models.Frontend;

public sealed class SupportFileResponse
{
    public required string TelegramFileId { get; init; }
    public string TelegramFileUniqueId { get; init; } = string.Empty;

    public required string FileName { get; init; }
    public required string MimeType { get; init; }
    public long FileSize { get; init; }

    public required string DownloadUrl { get; init; }

    public SupportFileThumbnailResponse? Thumbnail { get; init; }
}
