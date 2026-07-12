namespace TelegramGateway.Api.Models.Internal;

public sealed class GatewayThumbnailMetadata
{
    public required string TelegramFileId { get; init; }
    public string TelegramFileUniqueId { get; init; } = string.Empty;

    public int Width { get; init; }
    public int Height { get; init; }
    public long FileSize { get; init; }
}
