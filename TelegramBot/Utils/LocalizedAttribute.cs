namespace TelegramBot.Utils;

public class LocalizedAttribute(string russian) : Attribute
{
    public string Russian = russian;
}