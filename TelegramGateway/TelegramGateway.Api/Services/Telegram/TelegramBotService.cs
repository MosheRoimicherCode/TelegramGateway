using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TelegramGateway.Api.Models.Frontend;
using TelegramGateway.Api.Models.Internal;
using TelegramGateway.Api.Models.Telegram;
using TelegramGateway.Api.Options;

namespace TelegramGateway.Api.Services.Telegram;

public sealed class TelegramBotService : ITelegramBotService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        ILogger<TelegramBotService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TelegramSendMessageResult> SendFrontendMessageAsync(
        FrontendSendSupportMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateSendOptions();

        if (request.File is { Length: > 0 })
        {
            return await SendDocumentAsync(request, cancellationToken);
        }

        return await SendTextAsync(request, cancellationToken);
    }

    public async Task<TelegramFileDownload> DownloadFileAsync(
        string telegramFileId,
        CancellationToken cancellationToken = default)
    {
        ValidateBotToken();

        if (string.IsNullOrWhiteSpace(telegramFileId))
        {
            throw new ArgumentException("Telegram file id is required.", nameof(telegramFileId));
        }

        var file = await GetTelegramAsync<TelegramFileDto>(
            $"getFile?file_id={Uri.EscapeDataString(telegramFileId)}",
            cancellationToken);

        if (string.IsNullOrWhiteSpace(file.FilePath))
        {
            throw new InvalidOperationException("Telegram did not return a file path.");
        }

        var downloadUrl = BuildTelegramFileUrl(file.FilePath);
        var stream = await _httpClient.GetStreamAsync(downloadUrl, cancellationToken);

        _logger.LogInformation(
            "Telegram file stream opened. TelegramFileId={TelegramFileId}, FilePath={FilePath}",
            telegramFileId,
            file.FilePath);

        return new TelegramFileDownload
        {
            Stream = stream,
            FileName = Path.GetFileName(file.FilePath)
        };
    }

    public IReadOnlyList<GatewayFileMetadata> ExtractFiles(TelegramMessageDto message)
    {
        if (message.Document is not null)
        {
            return [FromDocument(message.Document)];
        }

        if (message.Photo is { Count: > 0 })
        {
            var bestPhoto = message.Photo
                .OrderByDescending(photo => photo.Width * photo.Height)
                .First();

            return [new GatewayFileMetadata
            {
                TelegramFileId = bestPhoto.FileId,
                TelegramFileUniqueId = bestPhoto.FileUniqueId,
                FileName = string.Empty,
                MimeType = "image/jpeg",
                FileSize = bestPhoto.FileSize ?? 0,
                FileKind = "photo",
                Thumbnail = new GatewayThumbnailMetadata
                {
                    TelegramFileId = bestPhoto.FileId,
                    TelegramFileUniqueId = bestPhoto.FileUniqueId,
                    Width = bestPhoto.Width,
                    Height = bestPhoto.Height,
                    FileSize = bestPhoto.FileSize ?? 0
                }
            }];
        }

        return [];
    }

    private async Task<TelegramSendMessageResult> SendTextAsync(
        FrontendSendSupportMessageRequest request,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["chat_id"] = _options.TargetChatId,
            ["text"] = BuildTelegramSupportText(request)
        });

        var telegramMessage = await PostTelegramAsync<TelegramMessageDto>("sendMessage", content, cancellationToken);
        var telegramChatId = telegramMessage.Chat?.Id ?? 0;

        if (telegramChatId == 0)
        {
            throw new InvalidOperationException("Telegram did not return the target chat id.");
        }

        _logger.LogInformation(
            "Telegram accepted text message. TelegramMessageId={TelegramMessageId}, SessionId={SessionId}, ProjectName={ProjectName}",
            telegramMessage.MessageId,
            request.SessionId,
            request.ProjectName);

        return new TelegramSendMessageResult
        {
            TelegramChatId = telegramChatId,
            TelegramMessageId = telegramMessage.MessageId,
            Text = request.Text
        };
    }

    private async Task<TelegramSendMessageResult> SendDocumentAsync(
        FrontendSendSupportMessageRequest request,
        CancellationToken cancellationToken)
    {
        var file = request.File ?? throw new InvalidOperationException("File is missing.");

        await using var fileStream = file.OpenReadStream();
        using var content = new MultipartFormDataContent();

        content.Add(new StringContent(_options.TargetChatId), "chat_id");

        if (!string.IsNullOrWhiteSpace(request.Text))
        {
            content.Add(new StringContent(BuildTelegramSupportText(request)), "caption");
        }

        using var fileContent = new StreamContent(fileStream);
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
        }

        content.Add(fileContent, "document", file.FileName);

        var telegramMessage = await PostTelegramAsync<TelegramMessageDto>("sendDocument", content, cancellationToken);
        var files = ExtractFiles(telegramMessage);
        var telegramChatId = telegramMessage.Chat?.Id ?? 0;

        if (telegramChatId == 0)
        {
            throw new InvalidOperationException("Telegram did not return the target chat id.");
        }

        if (files.Count == 0 || string.IsNullOrWhiteSpace(files[0].TelegramFileId))
        {
            throw new InvalidOperationException("Telegram accepted the file but did not return file metadata.");
        }

        _logger.LogInformation(
            "Telegram accepted file message. TelegramMessageId={TelegramMessageId}, TelegramFileId={TelegramFileId}, SessionId={SessionId}, ProjectName={ProjectName}",
            telegramMessage.MessageId,
            files[0].TelegramFileId,
            request.SessionId,
            request.ProjectName);

        return new TelegramSendMessageResult
        {
            TelegramChatId = telegramChatId,
            TelegramMessageId = telegramMessage.MessageId,
            Text = request.Text,
            Files = files
        };
    }

    private async Task<T> PostTelegramAsync<T>(
        string method,
        HttpContent content,
        CancellationToken cancellationToken)
    {
        var url = BuildTelegramMethodUrl(method);
        using var response = await _httpClient.PostAsync(url, content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        return ParseTelegramResponse<T>(method, response.StatusCode, response.IsSuccessStatusCode, responseJson);
    }

    private async Task<T> GetTelegramAsync<T>(string methodWithQuery, CancellationToken cancellationToken)
    {
        var url = BuildTelegramMethodUrl(methodWithQuery);
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        return ParseTelegramResponse<T>(methodWithQuery, response.StatusCode, response.IsSuccessStatusCode, responseJson);
    }

    private T ParseTelegramResponse<T>(
        string method,
        System.Net.HttpStatusCode statusCode,
        bool isSuccessStatusCode,
        string responseJson)
    {
        TelegramApiResponseDto<T>? telegramResponse;
        try
        {
            telegramResponse = JsonSerializer.Deserialize<TelegramApiResponseDto<T>>(
                responseJson,
                JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Telegram response could not be parsed. Method={Method}, StatusCode={StatusCode}", method, statusCode);
            throw;
        }

        if (!isSuccessStatusCode || telegramResponse?.Ok != true || telegramResponse.Result is null)
        {
            var description = telegramResponse?.Description ?? responseJson;
            _logger.LogError(
                "Telegram API call failed. Method={Method}, StatusCode={StatusCode}, Description={Description}",
                method,
                statusCode,
                description);

            throw new InvalidOperationException($"Telegram API call failed: {description}");
        }

        return telegramResponse.Result;
    }

    private GatewayFileMetadata FromDocument(TelegramDocumentDto document)
    {
        return new GatewayFileMetadata
        {
            TelegramFileId = document.FileId,
            TelegramFileUniqueId = document.FileUniqueId,
            FileName = document.FileName ?? string.Empty,
            MimeType = document.MimeType ?? string.Empty,
            FileSize = document.FileSize ?? 0,
            FileKind = "document",
            Thumbnail = document.Thumbnail is null
                ? null
                : new GatewayThumbnailMetadata
                {
                    TelegramFileId = document.Thumbnail.FileId,
                    TelegramFileUniqueId = document.Thumbnail.FileUniqueId,
                    Width = document.Thumbnail.Width,
                    Height = document.Thumbnail.Height,
                    FileSize = document.Thumbnail.FileSize ?? 0
                }
        };
    }

    private string BuildTelegramMethodUrl(string method)
    {
        return $"{_options.BotApiBaseUrl.TrimEnd('/')}/bot{_options.BotToken}/{method}";
    }

    private string BuildTelegramFileUrl(string filePath)
    {
        return $"{_options.BotApiBaseUrl.TrimEnd('/')}/file/bot{_options.BotToken}/{filePath}";
    }

    private void ValidateSendOptions()
    {
        ValidateBotToken();

        if (string.IsNullOrWhiteSpace(_options.TargetChatId))
        {
            throw new InvalidOperationException("Telegram TargetChatId is missing.");
        }
    }

    private void ValidateBotToken()
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            throw new InvalidOperationException("Telegram BotToken is missing.");
        }
    }

    private static string BuildTelegramSupportText(FrontendSendSupportMessageRequest request)
    {
        return $"""
            Support Request (Session: {request.SessionId}):
            User Name: {request.UserName}
            Phone Number: {request.PhoneNumber}
            Fly Project: {request.ProjectName}

            Message: {request.Text}
            """;
    }
}
