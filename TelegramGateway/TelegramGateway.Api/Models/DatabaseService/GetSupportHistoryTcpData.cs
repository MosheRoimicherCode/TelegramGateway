namespace TelegramGateway.Api.Models.DatabaseService;

public sealed class GetSupportHistoryTcpData
{
    public required string PhoneNumber { get; init; }
    public required string ProjectName { get; init; }
    public long AfterMessageId { get; init; }
    public int Limit { get; init; } = 50;
}
