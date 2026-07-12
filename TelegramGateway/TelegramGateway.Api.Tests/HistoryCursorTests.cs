using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using TelegramGateway.Api.Models.DatabaseService;
using TelegramGateway.Api.Models.Frontend;
using TelegramGateway.Api.Services;
using TelegramGateway.Api.Services.Orchestration;

namespace TelegramGateway.Api.Tests;

public sealed class HistoryCursorTests
{
    [Fact]
    public void Cursor_RoundTrip_PreservesLastMessageId()
    {
        var cursor = HistoryCursorCodec.Encode(102);

        Assert.Equal("eyJpZCI6MTAyfQ", cursor);
        Assert.True(HistoryCursorCodec.TryDecode(cursor, out var messageId));
        Assert.Equal(102, messageId);
        Assert.False(HistoryCursorCodec.TryDecode("not-a-cursor", out _));
    }

    [Fact]
    public async Task GetHistory_UsesReceivedCursorAndReturnsNextMessageCursor()
    {
        var database = new FakeDatabaseServiceTcpClient();
        database.Responses.Enqueue(Success(new DatabaseSupportHistoryPageResponse
        {
            Messages =
            [
                new SupportMessageResponse
                {
                    SessionId = "session-103",
                    Direction = "TelegramToFrontend",
                    Text = "next message",
                    CreatedAtUtc = DateTime.UtcNow
                }
            ],
            NextMessageId = 103,
            HasMore = false
        }));

        var orchestrator = new SupportMessageOrchestrator(
            new FakeTelegramBotService(),
            database,
            NullLogger<SupportMessageOrchestrator>.Instance);

        var page = await orchestrator.GetHistoryAsync(
            "0585200517",
            "project.fly",
            HistoryCursorCodec.Encode(102),
            10);

        var call = Assert.Single(database.Calls);
        Assert.Equal("GetSupportHistory", call.CommandType);
        var request = Assert.IsType<GetSupportHistoryTcpData>(call.Data);
        Assert.Equal(102, request.AfterMessageId);
        Assert.Equal(10, request.Limit);
        Assert.Equal("next message", Assert.Single(page.Messages).Text);
        Assert.True(HistoryCursorCodec.TryDecode(page.NextCursor, out var nextMessageId));
        Assert.Equal(103, nextMessageId);
    }

    private static DatabaseServiceResponsePacket Success<T>(T data)
    {
        return new DatabaseServiceResponsePacket
        {
            Ok = true,
            Data = JsonSerializer.SerializeToElement(data, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };
    }
}
