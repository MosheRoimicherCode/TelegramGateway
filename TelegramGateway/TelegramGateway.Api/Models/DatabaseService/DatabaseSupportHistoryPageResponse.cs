using TelegramGateway.Api.Models.Frontend;

namespace TelegramGateway.Api.Models.DatabaseService;

public sealed class DatabaseSupportHistoryPageResponse
{
    public IReadOnlyList<SupportMessageResponse> Messages { get; init; } = [];
    public long NextMessageId { get; init; }
    public bool HasMore { get; init; }
}
