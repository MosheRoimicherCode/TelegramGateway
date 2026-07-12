namespace TelegramGateway.Api.Models.Frontend;

public sealed class FrontendSupportFileMessageRequest
{
    public required string SessionId { get; init; }
    public required string PhoneNumber { get; init; }
    public required string UserName { get; init; }

    public string ProjectName { get; init; } = string.Empty;
    public string Caption { get; init; } = string.Empty;
}
