using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Utils;

namespace TelegramBot.Handlers.GameRoomStates;

public static class NightStarts
{
    public static async void SendMessage(GameRoom room)
    {
        Logger.Call(nameof(NightStarts), nameof(SendMessage));
        await NightIsComingMsg(room);
        await SendWhoIsAliveMsg(room);

        room.SetState(GameRoomState.WakeupBadguys);
    }

    private static Task<Message> NightIsComingMsg(GameRoom room) => Program.Bot.SendVideoAsync(room.Chat,
        new InputFileUrl("https://i.imgur.com/UQveEBG.gif"),
        caption:
        "\ud83c\udf03 Наступает ночь\nНа улицы города выходят лишь самые отважные и бесстрашные. Утром попробуем сосчитать их головы...",
        replyMarkup: new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
        {
            new("Перейти к боту")
            {
                CallbackData = "go to bot",
                Url = "https://t.me/tsuMafiaBot"
            },
        }));

    public static Task<Message> SendWhoIsAliveMsg(GameRoom room)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Живые игроки:");
        for (var i = 0; i < room.Players.WhereAlive().ToArray().Length; i++)
        {
            var user = room.Players[i].User;
            sb.AppendLine($"{i + 1}. [{user.FirstName}](https://t.me/{user.Username})");
        }

        sb.AppendLine("Кто-то из них:");
        sb.AppendLine(string.Join(", ", room.Players.WhereAlive().Select(x => x.Role.GetLocalized())));

        return Program.Bot.SendTextMessageAsync(room.Chat,
            sb.ToString(), disableWebPagePreview: true,
            parseMode: ParseMode.Markdown,
            replyMarkup: new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
            {
                new("Перейти к боту")
                {
                    CallbackData = "go to bot",
                    Url = "https://t.me/tsuMafiaBot"
                },
            }));
    }
}