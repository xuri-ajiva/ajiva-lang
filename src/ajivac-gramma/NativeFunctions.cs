namespace ajivac_lib;

public static class NativeFunctions
{
    public static void Log(int message) => LogCore(message.ToString());
    public static void Log(string message) => LogCore(message);
    private static void LogCore(object message)
    {
        Console.WriteLine("Log: " + message);
    }
    
    public static int Add(int a, int b) => a + b;
}
