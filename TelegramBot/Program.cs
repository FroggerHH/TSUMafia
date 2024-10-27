using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Handlers;

namespace TelegramBot;

static class Program
{
    public static ITelegramBotClient Bot = null!;
    public static List<ICommandHandler> CommandHandlers = null!;
    public static List<IRoleHandler> RoleHandlers = null!;
    public static readonly List<GameRoom> GameRooms = [];

    private static void Main()
    {
        Logger.Debug($"Starting application");
        FindHandlers();
        Logger.Debug($"Starting Telegram Bot");
        StartBot();
        Console.ReadKey();
    }

    private static void FindHandlers()
    {
        CommandHandlers = Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => typeof(ICommandHandler).IsAssignableFrom(type))
            .Where(type => type is { IsInterface: false, IsAbstract: false })
            .Select(Activator.CreateInstance)
            .Cast<ICommandHandler>()
            .ToList();

        RoleHandlers = Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => typeof(IRoleHandler).IsAssignableFrom(type))
            .Where(type => type is { IsInterface: false, IsAbstract: false })
            .Select(Activator.CreateInstance)
            .Cast<IRoleHandler>()
            .ToList();
    }

    private static void StartBot()
    {
        Bot = new TelegramBotClient("8083563461:AAEAGza7OUjk1lW4SWcODpgVOxzp1gazdSA");
        Bot.StartReceiving<MessageHandler>();

        Bot.SetMyCommandsAsync(
        [
            new BotCommand
            {
                Command = "start",
                Description = "Restart the bot",
            },
            new BotCommand
            {
                Command = "help",
                Description = "Help about the bot",
            },
            new BotCommand
            {
                Command = "game",
                Description = "Start recruiting for a new game",
            },
            new BotCommand
            {
                Command = "stop",
                Description = "Stop current game",
            },
        ], languageCode: "en");

        Bot.SetMyCommandsAsync(
        [
            new BotCommand
            {
                Command = "start",
                Description = "Запустить бота",
            },
            new BotCommand
            {
                Command = "help",
                Description = "Справка",
            },
            new BotCommand
            {
                Command = "game",
                Description = "Начать набор в новую игру",
            },
            new BotCommand
            {
                Command = "stop",
                Description = "Остановить текущую игру",
            },
        ], languageCode: "ru");
    }
}