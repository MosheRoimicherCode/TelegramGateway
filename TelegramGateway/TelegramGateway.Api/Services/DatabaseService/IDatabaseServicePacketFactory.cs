namespace TelegramGateway.Api.Services.DatabaseService;

public interface IDatabaseServicePacketFactory
{
    string CreatePacketJson<TData>(string commandType, TData data);
}
