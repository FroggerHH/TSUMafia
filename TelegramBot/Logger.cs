using System.Globalization;
using System.Text;

public static class Logger
{
    private static readonly string FilePath;

    private static readonly object Lock = new();

    static Logger()
    {
        var thread = Thread.CurrentThread;
        thread.CurrentCulture = thread.CurrentUICulture = CultureInfo.InvariantCulture;

        // var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        if (Directory.Exists(logsDir) == false) Directory.CreateDirectory(logsDir);

        FilePath = Path.Combine(logsDir, "Log.log");

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }
        catch
        {
            // ignored
        }

        WriteLine($"Log file: {FilePath}");
    }

    #region PublicMethods

    public static void Debug(string? message, string? type = null, string? method = null) =>
        Log(message, LogLevel.Debug, type, method);

    public static void Call(string type, string? method = null) => Log(string.Empty, LogLevel.Call, type, method);

    public static void TraceCall(Dictionary<string, object?>? inputs, string? type = null, string? method = null) =>
        Log(MergeInputMessages(inputs), LogLevel.Trace, type, method);

    public static void Info(string? message, string? type = null, string? method = null) =>
        Log(message, LogLevel.Info, type, method);

    public static void Warning(string? message, string? type = null, string? method = null) =>
        Log(message, LogLevel.Warning, type, method);

    public static void Error(string? message, string? type = null, string? method = null) =>
        Log(message, LogLevel.Error, type, method);

    public static void Exception(Exception exception) =>
        Error(exception.Message + Environment.NewLine + exception.StackTrace);
    // throw new NotImplementedException("void Logger.Exception is not implemented");

    private static void AddLogHeader(ref string result, LogLevel level, string? type = null, string? method = null)
    {
        var levelAndTime = $"[{level} at {DateTime.Now:d.M.yyyy HH:mm:ss}]";
        var message = result.Length > 0 ? $"\n\t{result}" : string.Empty;
        var stack = type is not null && method is not null ? $" {type}.{method}" : string.Empty;
        result = levelAndTime + stack + message;
    }

    public static void Log(string? message, LogLevel level, string? type = null, string? method = null)
    {
        message ??= "null";
        AddLogHeader(ref message, level, type, method);
        WriteLine(message);
    }

    #endregion

    #region PrivateMethods

    private static void Clear()
    {
        lock (Lock)
        {
            using var writer1 = new StreamWriter(FilePath, false);
            writer1.Write(string.Empty);
        }
    }

    private static void Write(string message)
    {
        lock (Lock)
        {
            using var writer = new StreamWriter(FilePath, true);
            writer.Write(message);
            Console.Write(message);
        }
    }

    private static void WriteLine(string message)
    {
        lock (Lock)
        {
            using var writer = new StreamWriter(FilePath, append: true, encoding: Encoding.UTF8);
            writer.WriteLine(message);
            Console.WriteLine(message);
        }
    }

    private static string? MergeInputMessages(Dictionary<string, object?>? input, bool showTypes = false)
    {
        var sb = new StringBuilder();
        if (input is null) return null;

        for (var i = 0; i < input.Count; i++)
        {
            var (name, value) = input.ElementAt(i);
            var type = value?.GetType().Name;
            if (value == null) sb.Append($"\n\t{i}. {name}=null");
            else
            {
                sb.Append($"\n\t{i}. {name}={value}");
                if (showTypes) sb.Append($"({type})");
            }
        }

        return sb.ToString().Substring(2, sb.Length - 2);
    }

    #endregion
}

public enum LogLevel
{
    Unknown = -1,

    Call = 1,
    Trace = 2,
    Debug = 3,
    Info = 4,
    Warning = 5,
    Error = 6,
    Exception = 7,
}