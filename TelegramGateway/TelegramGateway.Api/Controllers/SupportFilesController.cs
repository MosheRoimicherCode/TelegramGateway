using Microsoft.AspNetCore.Mvc;
using TelegramGateway.Api.Services.Telegram;

namespace TelegramGateway.Api.Controllers;

[ApiController]
[Route("api/support/files")]
public sealed class SupportFilesController : ControllerBase
{
    private readonly ITelegramBotService _telegram;
    private readonly ILogger<SupportFilesController> _logger;

    public SupportFilesController(
        ITelegramBotService telegram,
        ILogger<SupportFilesController> logger)
    {
        _telegram = telegram;
        _logger = logger;
    }

    [HttpGet("{telegramFileId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> DownloadFile(
        [FromRoute] string telegramFileId,
        [FromQuery] string? fileName,
        CancellationToken cancellationToken)
    {
        return DownloadTelegramFileAsync(telegramFileId, fileName, false, cancellationToken);
    }

    [HttpGet("{telegramFileId}/thumbnail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> DownloadThumbnail(
        [FromRoute] string telegramFileId,
        CancellationToken cancellationToken)
    {
        return DownloadTelegramFileAsync(telegramFileId, null, true, cancellationToken);
    }

    private async Task<IActionResult> DownloadTelegramFileAsync(
        string telegramFileId,
        string? requestedFileName,
        bool isThumbnail,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(telegramFileId))
        {
            return BadRequest(new { error = "Telegram file id is required." });
        }

        try
        {
            var download = await _telegram.DownloadFileAsync(telegramFileId, cancellationToken);

            _logger.LogInformation(
                "Telegram file streamed to frontend. TelegramFileId={TelegramFileId}, IsThumbnail={IsThumbnail}",
                telegramFileId,
                isThumbnail);

            if (isThumbnail)
            {
                return File(download.Stream, download.ContentType);
            }

            var downloadFileName = GetSafeFileName(requestedFileName)
                ?? GetSafeFileName(download.FileName)
                ?? "download";

            return File(download.Stream, download.ContentType, downloadFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Telegram file download failed. TelegramFileId={TelegramFileId}, IsThumbnail={IsThumbnail}",
                telegramFileId,
                isThumbnail);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "Telegram file download failed." });
        }
    }

    private static string? GetSafeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var safeFileName = Path.GetFileName(fileName.Trim());
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            safeFileName = safeFileName.Replace(invalidCharacter, '_');
        }

        return string.IsNullOrWhiteSpace(safeFileName) ? null : safeFileName;
    }
}
