using System.ComponentModel;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Handlers.GameRoomStates;
using TelegramBot.Utils;
using static TelegramBot.Role;

namespace TelegramBot;

public class GameRoom(Chat chat)
{
    public readonly Chat Chat = chat;
    public byte CurrentDay = 1;

    public GameRoomState State { get; private set; } = GameRoomState.WaitingForPlayersToJoin;

    public void SetState(GameRoomState value)
    {
        Logger.Debug($"State {State} -> {value}");
        State = value;
        ProcessState();
    }

    public readonly List<User> AwaitingPlayers = [];
    public readonly List<RoomPlayer> Players = [];
    public Message JoinMessage;

    public static readonly int TimeToJoin = 6;
    public static readonly int MinPlayers = 1;
    public readonly CancellationTokenSource Cts = new();

    public Dictionary<long, byte>? MafiaTargets = null;
    public readonly List<RoomPlayer> KilledThisNight = [];
    public RoomPlayer? DoctorSave;

    public async void StartGame()
    {
        Logger.Info($"Game starts in chat {Chat.Id}{Chat.Title}");

        await GreatAboutGameStart();

        await GivePlayersRandomRoles();

        await Task.Delay(2000);

        SetState(GameRoomState.NightStarts);
    }

    private async void CloseRoom()
    {
        await Cts.CancelAsync();
        Cts.Dispose();
    }

    private Task<Message> GreatAboutGameStart() => Program.Bot.SendTextMessageAsync(Chat, "Игра начинается",
        replyMarkup: new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
        {
            new("Перейти к боту")
            {
                CallbackData = "go to bot",
                Url = "https://t.me/tsuMafiaBot"
            },
        }));

    private async Task GivePlayersRandomRoles()
    {
        var rolesPool = GetRolesPool(AwaitingPlayers.Count);
        foreach (var user in AwaitingPlayers)
        {
            var random = rolesPool.Random();
            rolesPool.Remove(random);
            Players.Add(new RoomPlayer(user, random));
        }

        AwaitingPlayers.Clear();

        await Task.WhenAll(Players
            .Select(x => (User: x.User, Role: x.Role.GetLocalized()))
            .Select(async x =>
            {
                try
                {
                    return await Program.Bot.SendTextMessageAsync(x.User.Id, $"Эй, сегодня ты {x.Role}");
                }
                catch (ApiRequestException e)
                {
                    return await Program.Bot.SendTextMessageAsync(Chat,
                        $"Не могу написать {x.User.Username} в лс. Возможно ты меня заблокировал 😒");
                }
            }));
    }

    private List<Role> GetRolesPool(int playersCount) => playersCount switch
    {
        0 or 1 => [],
        // 2 => [Mafia, Normal],
        2 => [Mafia, Doctor],
        3 => [Mafia, Normal, Doctor],
        4 => [Mafia, Normal, Normal, Normal],
        5 => [Mafia, Mafia, Normal, Normal, Normal],
        6 => [Mafia, Mafia, Normal, Normal, Normal, Normal],
        7 => [Mafia, Mafia, Mafia, Normal, Normal, Normal, Normal],
        >= 8 => [Mafia, Mafia, Mafia, Normal, Normal, Normal, Normal, Normal],
        _ => throw new ArgumentOutOfRangeException(nameof(playersCount), playersCount, "Incorrect number of players")
    };

    private void ProcessState()
    {
        Logger.Call(nameof(GameRoom), nameof(ProcessState));
        switch (State)
        {
            case GameRoomState.WaitingForPlayersToJoin: throw new ArgumentException();
            case GameRoomState.DayDiscussion:
                DayDiscussion.Process(this);
                break;
            case GameRoomState.DayVoting: throw new NotImplementedException();
            case GameRoomState.Day_VotingResults: throw new NotImplementedException();

            case GameRoomState.AnnounceNightResults:
                AnnounceNightResults.SendMessage(this);
                break;

            case GameRoomState.NightStarts:
                NightStarts.SendMessage(this);
                var badguys = new WakeupBadguys(this);
                var goodguys = new WakeupGoodguys(this);
                badguys.Process();
                goodguys.Process();

                Task.Run(async () =>
                {
                    while (!badguys.IsFinished || !goodguys.IsFinished) await Task.Delay(500);

                    SetState(GameRoomState.AnnounceNightResults);
                });

                break;

            default: throw new ArgumentOutOfRangeException(nameof(State), State, "Incorrect state");
        }
    }


    public async void StartWaitingForPlayersToJoin()
    {
        CancellationToken token = Cts.Token;
        try
        {
            token.ThrowIfCancellationRequested();
            for (int i = 0; i < TimeToJoin; i += 2)
            {
                await Task.Delay(2_000, token);
                token.ThrowIfCancellationRequested();
                UpdateTimer(TimeToJoin - i, token);
            }

            await Program.Bot.DeleteMessageAsync(new ChatId(Chat.Id), JoinMessage.MessageId,
                cancellationToken: token);

            if (AwaitingPlayers.Count < MinPlayers)
            {
                await Program.Bot.SendTextMessageAsync(new ChatId(Chat.Id),
                    $"Недостаточно игроков, минимум {MinPlayers}",
                    cancellationToken: token);
                Program.GameRooms.Remove(this);
                CloseRoom();
                return;
            }

            StartGame();
        }
        catch (OperationCanceledException)
        {
            Cts.Dispose();
        }
    }

    private void UpdateTimer(int seconds, CancellationToken token)
    {
        lock (JoinMessage.Text)
        {
            var text = Regex.Replace(JoinMessage.Text, @"Осталось \d+ секунд",
                $"Осталось {seconds} секунд");

            Program.Bot.EditMessageTextAsync(new ChatId(Chat.Id), JoinMessage.MessageId, text,
                parseMode: ParseMode.Markdown, disableWebPagePreview: true,
                replyMarkup: new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
                {
                    new("Присоединится") { CallbackData = "join" },
                }),
                cancellationToken: token);
            JoinMessage.Text = text;
        }
    }
}

public class RoomPlayer(User user, Role role)
{
    public readonly User User = user;
    public readonly Role Role = role;
    public bool IsAlive = true;
}

public enum GameRoomState
{
    WaitingForPlayersToJoin = 0,

    AnnounceNightResults,
    DayDiscussion,
    DayVoting,
    Day_VotingResults,

    NightStarts,
}