using Telegram.Bot;
using Telegram.Bot.Exceptions;
using TelegramBot.Utils;

namespace TelegramBot.Handlers.GameRoomStates;

public class WakeupGoodguys(GameRoom room)
{
    public async void Process()
    {
        Logger.Call(nameof(WakeupGoodguys), nameof(Process));
        var playersByRole = room.Players.WhereNotEvil().SortByRole()
            .GroupBy(roomPlayer => roomPlayer.Role)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var pair in playersByRole) await HandleRole(pair);

        room.SetState(GameRoomState.AnnounceNightResults);
    }

    private async Task HandleRole(KeyValuePair<TelegramBot.Role, List<RoomPlayer>> pair)
    {
        var role = pair.Key;
        var players = pair.Value;
        Logger.TraceCall(new Dictionary<string, object?>
        {
            { "chat_id", room.Chat.Id },
            { "currentday", room.CurrentDay },
            { "role", role },
            { "players", players.GetString() },
        }, nameof(WakeupGoodguys), nameof(HandleRole));

        var handler = Program.RoleHandlers.Find(x => x.Role == pair.Key);
        if (handler is null)
        {
            Logger.Warning($"role {pair.Key} has no handler", nameof(WakeupGoodguys), nameof(HandleRole));
            await Task.WhenAll(players.Select(async x =>
            {
                try
                {
                    return await Program.Bot.SendTextMessageAsync(x.User.Id,
                        "Этой ночью вы сама безмятежность на фоне суетящихся добряков.");
                }
                catch (ApiRequestException e)
                {
                    return await Program.Bot.SendTextMessageAsync(room.Chat,
                        $"Не могу написать {x.User.Username} в лс. Возможно ты меня заблокировал 😒");
                }
            }));
            return;
        }

        var cts = new CancellationTokenSource();
        cts.CancelAfter(handler.TimeLimit_sec * 1000);
        await handler.HandleGameplayAsync(room, cts.Token);
    }
}