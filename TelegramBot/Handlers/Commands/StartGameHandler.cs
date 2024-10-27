using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot.Handlers.Commands;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class StartGameHandler : ICommandHandler
{
    public string Name => "game";

    public async Task HandleCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken token)
    {
        Logger.Call(nameof(StartGameHandler), nameof(HandleCommandAsync));
        if (message.Chat.Type == ChatType.Private)
        {
            await botClient.SendTextMessageAsync(message.Chat,
                "Самому с обой играть не в кайф, попробуй в группе с друзьями",
                cancellationToken: token);
            return;
        }

        var gameRoom = Program.GameRooms.Find(x => x.Chat.Id == message.Chat.Id);
        if (gameRoom is not null)
        {
            var reply = gameRoom.State == GameRoomState.WaitingForPlayersToJoin
                ? "Уже идёт набор в игру"
                : "Дождись окончания текущей игры";

            await botClient.SendTextMessageAsync(message.Chat, reply,
                cancellationToken: token);
            return;
        }


        var joinMessage = await botClient.SendTextMessageAsync(message.Chat,
            $"Ведётся набор в игру\nОсталось {GameRoom.TimeToJoin} секунд",
            replyMarkup: new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
            {
                new("Присоединится") { CallbackData = "join" },
            }),
            cancellationToken: token);
        gameRoom = new GameRoom(message.Chat);
        Program.GameRooms.Add(gameRoom);
        gameRoom.JoinMessage = joinMessage;

        gameRoom.StartWaitingForPlayersToJoin(token);
    }
}