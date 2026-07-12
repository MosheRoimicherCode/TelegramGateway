using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TelegramGateway.Api.Models.DatabaseService;
using TelegramGateway.Api.Options;

namespace TelegramGateway.Api.Services.DatabaseService;

public sealed class DatabaseServiceTcpClient : IDatabaseServiceTcpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly DatabaseServiceOptions _options;
    private readonly IDatabaseServicePacketFactory _packetFactory;

    public DatabaseServiceTcpClient(
        IOptions<DatabaseServiceOptions> options,
        IDatabaseServicePacketFactory packetFactory)
    {
        _options = options.Value;
        _packetFactory = packetFactory;
    }

    public async Task<DatabaseServiceResponsePacket> SendAsync<TData>(
        string commandType,
        TData data,
        CancellationToken cancellationToken = default)
    {
        var packetJson = _packetFactory.CreatePacketJson(commandType, data);

        using var client = new TcpClient();
        await client.ConnectAsync(_options.Host, _options.Port, cancellationToken);

        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, leaveOpen: true);
        await using var writer = new StreamWriter(stream, leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n"
        };

        await writer.WriteLineAsync(packetJson.AsMemory(), cancellationToken);

        var responseJson = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            throw new InvalidOperationException("DatabaseService returned an empty response.");
        }

        var response = JsonSerializer.Deserialize<DatabaseServiceResponsePacket>(responseJson, JsonOptions);
        return response ?? throw new InvalidOperationException("DatabaseService response could not be parsed.");
    }
}
