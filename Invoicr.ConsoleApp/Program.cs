using System.Globalization;
using System.Text;
using System.Text.Json;
using Invoicr.ConsoleApp;

//entry point projektu. Trochu modulární architektura.
class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        new App().Run();
    }
}

/*
 * TODO:
 * email validace
 * na macu nefunguje klavesnice??
 * zvetsit sazbu alespon 2x u dodavatele
 * Menu odberatele a dodavatele se liší
 * celkem u faktury 2x vetsi
 * Vylepšit zadávání u Enum a ANO/NE
 */