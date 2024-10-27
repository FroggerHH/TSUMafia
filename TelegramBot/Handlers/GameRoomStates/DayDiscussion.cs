using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Utils;

namespace TelegramBot.Handlers.GameRoomStates;

public static class DayDiscussion
{
    public static async void Process(GameRoom room)
    {
        await Program.Bot.SendTextMessageAsync(room.Chat,
            "Сейчас самое время обсудить результаты ночи, разобраться в причинах и следствиях...",
            cancellationToken: room.Cts.Token);
        await Task.Delay(30_000);
        room.SetState(GameRoomState.DayVoting);
    }
}