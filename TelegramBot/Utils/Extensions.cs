using System.ComponentModel;
using System.Reflection;
using Telegram.Bot.Types;

namespace TelegramBot.Utils;

public static class Extensions
{
    public static Dictionary<string, object?>? GetTraceFields(this Update? update)
    {
        if (update is null) return null;

        var result = new Dictionary<string, object?>();
        result.Add("update_id", update.Id);
        result.Add("message_id", update.Message?.MessageId);
        if (update.Message is not null)
        {
            result.Add("message_text", update.Message.Text);
            result.Add("from_id", update.Message.From?.Id);
            if (update.Message.From is not null)
            {
                result.Add("from_bot", update.Message.From.IsBot);
                result.Add("from_username", update.Message.From.Username);
                result.Add("from_name", $"{update.Message.From.FirstName} {update.Message.From.LastName}");
                result.Add("from_language", update.Message.From.LanguageCode);
            }

            result.Add("chat_id", update.Message.Chat.Id);
            result.Add("chat_type", update.Message.Chat.Type);
            if (update.Message.Audio is not null) result.Add("audio", update.Message.Audio);
            if (update.Message.Photo is not null) result.Add("photo", update.Message.Photo);
            if (update.Message.Poll is not null) result.Add("poll", update.Message.Poll);
            if (update.Message.Video is not null) result.Add("video", update.Message.Video);
            if (update.Message.Sticker is not null) result.Add("sticker", update.Message.Sticker);
            if (update.Message.Document is not null) result.Add("document", update.Message.Document?.FileName);
        }

        result.Add("update_type", update.Type);
        result.Add("callbackquery", update.CallbackQuery);
        if (update.CallbackQuery is not null)
        {
            result.Add("callbackquery_data", update.CallbackQuery.Data);
            result.Add("callbackquery_from_username", update.CallbackQuery.From.Username);
        }

        return result;
    }

    public static IEnumerable<RoomPlayer> WhereAlive(this IEnumerable<RoomPlayer> players) =>
        players.Where(x => x.IsAlive);

    public static IEnumerable<RoomPlayer> WhereEvil(this IEnumerable<RoomPlayer> players) =>
        players.Where(x => x.IsEvil());

    public static IEnumerable<RoomPlayer> WhereNotEvil(this IEnumerable<RoomPlayer> players) =>
        players.Where(x => !x.IsEvil());

    public static bool IsEvil(this RoomPlayer player) => (byte)player.Role >= 128;

    public static IEnumerable<RoomPlayer> SortByRole(this IEnumerable<RoomPlayer> players) =>
        players.OrderBy(p => p.Role);

    public static string GetLocalized<T>(this T enumValue) where T : Enum
    {
        var field = enumValue.GetType().GetField(enumValue.ToString());
        if (field is null) return enumValue.ToString();
        var attribute = Attribute.GetCustomAttribute(field, typeof(LocalizedAttribute)) as LocalizedAttribute;
        return attribute?.Russian ?? enumValue.ToString();
    }
}