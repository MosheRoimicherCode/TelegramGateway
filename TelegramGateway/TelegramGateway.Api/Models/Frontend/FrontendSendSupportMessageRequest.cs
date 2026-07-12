using Microsoft.AspNetCore.Http;

namespace TelegramGateway.Api.Models.Frontend;

public sealed class FrontendSendSupportMessageRequest
{
    public required string SessionId { get; init; }
    public required string PhoneNumber { get; init; }
    public required string UserName { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public IFormFile? File { get; init; }
}
