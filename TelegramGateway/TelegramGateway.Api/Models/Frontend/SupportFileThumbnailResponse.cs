namespace TelegramGateway.Api.Models.Frontend;

public sealed class SupportFileThumbnailResponse
{
    public required string TelegramFileId { get; init; }
    public string TelegramFileUniqueId { get; init; } = string.Empty;

    public required string PreviewUrl { get; init; }

    public int Width { get; init; }
    public int Height { get; init; }
    public long FileSize { get; init; }
}
