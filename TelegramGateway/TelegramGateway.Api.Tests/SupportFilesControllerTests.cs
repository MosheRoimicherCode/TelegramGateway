using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using TelegramGateway.Api.Controllers;
using TelegramGateway.Api.Services.Telegram;

namespace TelegramGateway.Api.Tests;

public sealed class SupportFilesControllerTests
{
    [Fact]
    public async Task DownloadFile_WithOriginalName_ReturnsAttachmentWithOriginalName()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("file contents"));
        var telegram = new FakeTelegramBotService
        {
            FileDownload = new TelegramFileDownload
            {
                Stream = stream,
                ContentType = "application/pdf",
                FileName = "telegram-file.pdf"
            }
        };
        var controller = new SupportFilesController(
            telegram,
            NullLogger<SupportFilesController>.Instance);

        var actionResult = await controller.DownloadFile(
            "telegram-file-id",
            "original drawing.pdf",
            CancellationToken.None);

        var fileResult = Assert.IsType<FileStreamResult>(actionResult);
        Assert.Equal("original drawing.pdf", fileResult.FileDownloadName);
        Assert.Equal("application/pdf", fileResult.ContentType);
    }

    [Fact]
    public async Task DownloadFile_WithoutOriginalName_PreservesTelegramExtension()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("image contents"));
        var telegram = new FakeTelegramBotService
        {
            FileDownload = new TelegramFileDownload
            {
                Stream = stream,
                ContentType = "application/octet-stream",
                FileName = "file_0.png"
            }
        };
        var controller = new SupportFilesController(
            telegram,
            NullLogger<SupportFilesController>.Instance);

        var actionResult = await controller.DownloadFile(
            "telegram-file-id",
            null,
            CancellationToken.None);

        var fileResult = Assert.IsType<FileStreamResult>(actionResult);
        Assert.Equal("file_0.png", fileResult.FileDownloadName);
        Assert.Equal("image/png", fileResult.ContentType);
    }
}
