namespace TelegramGateway.Api.Models.Frontend;

public sealed class FrontendSupportMessageRequest
{
    public required string SessionId { get; init; }
    public required string PhoneNumber { get; init; }
    public required string UserName { get; init; }
    public required string Message { get; init; }

    public string ProjectName { get; init; } = string.Empty;
}
