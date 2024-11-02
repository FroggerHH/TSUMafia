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

    private static void Main(string[] args)
    {
        Logger.Debug($"Starting application");
        FindHandlers();
        Logger.Debug($"Starting Telegram Bot");
        if (args.Length < 1)
        {
            Logger.Error("No bot token specified in application arguments");
            throw new Exception("No bot token specified in application arguments");
        }

        StartBot(args[0]);
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

    private static void StartBot(string botToken)
    {
        Bot = new TelegramBotClient(botToken);
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