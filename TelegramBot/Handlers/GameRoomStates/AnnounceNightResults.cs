using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Utils;

namespace TelegramBot.Handlers.GameRoomStates;

public static class AnnounceNightResults
{
    public static async void SendMessage(GameRoom room)
    {
        room.CurrentDay++;
        Logger.Call(nameof(NightStarts), nameof(SendMessage));
        await DayStartsMsg(room);
        await WhoIsKilled(room);
        await NightStarts.SendWhoIsAliveMsg(room);

        room.SetState(GameRoomState.DayDiscussion);
    }

    private static Task<Message> DayStartsMsg(GameRoom room)
    {
        return Program.Bot.SendVideoAsync(room.Chat, new InputFileUrl("https://i.imgur.com/Nz4sDmZ.gif"),
            caption:
            $"🏙 День {room.CurrentDay}\nСолнце всходит, подсушивая на тротуарах пролитую ночью кровь...",
            cancellationToken: room.Cts.Token);
    }

    private static Task<Message[]> WhoIsKilled(GameRoom room)
    {
        var kills = room.KilledThisNight
            .Select(pl => Program.Bot.SendTextMessageAsync(room.Chat,
                $"Этой ночью был жестоко убит {pl.Role.GetLocalized()} [{pl.User.FirstName}](https://t.me/{pl.User.Username})",
                parseMode: ParseMode.Markdown, disableWebPagePreview: true))
            .ToList();

        foreach (var player in room.Players)
            player.IsAlive = !room.KilledThisNight.Contains(player);
        room.KilledThisNight.Clear();

        return Task.WhenAll(kills);
    }
}