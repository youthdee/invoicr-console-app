using System.Globalization;
using System.Text.RegularExpressions;

namespace Invoicr.Managers;

public static class ConsoleManager
{
    /// <summary>
    /// Helper funkce, která vytvoří jednoduchý stylovaný header v konzoli.
    /// </summary>
    /// <param name="title"></param>
    public static void Header(string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  ╔══ {title.ToUpperInvariant()} ══");
        Console.ResetColor();
    }

    /// <summary>
    /// Helper funkce pro menu výběr ve formátu [{číslo}] {Popis}
    /// </summary>
    /// <param name="n"></param>
    /// <param name="label"></param>
    public static void MenuItem(int n, string label)
        => Console.WriteLine($"  [{n}] {label}");

    /// <summary>
    /// Helper funkce pro oddělení itemů v konzoli. 40x vypíše znak "-" a poté odsadí.
    /// </summary>
    public static void Separator()
        => Console.WriteLine("  " + new string('─', 40));

    /// <summary>
    /// Funkce pro přečtení volby, co uživatel zadal. Volba musí existovat a musí být notnull int. Pokud uživatel zadá řetězec /q tak se operace zruší.
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public static int? ReadChoice(int min, int max)
    {
        while (true)
        {
            Console.Write("  > ");
            var input = Console.ReadLine()?.Trim();
            
            if (input == "/q")
                return null;
            
            if (int.TryParse(input, out int choice) && choice >= min && choice <= max)
            {
                Console.Clear();
                return choice;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Zadejte číslo {min}–{max}.");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Helper funkce / "override" standardní metody Console.ReadLine.
    /// Vylepšená verze, která zobrazuje default value. (default value se aplikuje při string.Empty)
    /// Řětezec /q zruší operaci a vrátí null, s kterým se pak v programu pracuje.
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="defaultVal"></param>
    /// <returns></returns>
    public static string? ReadLine(string prompt, string? defaultVal = null)
    {
        var hint = defaultVal is not null ? $" [{defaultVal}]" : "";
        Console.Write($"  {prompt}{hint}: ");
        var val = Console.ReadLine()?.Trim();
        if (val == "/q")
            return null;
        return string.IsNullOrEmpty(val) ? (defaultVal ?? "") : val;
    }

    /// <summary>
    /// Nadstavba funkce ReadLine, která parsuje datetime.
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="defaultVal"></param>
    /// <returns></returns>
    public static DateTime? ReadDate(string prompt, DateTime? defaultVal = null)
    {
        var defStr = defaultVal?.ToString("dd.MM.yyyy");
        while (true)
        {
            var raw = ReadLine(prompt + " (dd.MM.yyyy)", defStr);

            if (raw == "/q")
                return null;

            if (DateTime.TryParseExact(raw, "dd.MM.yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                return dt;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  Špatný formát data. (dd.MM.yyyy)");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Nadstavba funkce ReadLine, která parsuje Int.
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="defaultVal"></param>
    /// <returns></returns>
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
            Console.WriteLine("  Zadejte číslo.");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Nadstavba funkce ReadLine, která parsuje Decimal
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="defaultVal"></param>
    /// <returns></returns>
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
            Console.WriteLine("  Zadejte číslo (desetinná tečka).");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Nadstavba funkce ReadLine, která parsuje Long.
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="defaultVal"></param>
    /// <returns></returns>
    public static long? ReadLong(string prompt, decimal? defaultVal = null)
    {
        var defStr = defaultVal?.ToString(CultureInfo.InvariantCulture);
        while (true)
        {
            var raw = ReadLine(prompt, defStr);

            if (raw == "/q")
                return null;

            if (long.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  Zadejte číslo.");
            Console.ResetColor();
        }
    }

    public static string? ReadEmail(string prompt, string? defaultVal = null)
    {
        var defStr = defaultVal?.ToString(CultureInfo.InvariantCulture);
        while (true)
        {
            var raw = ReadLine(prompt, defStr);

            if (raw == "/q")
                return null;

            if (ValidateEmail(raw ?? ""))
                return raw;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  Zadejte platný email..");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Helper funkce pro vypsání success zprávy.
    /// </summary>
    /// <param name="msg"></param>
    public static void Success(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ {msg}");
        Console.ResetColor();
    }

    /// <summary>
    /// Helper funkce pro vypsání chybové hlášky.
    /// </summary>
    /// <param name="msg"></param>
    public static void Error(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ✗ {msg}");
        Console.ResetColor();
    }

    /// <summary>
    /// Helper funkce pro vypsání informační zprávy.
    /// </summary>
    /// <param name="msg"></param>
    public static void Info(string msg)
        => Console.WriteLine($"  {msg}");

    /// <summary>
    /// Privátní funkce pro vypsání záhlaví tabulky.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="columns"></param>
    /// <param name="width"></param>
    private static void PrintTableHeader(string title, string columns, int width)
    {
        Console.WriteLine($"\n>>> {title.ToUpper()} <<<");
        Console.WriteLine(new string('-', width));
        Console.WriteLine(columns);
        Console.WriteLine(new string('-', width));
    }

    /// <summary>
    /// Funkce pro vypsání kompletní tabulky.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="items"></param>
    /// <param name="header"></param>
    /// <param name="width"></param>
    /// <typeparam name="T"></typeparam>
    public static void PrintTable<T>(string title, List<T> items, string header, int width)
    {
        PrintTableHeader(title, header, width);
        foreach (var item in items)
        {
            Console.WriteLine(item?.ToString());
        }

        Console.WriteLine(new string('-', width));
    }

    //Jednoduchá regex funkce, která bez výstupu zvaliduje email (Platný / neplatný).
    //Byla vytvořena AI
    //prompt: pomocí inline regexu dodělej tuhle funkci {název funkce a parameter} (GEMINI PRO)
    private static bool ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        // Inline regex: 
        // ^[^@\s]+     -> začátek, cokoli kromě zavináče a mezer
        // @            -> musí tam být zavináč
        // [^@\s]+      -> doména (cokoli kromě @ a mezer)
        // \.           -> musí tam být tečka
        // [^@\s]+$     -> koncovka a konec řetězce
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
    }
}