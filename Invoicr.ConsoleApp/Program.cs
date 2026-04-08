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
 * zvetsit sazbu alespon 2x u dodavatele
 * Menu odberatele a dodavatele se liší
 * format data absolutne nefunguje
 * celkem u faktury 2x vetsi
 * PDF vystup klidne udelejme nejak na prasaka (TXT)
 */