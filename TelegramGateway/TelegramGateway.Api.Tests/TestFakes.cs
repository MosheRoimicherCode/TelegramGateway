using TelegramGateway.Api.Models.DatabaseService;
using TelegramGateway.Api.Models.Frontend;
using TelegramGateway.Api.Models.Internal;
using TelegramGateway.Api.Models.Telegram;
using TelegramGateway.Api.Services.DatabaseService;
using TelegramGateway.Api.Services.Telegram;

namespace TelegramGateway.Api.Tests;

internal sealed class FakeDatabaseServiceTcpClient : IDatabaseServiceTcpClient
{
    public Queue<DatabaseServiceResponsePacket> Responses { get; } = new();
    public List<DatabaseCall> Calls { get; } = [];

    public Task<DatabaseServiceResponsePacket> SendAsync<TData>(
        string commandType,
        TData data,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(new DatabaseCall(commandType, data!));
        return Task.FromResult(Responses.Count > 0
            ? Responses.Dequeue()
            : new DatabaseServiceResponsePacket { Ok = true });
    }
}

internal sealed record DatabaseCall(string CommandType, object Data);

internal sealed class FakeTelegramBotService : ITelegramBotService
{
    public IReadOnlyList<GatewayFileMetadata> ExtractedFiles { get; init; } = [];
    public TelegramFileDownload? FileDownload { get; init; }

    public Task<TelegramSendMessageResult> SendFrontendMessageAsync(
        FrontendSendSupportMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<TelegramFileDownload> DownloadFileAsync(
        string telegramFileId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(FileDownload ?? throw new NotSupportedException());
    }

    public IReadOnlyList<GatewayFileMetadata> ExtractFiles(TelegramMessageDto message)
    {
        return ExtractedFiles;
    }
}
