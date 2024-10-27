using System.Diagnostics.CodeAnalysis;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot.Handlers.Commands;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class StopGameHandler : ICommandHandler
{
    public string Name => "stop";

    public async Task HandleCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken token)
    {
        Logger.Call(nameof(HelpHandler), nameof(HandleCommandAsync));
        if (message.Chat.Type == ChatType.Private)
        {
            await botClient.SendTextMessageAsync(message.Chat,
                "Самому с обой играть не в кайф, попробуй в группе с друзьями",
                cancellationToken: token);
            return;
        }

        var gameRoom = Program.GameRooms.Find(x => x.Chat.Id == message.Chat.Id);
        if (gameRoom is null)
        {
            Logger.Warning($"No game room to stop in chat {message.Chat.Id}{message.Chat.Title}",
                nameof(StopGameHandler), nameof(HandleCommandAsync));

            await botClient.SendTextMessageAsync(message.Chat, "А останавливать нечего...", cancellationToken: token);
            return;
        }

        gameRoom.Cts.Cancel();
        Program.GameRooms.Remove(gameRoom);

        try
        {
            await Task.WhenAll(
                botClient.DeleteMessageAsync(gameRoom.JoinMessage.Chat.Id, gameRoom.JoinMessage.MessageId,
                    cancellationToken: token),
                botClient.SendTextMessageAsync(message.Chat, "Игра остановлена", cancellationToken: token)
            );
        }
        catch (Exception e)
        {
            Logger.Exception(e);
        }
    }
}