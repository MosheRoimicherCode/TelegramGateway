namespace TelegramGateway.Api.Models.Frontend;

public sealed class SupportMessageResponse
{
    public required string SessionId { get; init; }
    public required string Direction { get; init; }
    public string Text { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }

    public IReadOnlyList<SupportFileResponse> Files { get; init; } = [];
}
