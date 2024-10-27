using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot;

public interface ICommandHandler
{
    string Name { get; }

    Task HandleCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);
}