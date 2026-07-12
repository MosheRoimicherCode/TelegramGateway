using System.Text.Json;

namespace TelegramGateway.Api.Models.DatabaseService;

public sealed class DatabaseServiceResponsePacket
{
    public bool Ok { get; init; }
    public JsonElement? Data { get; init; }
    public string? Error { get; init; }
}
