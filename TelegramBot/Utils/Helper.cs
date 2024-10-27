namespace TelegramBot.Utils;

public static class Helper
{
    public static readonly Role[] EvilRoles = Enum.GetValues<Role>().Where(x => (byte)x >= 128).ToArray();
}