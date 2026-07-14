using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TelegramGateway.Api.Models.DatabaseService;
using TelegramGateway.Api.Models.Frontend;
using TelegramGateway.Api.Models.Internal;
using TelegramGateway.Api.Models.Telegram;
using TelegramGateway.Api.Options;
using TelegramGateway.Api.Services.Orchestration;
using TelegramGateway.Api.Services.Telegram;

namespace TelegramGateway.Api.Tests;

public sealed class TelegramFileAndReplyTests
{
    [Fact]
    public async Task SendText_ReturnsOriginalMessageTextForHistory()
    {
        const string telegramJson = """
            {
              "ok": true,
              "result": {
                "message_id": 699,
                "chat": { "id": -100123456, "type": "supergroup" },
                "text": "Support Request (Session: session-1):\nUser Name: Moshe\nPhone Number: 0585200517\nFly Project: project.fly\n\nMessage: customer message"
              }
            }
            """;

        var handler = new RecordingHttpMessageHandler(telegramJson);
        var service = new TelegramBotService(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(new TelegramOptions
            {
                BotToken = "test-token",
                TargetChatId = "-100123456",
                BotApiBaseUrl = "https://telegram.test"
            }),
            NullLogger<TelegramBotService>.Instance);

        var result = await service.SendFrontendMessageAsync(new FrontendSendSupportMessageRequest
        {
            SessionId = "session-1",
            UserName = "Moshe",
            PhoneNumber = "0585200517",
            ProjectName = "project.fly",
            Text = "customer message"
        });

        Assert.Equal("customer message", result.Text);
    }

    [Fact]
    public async Task SendDocument_ReturnsChatMessageFileAndThumbnailIds()
    {
        const string telegramJson = """
            {
              "ok": true,
              "result": {
                "message_id": 700,
                "chat": { "id": -100123456, "type": "supergroup" },
                "caption": "attached drawing",
                "document": {
                  "file_id": "telegram-file-id",
                  "file_unique_id": "telegram-unique-id",
                  "file_name": "drawing.pdf",
                  "mime_type": "application/pdf",
                  "file_size": 2048,
                  "thumbnail": {
                    "file_id": "thumbnail-file-id",
                    "file_unique_id": "thumbnail-unique-id",
                    "width": 320,
                    "height": 200,
                    "file_size": 512
                  }
                }
              }
            }
            """;

        var handler = new RecordingHttpMessageHandler(telegramJson);
        var service = new TelegramBotService(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(new TelegramOptions
            {
                BotToken = "test-token",
                TargetChatId = "-100123456",
                BotApiBaseUrl = "https://telegram.test"
            }),
            NullLogger<TelegramBotService>.Instance);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("file contents"));
        var file = new FormFile(stream, 0, stream.Length, "File", "drawing.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var result = await service.SendFrontendMessageAsync(new FrontendSendSupportMessageRequest
        {
            SessionId = "session-1",
            UserName = "Moshe",
            PhoneNumber = "0585200517",
            ProjectName = "project.fly",
            Text = "attached drawing",
            File = file
        });

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.EndsWith("/bottest-token/sendDocument", handler.RequestUri?.AbsoluteUri);
        Assert.Equal(-100123456, result.TelegramChatId);
        Assert.Equal(700, result.TelegramMessageId);
        Assert.Equal("attached drawing", result.Text);
        var metadata = Assert.Single(result.Files);
        Assert.Equal("telegram-file-id", metadata.TelegramFileId);
        Assert.Equal("thumbnail-file-id", metadata.Thumbnail?.TelegramFileId);
    }

    [Fact]
    public async Task WebhookReply_UsesOriginalConversationAndSavesReplyFileId()
    {
        var database = new FakeDatabaseServiceTcpClient();
        database.Responses.Enqueue(Success(new TelegramMessageContextTcpResponse
        {
            UserId = 42,
            UserName = "Original customer",
            PhoneNumber = "0585200517",
            SessionId = "session-original",
            ProjectName = "project.fly"
        }));
        database.Responses.Enqueue(new DatabaseServiceResponsePacket { Ok = true });

        var telegram = new FakeTelegramBotService
        {
            ExtractedFiles =
            [
                new GatewayFileMetadata
                {
                    TelegramFileId = "reply-file-id",
                    TelegramFileUniqueId = "reply-unique-id",
                    FileName = "office-response.pdf",
                    MimeType = "application/pdf",
                    FileSize = 500,
                    FileKind = "document"
                }
            ]
        };

        var orchestrator = new SupportMessageOrchestrator(
            telegram,
            database,
            NullLogger<SupportMessageOrchestrator>.Instance);

        var response = await orchestrator.HandleTelegramWebhookAsync(new TelegramUpdateDto
        {
            UpdateId = 800,
            Message = new TelegramMessageDto
            {
                MessageId = 701,
                Chat = new TelegramChatDto { Id = -100123456 },
                Caption = "office reply",
                ReplyToMessage = new TelegramMessageDto { MessageId = 700 }
            }
        });

        Assert.True(response.Ok);
        Assert.False(response.Ignored);
        Assert.Equal(2, database.Calls.Count);

        var lookup = database.Calls[0];
        Assert.Equal("GetMessageContextByTelegramReference", lookup.CommandType);
        var lookupData = Assert.IsType<GetMessageContextByTelegramReferenceTcpData>(lookup.Data);
        Assert.Equal(-100123456, lookupData.TelegramChatId);
        Assert.Equal(700, lookupData.TelegramMessageId);

        var save = database.Calls[1];
        Assert.Equal("SaveSupportMessageWithFiles", save.CommandType);
        var saveData = Assert.IsType<SaveSupportMessageWithFilesTcpData>(save.Data);
        Assert.Equal("0585200517", saveData.PhoneNumber);
        Assert.Equal("project.fly", saveData.ProjectName);
        Assert.Equal("session-original", saveData.SessionId);
        Assert.Equal(-100123456, saveData.TelegramChatId);
        Assert.Equal(701, saveData.TelegramMessageId);
        Assert.Equal("TelegramToFrontend", saveData.Direction);
        Assert.Equal("office reply", saveData.Text);
        Assert.Equal("reply-file-id", Assert.Single(saveData.Files).TelegramFileId);
    }

    [Fact]
    public async Task WebhookTextReply_SavesTextWithoutFiles()
    {
        var database = new FakeDatabaseServiceTcpClient();
        database.Responses.Enqueue(Success(new TelegramMessageContextTcpResponse
        {
            UserId = 42,
            UserName = "Original customer",
            PhoneNumber = "0585200517",
            SessionId = "session-original",
            ProjectName = "project.fly"
        }));
        database.Responses.Enqueue(new DatabaseServiceResponsePacket { Ok = true });

        var orchestrator = new SupportMessageOrchestrator(
            new FakeTelegramBotService(),
            database,
            NullLogger<SupportMessageOrchestrator>.Instance);

        var response = await orchestrator.HandleTelegramWebhookAsync(new TelegramUpdateDto
        {
            UpdateId = 801,
            Message = new TelegramMessageDto
            {
                MessageId = 702,
                Chat = new TelegramChatDto { Id = -100123456 },
                Text = "office text reply",
                ReplyToMessage = new TelegramMessageDto { MessageId = 700 }
            }
        });

        Assert.True(response.Ok);
        Assert.Equal(2, database.Calls.Count);

        var save = database.Calls[1];
        Assert.Equal("SaveSupportMessage", save.CommandType);
        var saveData = Assert.IsType<SaveSupportMessageTcpData>(save.Data);
        Assert.Equal("office text reply", saveData.Text);
        Assert.Equal("TelegramToFrontend", saveData.Direction);
        Assert.Equal(702, saveData.TelegramMessageId);
    }

    [Fact]
    public void TelegramWebhookJson_ReadsReplyToMessageId()
    {
        const string json = """
            {
              "update_id": 800,
              "message": {
                "message_id": 701,
                "chat": { "id": -100123456 },
                "reply_to_message": { "message_id": 700 },
                "text": "office reply"
              }
            }
            """;

        var update = JsonSerializer.Deserialize<TelegramUpdateDto>(json);

        Assert.Equal(700, update?.Message?.ReplyToMessage?.MessageId);
    }

    private static DatabaseServiceResponsePacket Success<T>(T data)
    {
        return new DatabaseServiceResponsePacket
        {
            Ok = true,
            Data = JsonSerializer.SerializeToElement(data, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public RecordingHttpMessageHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
            });
        }
    }
}
