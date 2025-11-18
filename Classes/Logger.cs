using Notifier;

public class Logger
{
    public struct LogType
    {
        public string Severity { get; set; }
        public ConsoleColor Color { get; set; }

        public LogType(string severity, ConsoleColor color)
        {
            Severity = severity;
            Color = color;
        }
    }

    private static string GetTimestamp() => $"[{DateTime.Now:HH:mm:ss}]";

    public static readonly LogType Critical = new LogType("Critical", ConsoleColor.DarkRed);
    public static readonly LogType Debug = new LogType("Debug", ConsoleColor.DarkBlue);
    public static readonly LogType Verbose = new LogType("Verbose", ConsoleColor.Gray);
    public static readonly LogType Info = new LogType("Info", ConsoleColor.Blue);
    public static readonly LogType Error = new LogType("Error", ConsoleColor.Red);
    public static readonly LogType Success = new LogType("Success", ConsoleColor.Green);
    public static readonly LogType Warning = new LogType("Warning", ConsoleColor.Yellow);
    public static readonly LogType Update = new LogType("Update", ConsoleColor.Magenta);
    public static readonly LogType Regular = new LogType("Regular", ConsoleColor.White);
    public static readonly LogType Ok = new LogType("OK", ConsoleColor.Green);

    public static void Print(LogType logType, string message)
    {

        string formattedLog = $"{GetTimestamp()} {logType.Severity,-12} {message,-20}";

        Console.ForegroundColor = logType.Color;
        Console.WriteLine(formattedLog);
        Console.ResetColor();

        Directory.CreateDirectory(Path.GetDirectoryName(RBX._logfile) ?? "log.txt");
        if (!File.Exists(RBX._logfile)) File.Create(RBX._logfile).Dispose();

        File.AppendAllText(RBX._logfile, formattedLog + Environment.NewLine);
    }
}
