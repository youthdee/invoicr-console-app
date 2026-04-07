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