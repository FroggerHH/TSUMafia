using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace TelegramBot;

public interface IRoleHandler
{
    Role Role { get; }
    byte TimeLimit_sec { get; }

    Task HandleGameplayAsync(GameRoom room, CancellationToken token);
    Task HandleCallbackQueryAsync(ITelegramBotClient _, Update update, CancellationToken token);
}