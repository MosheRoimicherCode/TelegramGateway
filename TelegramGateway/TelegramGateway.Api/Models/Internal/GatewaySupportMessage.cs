namespace TelegramGateway.Api.Models.Internal;

public sealed class GatewaySupportMessage
{
    public required string SessionId { get; init; }
    public required string UserName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Text { get; init; }

    public string ProjectName { get; init; } = string.Empty;
    public MessageDirection Direction { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    public IReadOnlyList<GatewayFileMetadata> Files { get; init; } = [];
}
