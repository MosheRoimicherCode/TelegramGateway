using TelegramGateway.Api.Models.DatabaseService;

namespace TelegramGateway.Api.Services.DatabaseService;

public interface IDatabaseServiceTcpClient
{
    Task<DatabaseServiceResponsePacket> SendAsync<TData>(
        string commandType,
        TData data,
        CancellationToken cancellationToken = default);
}
