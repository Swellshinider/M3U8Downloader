
namespace MD.Infra;

/// <summary>
/// Utility class
/// </summary>
public static class Util
{
    /// <summary>
    /// Check if string is null or empty or whitespace
    /// </summary>
    public static bool IsNull(this string? str) => string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str);

    /// <summary>
    /// Write with color
    /// </summary>
    public static void Write(this string? str, ConsoleColor color = ConsoleColor.White)
    {
        Console.ForegroundColor = color;
        Console.Write(str);
        Console.ResetColor();
    }

    /// <summary>
    /// Write line with color
    /// </summary>
    public static void WriteLine(this string? str, ConsoleColor color = ConsoleColor.White)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(str);
        Console.ResetColor();
    }

    /// <summary>
    /// Confirm user action
    /// </summary>
    public static bool ConfirmUserAction(string message = "Are you sure you want to continue?")
    {
        $"{message} (Y/n): ".Write();
        var input = (Console.ReadLine() ?? "").Trim().ToLower();

        if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input))
            return true;

        return input == "y";
    }
}