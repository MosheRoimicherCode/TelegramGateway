using System.Text.Json;
using TelegramGateway.Api.Models.DatabaseService;

namespace TelegramGateway.Api.Services.DatabaseService;

public sealed class DatabaseServicePacketFactory : IDatabaseServicePacketFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string CreatePacketJson<TData>(string commandType, TData data)
    {
        if (string.IsNullOrWhiteSpace(commandType))
        {
            throw new ArgumentException("Command type is required.", nameof(commandType));
        }

        var packet = new DatabaseServiceRequestPacket
        {
            Type = commandType,
            Data = JsonSerializer.SerializeToElement(data, JsonOptions)
        };

        return JsonSerializer.Serialize(packet, JsonOptions);
    }
}
