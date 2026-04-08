using System.Text;

namespace Invoicr.Managers;

/// <summary>
/// Statická třída pro přímou práci s CSV soubory. 
/// Udělal jsem ji statickou, protože nechci vytvářet instanci. Třída je v paměti celou dobu a jenom jedna a ta samá,
/// Takže můžu s klidným vědomím volat její metody rovnou na třídě a ne provolávat metody/funkce objektu.
/// </summary>
public static class CsvManager
{
    //CSV separátor, typicky středník
    const char Separator = ';';

    /// <summary>
    /// Helper metoda pro escapování hodnot v jednotlivých sloupcích.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string Escape(string? text)
    {
        if (text == null)
        {
            text = string.Empty;
        }

        // Definice podmínek, kdy potřebuju text escapovat (obalit uvozovkama)
        bool containsSeparator = text.Contains(Separator);
        bool containsQuote = text.Contains('"');
        bool containsNewline = text.Contains('\n');

        //pokud platí alespoň jedna tak musím escapovat.
        if (containsSeparator || containsQuote || containsNewline)
        {
            string escapedContent = text.Replace("\"", "\"\"");
            return $"\"{escapedContent}\"";
        }

        //pokud není splněná žádná z výše uvedených podmínek tak ten text vrátím
        return text;
    }

    /// <summary>
    /// Helper funkce, která parsuje celý řádek.
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>

    //Tato funkce byla vygenerována umělou inteligencí, konkrétně GEMINI PRO a prompt byl následující:
    //„Napiš v C# metodu ParseLine, která rozparsuje řádek CSV do pole stringů s ohledem na text v uvozovkách.
    //Metoda musí ignorovat oddělovač uvnitř uvozovek a korektně zpracovat zdvojené uvozovky jako escapovaný znak.“
    public static string[] ParseLine(string line)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                // STAV: Jsme uvnitř uvozovek
                if (c == '"')
                {
                    // Koukneme na další znak - je to zdvojená uvozovka (escape)?
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++; // Přeskočíme tu druhou uvozovku
                    }
                    else
                    {
                        // Končíme režim uvozovek
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                // STAV: Jsme mimo uvozovky
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == Separator)
                {
                    // Narazili jsme na oddělovač -> uzavřít pole a začít nové
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }

        // Nesmíme zapomenout přidat poslední rozpracované pole
        fields.Add(currentField.ToString());

        return fields.ToArray();
    }

    /// <summary>
    /// Jednoduchá metoda vracející "řádek", který se pak může rovnou uložit do CSV souboru.
    /// </summary>
    /// <param name="cols"></param>
    /// <returns></returns>
    public static string Row(params string[] cols) =>
        string.Join(Separator, cols.Select(Escape));
}