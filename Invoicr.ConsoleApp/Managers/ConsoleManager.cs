using System.Globalization;

namespace Invoicr.Managers;

public static class ConsoleManager
{
    public static void Header(string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  ╔══ {title.ToUpperInvariant()} ══");
        Console.ResetColor();
    }

    public static void MenuItem(int n, string label)
        => Console.WriteLine($"  [{n}] {label}");

    public static void Separator()
        => Console.WriteLine("  " + new string('─', 40));

    public static int ReadChoice(int min, int max)
    {
        while (true)
        {
            Console.Write("  > ");
            var input = Console.ReadLine()?.Trim();
            if (int.TryParse(input, out int choice) && choice >= min && choice <= max)
            {
                Console.Clear();
                return choice;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Zadej číslo {min}–{max}.");
            Console.ResetColor();
        }
    }

    public static string? ReadLine(string prompt, string? defaultVal = null)
    {
        var hint = defaultVal is not null ? $" [{defaultVal}]" : "";
        Console.Write($"  {prompt}{hint}: ");
        var val = Console.ReadLine()?.Trim();
        if (val == "/q")
            return null;
        return string.IsNullOrEmpty(val) ? (defaultVal ?? "") : val;
    }

    public static DateTime? ReadDate(string prompt, DateTime? defaultVal = null)
    {
        var defStr = defaultVal?.ToString("yyyy-MM-dd");
        while (true)
        {
            var raw = ReadLine(prompt + " (yyyy-MM-dd)", defStr);

            if (raw == "/q")
                return null;

            if (DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                return dt;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  Špatný formát data.");
            Console.ResetColor();
        }
    }

    public static int? ReadInt(string prompt, int? defaultVal = null)
    {
        var defStr = defaultVal?.ToString(CultureInfo.InvariantCulture);
        while (true)
        {
            var raw = ReadLine(prompt, defStr);

            if (raw == "/q")
                return null;

            if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  Zadej číslo.");
            Console.ResetColor();
        }
    }

    public static decimal? ReadDecimal(string prompt, decimal? defaultVal = null)
    {
        var defStr = defaultVal?.ToString(CultureInfo.InvariantCulture);
        while (true)
        {
            var raw = ReadLine(prompt, defStr);

            if (raw == "/q")
                return null;

            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  Zadej číslo (desetinná tečka).");
            Console.ResetColor();
        }
    }

    public static void Success(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ {msg}");
        Console.ResetColor();
    }

    public static void Error(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ✗ {msg}");
        Console.ResetColor();
    }

    public static void Info(string msg)
        => Console.WriteLine($"  {msg}");
}