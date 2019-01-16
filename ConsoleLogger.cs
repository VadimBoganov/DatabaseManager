using System;

public enum LogStatus
{
    Info,
    Error,
    Debug
}

public static class ConsoleLogger
{
    public static void Write(LogStatus status, string message)
    {
        switch (status)
        {
            case LogStatus.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case LogStatus.Info:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(message);
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case LogStatus.Debug:
                Console.WriteLine(message);
                break;
            default:
                Console.WriteLine(message);
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
        }
    }
}