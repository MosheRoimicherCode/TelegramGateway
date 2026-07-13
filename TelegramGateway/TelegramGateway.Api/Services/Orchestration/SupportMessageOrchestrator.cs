using System.Text.Json;
using TelegramGateway.Api.Models.DatabaseService;
using TelegramGateway.Api.Models.Frontend;
using TelegramGateway.Api.Models.Internal;
using TelegramGateway.Api.Models.Telegram;
using TelegramGateway.Api.Services.DatabaseService;
using TelegramGateway.Api.Services.Telegram;

namespace TelegramGateway.Api.Services.Orchestration;

public sealed class SupportMessageOrchestrator : ISupportMessageOrchestrator
{
    private const string SaveSupportMessageCommand = "SaveSupportMessage";
    private const string SaveSupportMessageWithFilesCommand = "SaveSupportMessageWithFiles";
    private const string GetSupportHistoryCommand = "GetSupportHistory";
    private const string GetMessageContextCommand = "GetMessageContextByTelegramReference";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ITelegramBotService _telegram;
    private readonly IDatabaseServiceTcpClient _databaseService;
    private readonly ILogger<SupportMessageOrchestrator> _logger;

    public SupportMessageOrchestrator(
        ITelegramBotService telegram,
        IDatabaseServiceTcpClient databaseService,
        ILogger<SupportMessageOrchestrator> logger)
    {
        _telegram = telegram;
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task<SupportHistoryPageResponse> GetHistoryAsync(
        string phoneNumber,
        string projectName,
        string? afterCursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (!HistoryCursorCodec.TryDecode(afterCursor, out var afterMessageId))
        {
            throw new ArgumentException("The history cursor is invalid.", nameof(afterCursor));
        }

        _logger.LogInformation(
            "Requesting support history from DatabaseService. PhoneNumber={PhoneNumber}, ProjectName={ProjectName}, AfterMessageId={AfterMessageId}, Limit={Limit}",
            phoneNumber,
            projectName,
            afterMessageId,
            limit);

        var response = await _databaseService.SendAsync(
            GetSupportHistoryCommand,
            new GetSupportHistoryTcpData
            {
                PhoneNumber = phoneNumber,
                ProjectName = projectName,
                AfterMessageId = afterMessageId,
                Limit = limit
            },
            cancellationToken);

        if (!response.Ok)
        {
            _logger.LogError(
                "DatabaseService failed to return support history. PhoneNumber={PhoneNumber}, ProjectName={ProjectName}, Error={Error}",
                phoneNumber,
                projectName,
                response.Error);

            return new SupportHistoryPageResponse
            {
                NextCursor = HistoryCursorCodec.Encode(afterMessageId)
            };
        }

        var databasePage = response.Data is null
            ? new DatabaseSupportHistoryPageResponse { NextMessageId = afterMessageId }
            : response.Data.Value.Deserialize<DatabaseSupportHistoryPageResponse>(JsonOptions)
                ?? new DatabaseSupportHistoryPageResponse { NextMessageId = afterMessageId };

        var history = AddFrontendFileUrls(databasePage.Messages);
        var page = new SupportHistoryPageResponse
        {
            Messages = history,
            NextCursor = HistoryCursorCodec.Encode(databasePage.NextMessageId),
            HasMore = databasePage.HasMore
        };

        _logger.LogInformation(
            "Support history returned successfully to frontend. PhoneNumber={PhoneNumber}, ProjectName={ProjectName}, Count={Count}",
            phoneNumber,
            projectName,
            history.Count);

        return page;
    }

    public async Task<SendSupportMessageResponse> SendFromFrontendAsync(
        FrontendSendSupportMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var hasText = !string.IsNullOrWhiteSpace(request.Text);
        var hasFile = request.File is { Length: > 0 };

        _logger.LogInformation(
            "Starting frontend message path. SessionId={SessionId}, PhoneNumber={PhoneNumber}, ProjectName={ProjectName}, HasText={HasText}, HasFile={HasFile}",
            request.SessionId,
            request.PhoneNumber,
            request.ProjectName,
            hasText,
            hasFile);

        TelegramSendMessageResult telegramResult;
        try
        {
            telegramResult = await _telegram.SendFrontendMessageAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Frontend message path failed while sending to Telegram. SessionId={SessionId}, PhoneNumber={PhoneNumber}, ProjectName={ProjectName}",
                request.SessionId,
                request.PhoneNumber,
                request.ProjectName);

            return new SendSupportMessageResponse
            {
                Ok = false,
                Error = "Telegram send failed."
            };
        }

        _logger.LogInformation(
            "Telegram received frontend message. SessionId={SessionId}, TelegramMessageId={TelegramMessageId}, FileCount={FileCount}",
            request.SessionId,
            telegramResult.TelegramMessageId,
            telegramResult.Files.Count);

        var databaseResponse = await SaveHistoryAsync(request, telegramResult, cancellationToken);
        if (!databaseResponse.Ok)
        {
            _logger.LogError(
                "DatabaseService failed to store frontend message history. SessionId={SessionId}, TelegramMessageId={TelegramMessageId}, Error={Error}",
                request.SessionId,
                telegramResult.TelegramMessageId,
                databaseResponse.Error);

            return new SendSupportMessageResponse
            {
                Ok = false,
                Error = databaseResponse.Error ?? "DatabaseService save failed."
            };
        }

        _logger.LogInformation(
            "Frontend message path completed successfully. Telegram received message and DatabaseService stored history. SessionId={SessionId}, TelegramMessageId={TelegramMessageId}, FileCount={FileCount}",
            request.SessionId,
            telegramResult.TelegramMessageId,
            telegramResult.Files.Count);

        return new SendSupportMessageResponse
        {
            Ok = true
        };
    }

    public async Task<TelegramWebhookProcessResponse> HandleTelegramWebhookAsync(
        TelegramUpdateDto update,
        CancellationToken cancellationToken = default)
    {
        if (update.Message is null)
        {
            _logger.LogInformation(
                "Telegram webhook update ignored because it does not contain a message. UpdateId={UpdateId}",
                update.UpdateId);

            return new TelegramWebhookProcessResponse
            {
                Ok = true,
                Ignored = true
            };
        }

        var message = update.Message;
        // Telegram puts text written together with a photo/document in Caption.
        // Prefer a real text value, but fall back when Text is null or empty.
        var text = string.IsNullOrWhiteSpace(message.Text)
            ? message.Caption ?? string.Empty
            : message.Text;
        var files = _telegram.ExtractFiles(message);

        if (string.IsNullOrWhiteSpace(text) && files.Count == 0)
        {
            _logger.LogInformation(
                "Telegram webhook message ignored because it has no text and no supported file. UpdateId={UpdateId}, TelegramMessageId={TelegramMessageId}",
                update.UpdateId,
                message.MessageId);

            return new TelegramWebhookProcessResponse
            {
                Ok = true,
                Ignored = true
            };
        }

        if (message.Chat is null || message.ReplyToMessage is null)
        {
            _logger.LogInformation(
                "Telegram webhook message ignored because it is not a reply to a correlated support message. UpdateId={UpdateId}, TelegramMessageId={TelegramMessageId}",
                update.UpdateId,
                message.MessageId);

            return new TelegramWebhookProcessResponse
            {
                Ok = true,
                Ignored = true
            };
        }

        DatabaseServiceResponsePacket contextResponse;
        try
        {
            contextResponse = await _databaseService.SendAsync(
                GetMessageContextCommand,
                new GetMessageContextByTelegramReferenceTcpData
                {
                    TelegramChatId = message.Chat.Id,
                    TelegramMessageId = message.ReplyToMessage.MessageId
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DatabaseService context lookup failed. UpdateId={UpdateId}, TelegramChatId={TelegramChatId}, RepliedToMessageId={RepliedToMessageId}",
                update.UpdateId,
                message.Chat.Id,
                message.ReplyToMessage.MessageId);

            return new TelegramWebhookProcessResponse
            {
                Ok = false,
                Error = "DatabaseService context lookup failed."
            };
        }

        if (!contextResponse.Ok || contextResponse.Data is null)
        {
            _logger.LogWarning(
                "Telegram reply ignored because its original message context was not found. UpdateId={UpdateId}, TelegramChatId={TelegramChatId}, RepliedToMessageId={RepliedToMessageId}, Error={Error}",
                update.UpdateId,
                message.Chat.Id,
                message.ReplyToMessage.MessageId,
                contextResponse.Error);

            return new TelegramWebhookProcessResponse
            {
                Ok = true,
                Ignored = true
            };
        }

        var originalContext = contextResponse.Data.Value
            .Deserialize<TelegramMessageContextTcpResponse>(JsonOptions);

        if (originalContext is null)
        {
            return new TelegramWebhookProcessResponse
            {
                Ok = false,
                Error = "Original message context is invalid."
            };
        }

        var chatId = message.Chat.Id;
        var sessionId = originalContext.SessionId;
        var createdAtUtc = message.Date > 0
            ? DateTimeOffset.FromUnixTimeSeconds(message.Date).UtcDateTime
            : DateTime.UtcNow;

        _logger.LogInformation(
            "Telegram webhook message received. UpdateId={UpdateId}, TelegramMessageId={TelegramMessageId}, SessionId={SessionId}, FileCount={FileCount}",
            update.UpdateId,
            message.MessageId,
            sessionId,
            files.Count);

        try
        {
            var databaseResponse = await SaveWebhookHistoryAsync(
                sessionId,
                originalContext.UserName,
                originalContext.PhoneNumber,
                originalContext.ProjectName,
                text,
                chatId,
                message.MessageId,
                createdAtUtc,
                files,
                cancellationToken);

            if (!databaseResponse.Ok)
            {
                _logger.LogError(
                    "DatabaseService failed to store Telegram webhook message. UpdateId={UpdateId}, TelegramMessageId={TelegramMessageId}, Error={Error}",
                    update.UpdateId,
                    message.MessageId,
                    databaseResponse.Error);

                return new TelegramWebhookProcessResponse
                {
                    Ok = false,
                    Error = databaseResponse.Error ?? "DatabaseService save failed."
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Telegram webhook path failed while storing message. UpdateId={UpdateId}, TelegramMessageId={TelegramMessageId}",
                update.UpdateId,
                message.MessageId);

            return new TelegramWebhookProcessResponse
            {
                Ok = false,
                Error = "DatabaseService save failed."
            };
        }

        _logger.LogInformation(
            "Telegram webhook path completed successfully. UpdateId={UpdateId}, TelegramMessageId={TelegramMessageId}, FileCount={FileCount}",
            update.UpdateId,
            message.MessageId,
            files.Count);

        return new TelegramWebhookProcessResponse
        {
            Ok = true
        };
    }

    private Task<DatabaseServiceResponsePacket> SaveHistoryAsync(
        FrontendSendSupportMessageRequest request,
        TelegramSendMessageResult telegramResult,
        CancellationToken cancellationToken)
    {
        if (telegramResult.Files.Count == 0)
        {
            var data = new SaveSupportMessageTcpData
            {
                SessionId = request.SessionId,
                UserName = request.UserName,
                PhoneNumber = request.PhoneNumber,
                ProjectName = request.ProjectName,
                Text = telegramResult.Text,
                TelegramChatId = telegramResult.TelegramChatId,
                TelegramMessageId = telegramResult.TelegramMessageId,
                Direction = MessageDirection.FrontendToTelegram.ToString(),
                CreatedAtUtc = DateTime.UtcNow
            };

            return _databaseService.SendAsync(SaveSupportMessageCommand, data, cancellationToken);
        }

        var dataWithFiles = new SaveSupportMessageWithFilesTcpData
        {
            SessionId = request.SessionId,
            UserName = request.UserName,
            PhoneNumber = request.PhoneNumber,
            ProjectName = request.ProjectName,
            Text = telegramResult.Text,
            TelegramChatId = telegramResult.TelegramChatId,
            TelegramMessageId = telegramResult.TelegramMessageId,
            Direction = MessageDirection.FrontendToTelegram.ToString(),
            CreatedAtUtc = DateTime.UtcNow,
            Files = telegramResult.Files.Select(ToTcpFileData).ToArray()
        };

        return _databaseService.SendAsync(SaveSupportMessageWithFilesCommand, dataWithFiles, cancellationToken);
    }

    private Task<DatabaseServiceResponsePacket> SaveWebhookHistoryAsync(
        string sessionId,
        string userName,
        string phoneNumber,
        string projectName,
        string text,
        long telegramChatId,
        long telegramMessageId,
        DateTime createdAtUtc,
        IReadOnlyList<GatewayFileMetadata> files,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            var data = new SaveSupportMessageTcpData
            {
                SessionId = sessionId,
                UserName = userName,
                PhoneNumber = phoneNumber,
                ProjectName = projectName,
                Text = text,
                TelegramChatId = telegramChatId,
                TelegramMessageId = telegramMessageId,
                Direction = MessageDirection.TelegramToFrontend.ToString(),
                CreatedAtUtc = createdAtUtc
            };

            return _databaseService.SendAsync(SaveSupportMessageCommand, data, cancellationToken);
        }

        var dataWithFiles = new SaveSupportMessageWithFilesTcpData
        {
            SessionId = sessionId,
            UserName = userName,
            PhoneNumber = phoneNumber,
            ProjectName = projectName,
            Text = text,
            TelegramChatId = telegramChatId,
            TelegramMessageId = telegramMessageId,
            Direction = MessageDirection.TelegramToFrontend.ToString(),
            CreatedAtUtc = createdAtUtc,
            Files = files.Select(ToTcpFileData).ToArray()
        };

        return _databaseService.SendAsync(SaveSupportMessageWithFilesCommand, dataWithFiles, cancellationToken);
    }

    private static SaveSupportMessageFileTcpData ToTcpFileData(GatewayFileMetadata file)
    {
        return new SaveSupportMessageFileTcpData
        {
            TelegramFileId = file.TelegramFileId,
            TelegramFileUniqueId = file.TelegramFileUniqueId,
            FileName = file.FileName,
            MimeType = file.MimeType,
            FileSize = file.FileSize,
            FileKind = file.FileKind,
            Thumbnail = file.Thumbnail is null
                ? null
                : new SaveSupportMessageFileThumbnailTcpData
                {
                    TelegramFileId = file.Thumbnail.TelegramFileId,
                    TelegramFileUniqueId = file.Thumbnail.TelegramFileUniqueId,
                    Width = file.Thumbnail.Width,
                    Height = file.Thumbnail.Height,
                    FileSize = file.Thumbnail.FileSize
            }
        };
    }

    private static IReadOnlyList<SupportMessageResponse> AddFrontendFileUrls(
        IReadOnlyList<SupportMessageResponse> history)
    {
        return history.Select(message => new SupportMessageResponse
        {
            SessionId = message.SessionId,
            Direction = message.Direction,
            Text = message.Text,
            CreatedAtUtc = message.CreatedAtUtc,
            Files = message.Files.Select(AddFrontendFileUrls).ToArray()
        }).ToArray();
    }

    private static SupportFileResponse AddFrontendFileUrls(SupportFileResponse file)
    {
        return new SupportFileResponse
        {
            TelegramFileId = file.TelegramFileId,
            TelegramFileUniqueId = file.TelegramFileUniqueId,
            FileName = file.FileName,
            MimeType = file.MimeType,
            FileSize = file.FileSize,
            DownloadUrl = BuildFileDownloadUrl(file.TelegramFileId, file.FileName),
            Thumbnail = file.Thumbnail is null
                ? null
                : new SupportFileThumbnailResponse
                {
                    TelegramFileId = file.Thumbnail.TelegramFileId,
                    TelegramFileUniqueId = file.Thumbnail.TelegramFileUniqueId,
                    PreviewUrl = string.IsNullOrWhiteSpace(file.Thumbnail.PreviewUrl)
                        ? BuildThumbnailDownloadUrl(file.Thumbnail.TelegramFileId)
                        : file.Thumbnail.PreviewUrl,
                    Width = file.Thumbnail.Width,
                    Height = file.Thumbnail.Height,
                    FileSize = file.Thumbnail.FileSize
                }
        };
    }

    private static string BuildFileDownloadUrl(string telegramFileId, string fileName)
    {
        var url = $"api/support/files/{Uri.EscapeDataString(telegramFileId)}";
        return string.IsNullOrWhiteSpace(fileName)
            ? url
            : $"{url}?fileName={Uri.EscapeDataString(fileName.Trim())}";
    }

    private static string BuildThumbnailDownloadUrl(string telegramFileId)
    {
        return $"api/support/files/{Uri.EscapeDataString(telegramFileId)}/thumbnail";
    }

    private static string BuildTelegramUserName(TelegramUserDto? user)
    {
        if (user is null)
        {
            return "Telegram user";
        }

        var fullName = string.Join(
            ' ',
            new[] { user.FirstName, user.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return string.IsNullOrWhiteSpace(user.Username)
            ? $"Telegram user {user.Id}"
            : user.Username;
    }
}
