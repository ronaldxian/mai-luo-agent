namespace OcCli.Commands;

public static class AnsiConsole
{
    private const string InfoColor = "\u001b[36m";
    private const string DoneColor = "\u001b[32m";
    private const string WarnColor = "\u001b[33m";
    private const string ErrorColor = "\u001b[31m";
    private const string Reset = "\u001b[0m";

    public static void Info(string template, params object[] args)
    {
        var message = string.Format(template, args);
        Console.WriteLine($"{InfoColor}[INFO]{Reset} {message}");
    }

    public static void Done(string template, params object[] args)
    {
        var message = string.Format(template, args);
        Console.WriteLine($"{DoneColor}[DONE]{Reset} {message}");
    }

    public static void Warn(string template, params object[] args)
    {
        var message = string.Format(template, args);
        Console.WriteLine($"{WarnColor}[WARN]{Reset} {message}");
    }

    public static void Error(string template, params object[] args)
    {
        var message = string.Format(template, args);
        Console.WriteLine($"{ErrorColor}[ERROR]{Reset} {message}");
    }
}
