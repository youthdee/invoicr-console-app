using System.Text.Json;
using Invoicr.ConsoleApp.Repositories;
using Invoicr.Managers;
using Invoicr.Objects;
using Invoicr.Objects.AppSettings;
using Invoicr.Repositories;

namespace Invoicr.ConsoleApp;

public class App
{
    private readonly AppSettingsManager appSettingsManager;
    private readonly SupplierRepository suplierRepository;
    private readonly ClientRepository clientRepository;
    private readonly InvoiceRepository invoiceRepository;

    private readonly AppSettings appSettings;

    public App()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string filePath = Path.Combine(baseDir, "appsettings.json");

        if (File.Exists(filePath))
        {
            string jsonString = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            appSettings = JsonSerializer.Deserialize<AppSettings>(jsonString, options)!;
        }
        else
        {
            Console.WriteLine("Chyba: Soubor config.json nebyl nalezen!");
        }

        //inicializace repozitářů
        suplierRepository = new SupplierRepository();
        clientRepository = new ClientRepository();
        invoiceRepository = new InvoiceRepository();
    }

    public void Run()
    {
        while (true)
        {
            ConsoleManager.Header("Fakturační systém");
            ConsoleManager.MenuItem(1, "Vytvořit novou fakturu");
            ConsoleManager.MenuItem(2, "Upravit seznam dodavatelů");
            ConsoleManager.MenuItem(3, "Upravit seznam odběratelů");
            ConsoleManager.MenuItem(4, "Zobrazit a generovat faktury");
            ConsoleManager.Separator();
            ConsoleManager.MenuItem(0, "Konec");

            int choice = ConsoleManager.ReadChoice(0, 4);
            switch (choice)
            {
                case 1: MenuInvoice(); break;
                case 2: MenuSuppliers(); break;
                case 3: MenuClients(); break;
                case 4: ShowOrGenerateInvoices(); break;
                case 0: return;
            }
        }
    }

    void MenuInvoice()
    {
        while (true)
        {
            ConsoleManager.Header("Faktury");
            ConsoleManager.MenuItem(1, "Vytvořit novou fakturu");
            ConsoleManager.MenuItem(2, "Zobrazit seznam faktur");
            ConsoleManager.Separator();
            ConsoleManager.MenuItem(0, "Zpět");

            int choice = ConsoleManager.ReadChoice(0, 2);
            switch (choice)
            {
                case 1: CreateInvoice(); break;
                case 2: ListInvoices(); break;
                case 0: return;
            }
        }
    }

    void CreateInvoice()
    {
        ConsoleManager.Header("Nová faktura");

        if (suplierRepository.Items.Count == 0)
        {
            ConsoleManager.Error("Nejprve přidej dodavatele.");
            return;
        }

        if (clientRepository.Items.Count == 0)
        {
            ConsoleManager.Error("Nejprve přidej odběratele.");
            return;
        }

        // Supplier
        ConsoleManager.Info("Dodavatelé:");
        for (int i = 0; i < suplierRepository.Items.Count; i++)
            ConsoleManager.Info(
                $"  [{i + 1}] {suplierRepository.Items[i].Name} (IČO: {suplierRepository.Items[i].ICO})");
        ConsoleManager.Info($"  Vyber 1–{suplierRepository.Items.Count}:");
        int si = ConsoleManager.ReadChoice(1, suplierRepository.Items.Count) - 1;
        var supplier = suplierRepository.Items[si];

        // Client
        ConsoleManager.Info("Odběratelé:");
        for (int i = 0; i < clientRepository.Items.Count; i++)
            ConsoleManager.Info($"  [{i + 1}] {clientRepository.Items[i].Name} (IČO: {clientRepository.Items[i].ICO})");
        ConsoleManager.Info($"  Vyber 1–{clientRepository.Items.Count}:");
        int ci = ConsoleManager.ReadChoice(1, clientRepository.Items.Count) - 1;
        var client = clientRepository.Items[ci];

        string number = appSettingsManager.NextInvoiceNumber();
        ConsoleManager.Info($"Číslo faktury: {number}");

        var issueDate = ConsoleManager.ReadDate("Datum vystavení", DateTime.Today);
        var dueDate = ConsoleManager.ReadDate("Datum splatnosti", DateTime.Today.AddDays(14));
        var hours = ConsoleManager.ReadDecimal("Odpracované hodiny");
        var rate = ConsoleManager.ReadDecimal("Hodinová sazba");
        var currency = ConsoleManager.ReadLine("Měna", "CZK");
        var note = ConsoleManager.ReadLine("Poznámka", "");

        var invoice = new Invoice();
        //number, supplier.Id, client.Id, issueDate, dueDate, hours, rate, currency, note
        invoiceRepository.Items.Add(invoice);
        invoiceRepository.Save();
        appSettingsManager.BumpInvoiceNumber();

        ConsoleManager.Success($"Faktura {number} uložena. Celkem: {hours * rate:N2} {currency}");
        ConsoleManager.Info($"PDF výstup bude v: {appSettingsManager.Settings.PdfOutputFolder}");
    }

    void ListInvoices()
    {
        ConsoleManager.Header("Seznam faktur");
        if (invoiceRepository.Items.Count == 0)
        {
            ConsoleManager.Info("Žádné faktury.");
            return;
        }

        Console.WriteLine($"  {"Číslo",-12} {"Dodavatel",-20} {"Odběratel",-20} {"Datum",-12} {"Celkem",10}");
        ConsoleManager.Separator();
        foreach (var inv in invoiceRepository.Items)
        {
            var sup = suplierRepository.Get(inv.SupplierId)?.Name ?? inv.SupplierId.ToString();
            var cli = clientRepository.Get(inv.ClientId)?.Name ?? inv.ClientId.ToString();
            var total = inv.HoursWorked * inv.HourRate;
            Console.WriteLine(
                $"  {inv.Number,-12} {sup,-20} {cli,-20} {inv.IssueDate:yyyy-MM-dd}  {total,8:N2} {inv.Currency}");
        }
    }

    // ── SUPPLIERS ─────────────────────────────

    void MenuSuppliers()
    {
        while (true)
        {
            ConsoleManager.Header("Dodavatelé");
            ConsoleManager.MenuItem(1, "Přidat dodavatele");
            ConsoleManager.MenuItem(2, "Zobrazit seznam");
            ConsoleManager.MenuItem(3, "Upravit dodavatele");
            ConsoleManager.MenuItem(4, "Smazat dodavatele");
            ConsoleManager.Separator();
            ConsoleManager.MenuItem(0, "Zpět");

            int choice = ConsoleManager.ReadChoice(0, 4);
            switch (choice)
            {
                case 1: AddSupplier(); break;
                case 2: ListSuppliers(); break;
                case 3: EditSupplier(); break;
                case 4: DeleteSupplier(); break;
                case 0: return;
            }
        }
    }

    void AddSupplier()
    {
        ConsoleManager.Header("Nový dodavatel");
        var id = suplierRepository.NextId();
        string? name = ConsoleManager.ReadLine("Název");

        if (string.IsNullOrEmpty(name))
            return;

        ConsoleManager.Success($"Dodavatel '{name}' uložen (ID {id}).");
    }

    void ListSuppliers()
    {
        ConsoleManager.Header("Seznam dodavatelů");
        if (suplierRepository.Items.Count == 0)
        {
            ConsoleManager.Info("Žádní dodavatelé.");
            return;
        }

        foreach (var s in suplierRepository.Items)
            ConsoleManager.Info($"[{s.Id}] {s.Name} | IČO: {s.ICO} | {s.Address} | {s.Email}");
    }

    void EditSupplier()
    {
        ListSuppliers();
        if (suplierRepository.Items.Count == 0) return;
        var id = ConsoleManager.ReadInt("ID dodavatele k úpravě");

        if (id == null)
            return;

        ConsoleManager.Success("Dodavatel upraven.");
    }

    void DeleteSupplier()
    {
        ListSuppliers();
        if (suplierRepository.Items.Count == 0) return;
        var id = ConsoleManager.ReadInt("ID dodavatele ke smazání");

        if (id == null)
            return;

        ConsoleManager.Success("Dodavatel smazán.");
    }

    // ── CLIENTS ───────────────────────────────

    void MenuClients()
    {
        while (true)
        {
            ConsoleManager.Header("Odběratelé");
            ConsoleManager.MenuItem(1, "Přidat odběratele");
            ConsoleManager.MenuItem(2, "Zobrazit seznam");
            ConsoleManager.MenuItem(3, "Upravit odběratele");
            ConsoleManager.MenuItem(4, "Smazat odběratele");
            ConsoleManager.Separator();
            ConsoleManager.MenuItem(0, "Zpět");

            int choice = ConsoleManager.ReadChoice(0, 4);
            switch (choice)
            {
                case 1: AddClient(); break;
                case 2: ListClients(); break;
                case 3: EditClient(); break;
                case 4: DeleteClient(); break;
                case 0: return;
            }
        }
    }

    void AddClient()
    {
        ConsoleManager.Header("Nový odběratel ('/q' pro ukončení)");
        var id = clientRepository.NextId();
        string? name = ConsoleManager.ReadLine("Název");
        if (string.IsNullOrEmpty(name)) return;

        ConsoleManager.Success($"Odběratel '{name}' uložen (ID {id}).");
    }

    void ListClients()
    {
        ConsoleManager.Header("Seznam odběratelů");
        if (clientRepository.Items.Count == 0)
        {
            ConsoleManager.Info("Žádní odběratelé.");
            return;
        }

        foreach (var c in clientRepository.Items)
            ConsoleManager.Info($"[{c.Id}] {c.Name} | IČO: {c.ICO} | {c.Address} | {c.Email}");
    }

    void EditClient()
    {
        ListClients();
        if (clientRepository.Items.Count == 0) return;
        int? id = ConsoleManager.ReadInt("ID odběratele k úpravě ('/q' pro ukončení)");

        if (id == null)
            return;

        Client? client = clientRepository.Get(id.Value);
        if (client != null)
        {
            ConsoleManager.Success("Odběratel upraven.");
        }
        else
        {
            ConsoleManager.Error($"Klient s id: {id} bohužel neexistuje.");
        }

        return;
    }

    void DeleteClient()
    {
        ListClients();
        if (clientRepository.Items.Count == 0) return;
        int? id = ConsoleManager.ReadInt("ID odběratele ke smazání ('/q' pro ukončení)");

        if (id == null)
            return;

        ConsoleManager.Success("Odběratel smazán.");
    }

    void ShowOrGenerateInvoices()
    {
        // while (true)
        // {
        // }
    }
}