using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TelegramGateway.Api.Models.Telegram;
using TelegramGateway.Api.Options;
using TelegramGateway.Api.Services.Orchestration;

namespace TelegramGateway.Api.Controllers;

[ApiController]
[Route("api/telegram/webhook")]
public sealed class TelegramWebhookController : ControllerBase
{
    private const string TelegramSecretHeader = "8733184405:AAGRHK3FzDn4NRJQsKIYf6EXAaX0JDjJAKA";

    private readonly ISupportMessageOrchestrator _orchestrator;
    private readonly TelegramOptions _telegramOptions;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        ISupportMessageOrchestrator orchestrator,
        IOptions<TelegramOptions> telegramOptions,
        ILogger<TelegramWebhookController> logger)
    {
        _orchestrator = orchestrator;
        _telegramOptions = telegramOptions.Value;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Receive(
        [FromBody] TelegramUpdateDto update,
        CancellationToken cancellationToken)
    {
        if (!IsValidSecretToken())
        {
            _logger.LogWarning("Telegram webhook rejected because the secret token header is invalid.");
            return Unauthorized(new { error = "Invalid Telegram webhook secret token." });
        }

        var response = await _orchestrator.HandleTelegramWebhookAsync(update, cancellationToken);
        if (!response.Ok)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = response.Error ?? "Telegram webhook processing failed." });
        }

        return Ok(response);
    }

    private bool IsValidSecretToken()
    {
        if (string.IsNullOrWhiteSpace(_telegramOptions.WebhookSecretToken))
        {
            return true;
        }

        if (!Request.Headers.TryGetValue(TelegramSecretHeader, out var providedSecretToken))
        {
            return false;
        }

        return string.Equals(
            providedSecretToken.ToString(),
            _telegramOptions.WebhookSecretToken,
            StringComparison.Ordinal);
    }
}
