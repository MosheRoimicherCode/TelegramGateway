namespace TelegramGateway.Api.Models.DatabaseService;

public sealed class SaveSupportMessageFileTcpData
{
    public required string TelegramFileId { get; init; }

    public string TelegramFileUniqueId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public required string FileKind { get; init; }

    public SaveSupportMessageFileThumbnailTcpData? Thumbnail { get; init; }
}
