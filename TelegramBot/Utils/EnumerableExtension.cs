namespace TelegramBot.Utils;

public static class EnumerableExtension
{
    public static T? Random<T>(this IList<T>? list)
    {
        if (list == null || list.Count == 0) return default;
        return list[System.Random.Shared.Next(0, list.Count)];
    }

    public static T? Random<T>(this T?[]? array)
    {
        if (array == null || array.Length == 0) return default;
        return array[System.Random.Shared.Next(0, array.Length)];
    }

    public static string GetString<T>(this IEnumerable<T> list, string separator = ", ") =>
        string.Join(separator, list);
}