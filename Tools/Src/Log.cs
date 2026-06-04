namespace InsanityWorldMod.Tools;

public static partial class Constants
{
    public const string LOG_TAG = "[InsanityWorld]";
}

public static partial class Funcs
{
    public static void LogInfo(string msg)  => Console.WriteLine($"{LOG_TAG} {msg}");
    public static void LogError(string msg) => Console.Error.WriteLine($"{LOG_TAG} {msg}");
}
