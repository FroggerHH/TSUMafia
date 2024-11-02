using Telegram.Bot;
using Telegram.Bot.Exceptions;
using TelegramBot.Utils;

namespace TelegramBot.Handlers.GameRoomStates;

public class WakeupBadguys(GameRoom room)
{
    private readonly List<Task> _tasks = [];
    public bool IsFinished => _tasks.All(x => x.IsCompleted);

    public void Process()
    {
        Logger.Call(nameof(WakeupBadguys), nameof(Process));
        var playersByRole = room.Players
            .WhereEvil()
            .SortByRole()
            .GroupBy(x => x.Role)
            .ToDictionary(x => x.Key, x => x.ToList());

        foreach (var pair in playersByRole) HandleRole(pair);
    }

    private async void HandleRole(KeyValuePair<TelegramBot.Role, List<RoomPlayer>> pair)
    {
        var role = pair.Key;
        var players = pair.Value;
        Logger.TraceCall(new Dictionary<string, object?>
        {
            { "chat_id", room.Chat.Id },
            { "currentday", room.CurrentDay },
            { "role", role },
            { "players", players.GetString() },
        }, nameof(WakeupBadguys), nameof(HandleRole));

        var handler = Program.RoleHandlers.Find(x => x.Role == pair.Key);
        if (handler is null)
        {
            Logger.Warning($"role {pair.Key} has no handler", nameof(WakeupBadguys), nameof(HandleRole));
            await Task.WhenAll(players.Select(async pl =>
            {
                try
                {
                    return await Program.Bot.SendTextMessageAsync(pl.User.Id,
                        "Пока варвары беснуются, вы с удовольствием пересматривая свои любимые фильмы ужасов");
                }
                catch (ApiRequestException e)
                {
                    return await Program.Bot.SendTextMessageAsync(room.Chat,
                        $"Не могу написать {pl.User.Username} в лс. Возможно ты меня заблокировал 😒");
                }
            }));
            return;
        }

        var cts = new CancellationTokenSource();
        cts.CancelAfter(handler.TimeLimit_sec * 1000);
        // await handler.HandleGameplayAsync(room, cts.Token);
        _tasks.Add(handler.HandleGameplayAsync(room, cts.Token));
    }
}