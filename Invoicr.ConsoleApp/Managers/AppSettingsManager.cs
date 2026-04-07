using System.Text;
using System.Text.Json;
using Invoicr.Objects.AppSettings;

namespace Invoicr.Managers;

//jednoduchý správce, pro nastavování aplikace
public class AppSettingsManager
{
    readonly string _path;
    public AppSettings Settings { get; private set; }

    public AppSettingsManager(string path)
    {
        _path = path;
        Settings = Load();
    }

    AppSettings Load()
    {
        if (File.Exists(_path))
        {
            var json = File.ReadAllText(_path, Encoding.UTF8);
            return JsonSerializer.Deserialize<AppSettings>(json,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? Default();
        }

        var def = Default();
        Save(def);
        return def;
    }

    static AppSettings Default() => new()
    {
        CsvFolder = "data",
        InvoicePrefix = "FAK",
        InvoiceStartNumber = 1,
        InvoiceStep = 1,
        PdfOutputFolder = "output/pdf"
    };
    //new("FAK", 1, 1, "output/pdf", "data");

    public void Save(AppSettings s)
    {
        Settings = s;
        var json = JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json, Encoding.UTF8);
    }

    public string NextInvoiceNumber()
    {
        // find highest existing suffix and increment by step
        // caller is responsible for persisting after use
        return $"{Settings.InvoicePrefix}{Settings.InvoiceStartNumber:D4}";
    }

    public void BumpInvoiceNumber()
    {
        Settings.InvoiceStartNumber += Settings.InvoiceStartNumber;
        Save(Settings);
    }
}