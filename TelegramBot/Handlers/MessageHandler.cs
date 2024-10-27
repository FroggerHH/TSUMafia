using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Utils;

namespace TelegramBot.Handlers;

public class MessageHandler : IUpdateHandler
{
    public Task HandleUpdateAsync(ITelegramBotClient botClient, Update? update, CancellationToken token)
    {
        try
        {
            if (update is null) return Task.CompletedTask;
            Logger.TraceCall(update.GetTraceFields(), nameof(MessageHandler), nameof(HandleUpdateAsync));

            if (HandleCallbackQueryAsync(botClient, update, token, out var handleUpdateAsync))
                return handleUpdateAsync;

            if (HandleCommands(botClient, update, token, out var handleCommandAsync))
                return handleCommandAsync;

            // Simple message

            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            Logger.Exception(e);
            return Task.CompletedTask;
        }
    }

    private bool HandleCallbackQueryAsync(ITelegramBotClient botClient, Update update, CancellationToken token,
        out Task handleUpdateAsync)
    {
        handleUpdateAsync = Task.CompletedTask;
        if (update.CallbackQuery is not null)
        {
            foreach (var handler in Program.RoleHandlers)
            {
                if (update.CallbackQuery?.Data is null || update.CallbackQuery.Message is null ||
                    update.CallbackQuery.Data.Length <= 0 ||
                    !update.CallbackQuery.Data.StartsWith(handler.GetType().Name)) continue;

                Logger.TraceCall(new()
                {
                    { "chat_id", update.CallbackQuery.Message.Chat.Id },
                    { "message", update.CallbackQuery.Message.Text },
                    { "query_data", update.CallbackQuery.Data },
                }, nameof(MessageHandler), nameof(HandleCallbackQueryAsync));

                handler.HandleCallbackQueryAsync(botClient, update, token);
            }

            handleUpdateAsync = HandleJoinGameQuery(botClient, update, token);
            return true;
        }

        return false;
    }

    private static bool HandleCommands(ITelegramBotClient botClient, Update update, CancellationToken token,
        out Task handleCommandAsync)
    {
        handleCommandAsync = Task.CompletedTask;
        if (update.Type == UpdateType.Message && update.Message?.Text?.StartsWith('/') == true)
        {
            update.Message.Text = update.Message.Text.Replace("@tsuMafiaBot", "");
            var commandName = update.Message.Text[1..];
            var handler = Program.CommandHandlers.Find(x => x.Name == commandName);
            if (handler is null)
            {
                handleCommandAsync = botClient.SendTextMessageAsync(update.Message.Chat,
                    $"Команда `{commandName}` не существует", cancellationToken: token,
                    parseMode: ParseMode.Markdown);
                return true;
            }

            handleCommandAsync = handler.HandleCommandAsync(botClient, update.Message, token);
            return true;
        }

        return false;
    }

    private Task HandleJoinGameQuery(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        if (update.CallbackQuery?.Data != "join") return Task.CompletedTask;
        var gameRoom = Program.GameRooms
            .Find(x =>
                x.JoinMessage is not null &&
                x.Chat.Id == update.CallbackQuery.Message!.Chat.Id &&
                x.JoinMessage.MessageId == update.CallbackQuery.Message!.MessageId
            );
        if (gameRoom is null)
        {
            Logger.Error($"Game room for query not found", nameof(MessageHandler), nameof(HandleJoinGameQuery));
            botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Игра больше недействительна", true,
                cancellationToken: token);
            return Task.CompletedTask;
        }

        if (gameRoom.AwaitingPlayers.Any(x => x.Id == update.CallbackQuery.From.Id))
        {
            Logger.Debug("Player already joined", nameof(MessageHandler), nameof(HandleJoinGameQuery));
            botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Вы уже в игре ;)", true,
                cancellationToken: token);
            return Task.CompletedTask;
        }

        gameRoom.AwaitingPlayers.Add(update.CallbackQuery.From);
        Logger.Info($"Player joined room in chat {gameRoom.Chat.Id}{gameRoom.Chat.Title}", nameof(MessageHandler),
            nameof(HandleUpdateAsync));
        lock (gameRoom.JoinMessage.Text)
        {
            var sb = new StringBuilder();
            var replace =
                new Regex("секунд.*", RegexOptions.Singleline).Replace(gameRoom.JoinMessage.Text, "секунд");
            sb.AppendLine(replace);
            sb.AppendLine($"\nЗарегистрировались " +
                          $"{(gameRoom.AwaitingPlayers.Count == 0 ? "" : gameRoom.AwaitingPlayers.Count)}:");
            foreach (var player in gameRoom.AwaitingPlayers)
                sb.AppendLine($"[{player.FirstName}](https://t.me/{player.Username})");

            botClient.EditMessageTextAsync(new ChatId(gameRoom.Chat.Id), gameRoom.JoinMessage.MessageId,
                sb.ToString(), parseMode: ParseMode.Markdown,
                disableWebPagePreview: true,
                replyMarkup: new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
                {
                    new("Присоединится") { CallbackData = "join" },
                }),
                cancellationToken: token);
            gameRoom.JoinMessage.Text = sb.ToString();
            botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "", cancellationToken: token);
        }

        return Task.CompletedTask;
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception? exception, CancellationToken token)
    {
        Logger.Error($"Exception={exception?.Message ?? "null"}", nameof(MessageHandler),
            nameof(HandlePollingErrorAsync));

        return Task.CompletedTask;
    }
}