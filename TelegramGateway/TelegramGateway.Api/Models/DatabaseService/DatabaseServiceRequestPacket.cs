using System.Text.Json;

namespace TelegramGateway.Api.Models.DatabaseService;

public sealed class DatabaseServiceRequestPacket
{
    public required string Type { get; init; }
    public JsonElement? Data { get; init; }
}
