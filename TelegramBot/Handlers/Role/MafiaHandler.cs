using System.Diagnostics.CodeAnalysis;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Utils;

namespace TelegramBot.Handlers.Role;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class MafiaHandler : IRoleHandler
{
    private IRoleHandler _roleHandlerImplementation;
    public TelegramBot.Role Role => TelegramBot.Role.Mafia;
    public byte TimeLimit_sec => 10;

    private string GetRandomLocation(GameRoom room) => new List<string>
    {
        " в баре", " в клубе", " в маке", " у Лёхи", " в столовке", " в подвальной библиотеке", " в шараге",
        (Random.Shared.Next(0, 1) == 1 ? " а дальним столиком" : string.Empty) + $" в додо",
        " в офисе", " в офисе", " в офисе",
        $" у {TargetPlayers(room).Random()}",
    }.Random()!;

    private string GetRandomMessage(GameRoom room) =>
        new List<string>
        {
            $"Этой ночью бандиты собрались{GetRandomLocation(room)}\n" +
            "Кто же станет вашей, ребята, целью этой ночью?",

            $"Этой ночью бандиты собрались{GetRandomLocation(room)}\n" +
            "Кто же станет вашей, ребята, целью этой ночью?",

            $"Этой ночью бандиты собрались{GetRandomLocation(room)}. " +
            $"{TargetPlayers(room).Random()!.User.FirstName} поглядывает в вашу сторону.\n" +
            "Кто же станет вашей, ребята, целью этой ночью?",

            $"Этой ночью бандиты собрались{GetRandomLocation(room)}. " +
            $"Вас беспокоит присутствие {TargetPlayers(room).Random()!.User.FirstName}.\n" +
            "Кто же станет вашей, ребята, целью этой ночью?",

            "Выберите жертву:",
            "Выберите жертву:",
            "Выберите жертву:",
            "Выберите жертву:",
            "Выберите жертву:",
            "Выберите жертву:",
            "Выберите жертву:",
            "Выберите жертву:",
            "Выберите жертву:",
        }.Random()!;

    private List<RoomPlayer> TargetPlayers(GameRoom room) => room.Players.Where(player => player.Role == Role).ToList();

    public async Task HandleGameplayAsync(GameRoom room, CancellationToken token)
    {
        Logger.Call(nameof(MafiaHandler), nameof(HandleGameplayAsync));
        try
        {
            room.MafiaTargets = [];
            token.ThrowIfCancellationRequested();

            await AskForVote(room);

            await WaiteForVotes(room, token);

            CheckResults(room);
        }
        catch (OperationCanceledException)
        {
            CheckResults(room);
        }
    }

    private async Task WaiteForVotes(GameRoom room, CancellationToken token)
    {
        int numberOfVotes;
        var mafiaCount = TargetPlayers(room).Count;
        do
        {
            await Task.Delay(200, token);
            numberOfVotes = room.MafiaTargets?.Select(x => x.Value).Sum(x => x) ?? 0;
        } while (numberOfVotes < mafiaCount);
    }

    private async Task AskForVote(GameRoom room)
    {
        var tasks = TargetPlayers(room).Select(async player =>
        {
            string randomMessage;
            do randomMessage = GetRandomMessage(room);
            while (randomMessage.Contains(player.User.FirstName));

            try
            {
                return await Program.Bot.SendTextMessageAsync(player.User.Id, randomMessage,
                    replyMarkup: new InlineKeyboardMarkup(
                        room.Players
                            .WhereNotEvil()
                            .Select(x =>
                                new InlineKeyboardButton($"{x.User.FirstName} {x.User.LastName}")
                                {
                                    CallbackData = $"{nameof(MafiaHandler)} mafia_select {x.User.Id} {room.Chat.Id}"
                                })
                            .ToList()),
                    cancellationToken: room.Cts.Token);
            }
            catch (ApiRequestException e)
            {
                return await Program.Bot.SendTextMessageAsync(room.Chat,
                    $"Не могу написать {player.User.Username} в лс. Возможно ты меня заблокировал 😒");
            }
        }).ToArray();
        await Task.WhenAll(tasks);
    }

    private void CheckResults(GameRoom room)
    {
        if (room.MafiaTargets is null)
        {
            Logger.Error($"room.MafiaTargets is null", nameof(MafiaHandler), "HandleGameplayAsync.catch");
            return;
        }

        var votes = room.MafiaTargets.OrderByDescending(x => x.Value).ToList();
        var numberOfVotes = votes.Sum(x => x.Value);
        if (numberOfVotes <= 0 || votes.Count <= 0 || (votes.Count >= 2 && votes[0].Value == votes[1].Value))
        {
            MafiaNotSure(room);
            return;
        }

        var target = room.Players.Find(x => x.User.Id == votes.First().Key)!;
        room.KilledThisNight.Add(target);

        Program.Bot.SendTextMessageAsync(room.Chat, $"\ud83e\udd35\ud83c\udffb Мафия выбрала жертву...",
            cancellationToken: room.Cts.Token);
    }

    private static async void MafiaNotSure(GameRoom room) =>
        await Program.Bot.SendTextMessageAsync(room.Chat.Id, "Мафия так и не определилась с выбором...",
            cancellationToken: room.Cts.Token);


    public Task HandleCallbackQueryAsync(ITelegramBotClient botClient, Update? update, CancellationToken token)
    {
        if (update?.CallbackQuery?.Data is null || update.CallbackQuery.Message is null ||
            update.CallbackQuery.Data.Length <= 0 ||
            !update.CallbackQuery.Data.StartsWith(nameof(MafiaHandler))) return Task.CompletedTask;


        var split = update.CallbackQuery.Data.Split(" ");
        if (split.Length < 4)
        {
            Logger.Error($"No mafia target player id specified",
                nameof(MafiaHandler), nameof(HandleCallbackQueryAsync));
            return Task.CompletedTask;
        }

        var chatIdStr = split[3];
        if (!long.TryParse(chatIdStr, out long chatId))
        {
            Logger.Error($"{chatIdStr} is not a valid long type for chatId",
                nameof(MafiaHandler), nameof(HandleCallbackQueryAsync));
            return Task.CompletedTask;
        }

        var room = Program.GameRooms.Find(x => x.Chat.Id == chatId);
        if (room is null)
        {
            Logger.Error($"Unable to find room", nameof(MafiaHandler), nameof(HandleCallbackQueryAsync));
            return Task.CompletedTask;
        }

        var playerIdStr = split[2];
        if (!long.TryParse(playerIdStr, out long playerId))
        {
            Logger.Error($"{playerIdStr} is not a valid long type for playerId",
                nameof(MafiaHandler), nameof(HandleCallbackQueryAsync));
            return Task.CompletedTask;
        }

        var roomPlayer = room.Players.Find(x => x.User.Id == playerId);
        if (roomPlayer is null)
        {
            Logger.Error($"roomPlayer for {playerId} not found", nameof(MafiaHandler),
                nameof(HandleCallbackQueryAsync));
            return Task.CompletedTask;
        }

        room.MafiaTargets ??= [];
        if (!room.MafiaTargets.TryAdd(playerId, 1))
            room.MafiaTargets[playerId]++;

        return Task.WhenAll(
        [
            botClient.EditMessageTextAsync(update.CallbackQuery.Message.Chat, update.CallbackQuery.Message.MessageId,
                $"Вы выбрали {roomPlayer?.User.FirstName} {roomPlayer?.User.LastName}",
                cancellationToken: token),
            botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "", cancellationToken: token)
        ]);
    }
}