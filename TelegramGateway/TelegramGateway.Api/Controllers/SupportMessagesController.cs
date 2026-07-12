using Microsoft.AspNetCore.Mvc;
using TelegramGateway.Api.Models.Frontend;
using TelegramGateway.Api.Services;

namespace TelegramGateway.Api.Controllers;

[ApiController]
[Route("api/support/messages")]
public sealed class SupportMessagesController : ControllerBase
{
    private readonly ISupportMessageService _supportMessages;

    public SupportMessagesController(ISupportMessageService supportMessages)
    {
        _supportMessages = supportMessages;
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(SupportHistoryPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string phoneNumber,
        [FromQuery] string projectName,
        [FromQuery] string? afterCursor = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            missingFields.Add(nameof(phoneNumber));
        }

        if (string.IsNullOrWhiteSpace(projectName))
        {
            missingFields.Add(nameof(projectName));
        }

        if (missingFields.Count > 0)
        {
            return BadRequest(new
            {
                error = "Missing required fields.",
                missingFields
            });
        }

        if (limit is < 1 or > 100)
        {
            return BadRequest(new { error = "Limit must be between 1 and 100." });
        }

        if (!HistoryCursorCodec.TryDecode(afterCursor, out _))
        {
            return BadRequest(new { error = "afterCursor is invalid." });
        }

        try
        {
            var history = await _supportMessages.GetHistoryAsync(
                phoneNumber,
                projectName,
                afterCursor,
                limit,
                cancellationToken);

            return Ok(history);
        }
        catch (NotImplementedException ex)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new { error = ex.Message });
        }
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SendSupportMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> SendFromFrontend(
        [FromForm] FrontendSendSupportMessageRequest request,
        CancellationToken cancellationToken)
    {
        var missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            missingFields.Add(nameof(request.SessionId));
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            missingFields.Add(nameof(request.PhoneNumber));
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            missingFields.Add(nameof(request.UserName));
        }

        var hasText = !string.IsNullOrWhiteSpace(request.Text);
        var hasFile = request.File is { Length: > 0 };

        if (!hasText && !hasFile)
        {
            missingFields.Add("Text or File");
        }

        if (missingFields.Count > 0)
        {
            return BadRequest(new
            {
                error = "Missing required fields.",
                missingFields
            });
        }

        try
        {
            var response = await _supportMessages.SendFromFrontendAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (NotImplementedException ex)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new { error = ex.Message });
        }
    }
}
