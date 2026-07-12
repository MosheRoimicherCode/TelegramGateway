using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;

namespace TelegramGateway.Api.Services;

public static class HistoryCursorCodec
{
    public static string Encode(long messageId)
    {
        var json = JsonSerializer.Serialize(new CursorData { Id = messageId });
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(json));
    }

    public static bool TryDecode(string? cursor, out long messageId)
    {
        messageId = 0;

        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        try
        {
            var json = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(cursor));
            var data = JsonSerializer.Deserialize<CursorData>(json);

            if (data is null || data.Id < 0)
            {
                return false;
            }

            messageId = data.Id;
            return true;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return false;
        }
    }

    private sealed class CursorData
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }
    }
}
