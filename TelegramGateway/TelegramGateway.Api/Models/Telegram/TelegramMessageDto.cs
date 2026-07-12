using System.Text.Json.Serialization;

namespace TelegramGateway.Api.Models.Telegram;

public sealed class TelegramMessageDto
{
    [JsonPropertyName("message_id")]
    public long MessageId { get; init; }

    [JsonPropertyName("from")]
    public TelegramUserDto? From { get; init; }

    [JsonPropertyName("chat")]
    public TelegramChatDto? Chat { get; init; }

    [JsonPropertyName("date")]
    public long Date { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("caption")]
    public string? Caption { get; init; }

    [JsonPropertyName("document")]
    public TelegramDocumentDto? Document { get; init; }

    [JsonPropertyName("photo")]
    public IReadOnlyList<TelegramPhotoSizeDto>? Photo { get; init; }

    [JsonPropertyName("reply_to_message")]
    public TelegramMessageDto? ReplyToMessage { get; init; }
}
