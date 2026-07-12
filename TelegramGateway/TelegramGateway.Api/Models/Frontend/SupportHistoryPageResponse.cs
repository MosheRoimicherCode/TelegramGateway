namespace TelegramGateway.Api.Models.Frontend;

public sealed class SupportHistoryPageResponse
{
    public IReadOnlyList<SupportMessageResponse> Messages { get; init; } = [];
    public string NextCursor { get; init; } = string.Empty;
    public bool HasMore { get; init; }
}
