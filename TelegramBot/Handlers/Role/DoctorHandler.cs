using System.Diagnostics.CodeAnalysis;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Utils;

namespace TelegramBot.Handlers.Role;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class DoctorHandler : IRoleHandler
{
    public TelegramBot.Role Role => TelegramBot.Role.Doctor;
    public byte TimeLimit_sec => 10;

    public async Task HandleGameplayAsync(GameRoom room, CancellationToken token)
    {
        Logger.Call(nameof(DoctorHandler), nameof(HandleGameplayAsync));
        try
        {
            room.DoctorSave = null; // сброс предыдущего выбора
            token.ThrowIfCancellationRequested();

            await AskForSave(room);

            await WaitForSaveChoice(room, token);

            ApplySaveResult(room);
        }
        catch (OperationCanceledException)
        {
            Logger.Warning("Doctor move was canceled", nameof(DoctorHandler), nameof(HandleGameplayAsync));
        }
    }

    private async Task WaitForSaveChoice(GameRoom room, CancellationToken token)
    {
        // Ожидание ответа от Доктора
        while (room.DoctorSave == null)
        {
            await Task.Delay(200, token);
        }
    }

    private async Task AskForSave(GameRoom room)
    {
        // Получаем доктора
        var doctor = room.Players.FirstOrDefault(player => player.Role == Role);
        if (doctor == null)
        {
            Logger.Error("Doctor not found in room", nameof(DoctorHandler), nameof(AskForSave));
            return;
        }

        try
        {
            // Отправка сообщения с кнопками выбора игрока для спасения
            await Program.Bot.SendTextMessageAsync(
                doctor.User.Id,
                "Кого вы хотите спасти этой ночью?",
                replyMarkup: new InlineKeyboardMarkup(
                    room.Players.Select(player =>
                        new InlineKeyboardButton($"{player.User.FirstName} {player.User.LastName}")
                        {
                            CallbackData = $"{nameof(DoctorHandler)} doctor_save {player.User.Id} {room.Chat.Id}"
                        }).ToList()
                ),
                cancellationToken: room.Cts.Token
            );
        }
        catch (ApiRequestException)
        {
            await Program.Bot.SendTextMessageAsync(
                room.Chat,
                $"Не могу написать {doctor.User.Username} в лс. Возможно вы заблокировали бота.",
                cancellationToken: room.Cts.Token
            );
        }
    }

    private void ApplySaveResult(GameRoom room)
    {
        // Если доктор выбрал цель, то сохраняем её в свойстве room.DoctorSave
        if (room.DoctorSave != null)
        {
            Logger.Info($"Doctor chose to save player with ID: {room.DoctorSave}", nameof(DoctorHandler),
                nameof(ApplySaveResult));
        }
        else
        {
            Logger.Warning("Doctor did not choose anyone to save", nameof(DoctorHandler), nameof(ApplySaveResult));
        }
    }

    public Task HandleCallbackQueryAsync(ITelegramBotClient botClient, Update? update, CancellationToken token)
    {
        if (update?.CallbackQuery?.Data == null ||
            update.CallbackQuery.Message == null ||
            !update.CallbackQuery.Data.StartsWith(nameof(DoctorHandler))) return Task.CompletedTask;

        var split = update.CallbackQuery.Data.Split(" ");
        if (split.Length < 4)
        {
            Logger.Error("No doctor save target player ID specified", nameof(DoctorHandler),
                nameof(HandleCallbackQueryAsync));
            return Task.CompletedTask;
        }

        var chatIdStr = split[3];
        if (!long.TryParse(chatIdStr, out long chatId))
        {
            Logger.Error($"Invalid chat ID: {chatIdStr}", nameof(DoctorHandler), nameof(HandleCallbackQueryAsync));
            return Task.CompletedTask;
        }

        var room = Program.GameRooms.Find(x => x.Chat.Id == chatId);
        if (room == null)
        {
            Logger.Error("Room not found", nameof(DoctorHandler), nameof(HandleCallbackQueryAsync));
            return Task.CompletedTask;
        }

        var playerIdStr = split[2];
        if (!long.TryParse(playerIdStr, out long playerId))
        {
            Logger.Error($"Invalid player ID: {playerIdStr}", nameof(DoctorHandler), nameof(HandleCallbackQueryAsync));
            return Task.CompletedTask;
        }

        var roomPlayer = room.Players.Find(x => x.User.Id == playerId);

        if (roomPlayer is null)
        {
            Logger.Error($"roomPlayer for {playerId} not found", nameof(MafiaHandler),
                nameof(HandleCallbackQueryAsync));
            return Task.CompletedTask;
        }

        room.DoctorSave = roomPlayer;
        
        return Task.WhenAll(
            botClient.EditMessageTextAsync(
                update.CallbackQuery.Message.Chat,
                update.CallbackQuery.Message.MessageId,
                $"Вы выбрали спасти {roomPlayer?.User.FirstName} {roomPlayer?.User.LastName}",
                cancellationToken: token
            ),
            botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "", cancellationToken: token)
        );
    }
}