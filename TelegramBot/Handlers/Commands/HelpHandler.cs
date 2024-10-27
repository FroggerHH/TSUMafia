using System.Diagnostics.CodeAnalysis;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot.Handlers.Commands;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class HelpHandler : ICommandHandler
{
    public string Name => "help";

    public Task HandleCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken token)
    {
        Logger.Call(nameof(HelpHandler), nameof(HandleCommandAsync));

        return botClient.SendTextMessageAsync(message.Chat.Id, "Слишком поздно, уже ничто тебе не поможет",
            cancellationToken: token);
    }
}