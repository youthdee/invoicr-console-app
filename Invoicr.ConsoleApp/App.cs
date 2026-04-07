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
            ConsoleManager.Header("Fakturační systém Invoicr");
            ConsoleManager.MenuItem(1, "Vytvořit novou fakturu");
            ConsoleManager.MenuItem(2, "Upravit seznam dodavatelů");
            ConsoleManager.MenuItem(3, "Upravit seznam odběratelů");
            ConsoleManager.MenuItem(4, "Zobrazit nebo generovat faktury");
            ConsoleManager.Separator();
            ConsoleManager.MenuItem(0, "Ukončit");

            int choice = ConsoleManager.ReadChoice(0, 4);
            switch (choice)
            {
                case 1: CreateInvoice(); break;
                case 2: MenuSuppliers(); break;
                case 3: MenuClients(); break;
                case 4: ShowOrGenerateInvoices(); break;
                case 0: return;
            }
        }
    }

    void CreateInvoice()
    {
        ConsoleManager.Header("Nová faktura");

        if (suplierRepository.Items.Count == 0)
        {
            ConsoleManager.Error("Nejprve přidejte dodavatele.");
            return;
        }

        if (clientRepository.Items.Count == 0)
        {
            ConsoleManager.Error("Nejprve přidejte odběratele.");
            return;
        }

        ConsoleManager.Info("Dodavatelé:");
        for (int i = 0; i < suplierRepository.Items.Count; i++)
            ConsoleManager.Info(
                $"  [{i + 1}] {suplierRepository.Items[i].Name} (IČO: {suplierRepository.Items[i].ICO})");
        ConsoleManager.Info($"  Vyberte 1–{suplierRepository.Items.Count}:");
        int si = ConsoleManager.ReadChoice(1, suplierRepository.Items.Count) - 1;
        Supplier supplier = suplierRepository.Items[si];

        ConsoleManager.Info("Odběratelé:");
        for (int i = 0; i < clientRepository.Items.Count; i++)
            ConsoleManager.Info($"  [{i + 1}] {clientRepository.Items[i].Name} (IČO: {clientRepository.Items[i].ICO})");
        ConsoleManager.Info($"  Vyberte 1–{clientRepository.Items.Count}:");
        int ci = ConsoleManager.ReadChoice(1, clientRepository.Items.Count) - 1;
        Client client = clientRepository.Items[ci];

        string number = appSettingsManager.NextInvoiceNumber();
        ConsoleManager.Info($"Číslo faktury: {number}");

        DateTime? issueDate = ConsoleManager.ReadDate("Datum vystavení", DateTime.Today);
        if (issueDate == null)
            return;
        DateTime? dueDate = ConsoleManager.ReadDate("Datum splatnosti", DateTime.Today.AddDays(14));
        if (dueDate == null)
            return;
        decimal? hours = ConsoleManager.ReadDecimal("Odpracované hodiny");
        if (hours == null)
            return;
        decimal? rate = ConsoleManager.ReadDecimal("Hodinová sazba", supplier.HourRate);
        if (rate == null)
            return;
        int? currency = ConsoleManager.ReadInt("Měna (0 pro CZK, 1 pro EUR)", 0);
        if (currency == null)
            return;
        string? note = ConsoleManager.ReadLine("Poznámka", $"Vytvořeno {DateTime.Now}");
        if (string.IsNullOrEmpty(note))
            return;

        var invoice = new Invoice()
        {
            Id = invoiceRepository.NextId(),
            Number = number,
            IssueDate = issueDate.Value,
            DueDate = dueDate.Value,
            ClientId = client.Id,
            SupplierId = supplier.Id,
            Currency = (Currency)currency.Value,
            Note = note,
            HourRate = rate.Value,
            HoursWorked = hours.Value
        };

        if (invoiceRepository.Create(invoice) != null)
        {
            appSettingsManager.BumpInvoiceNumber();

            ConsoleManager.Success($"Faktura {number} uložena. Celkem: {hours * rate:N2} {currency}");
            ConsoleManager.Info($"PDF výstup naleznete v: {appSettingsManager.Settings.PdfOutputFolder}");
        }
        else
        {
        }
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

        string? description = ConsoleManager.ReadLine("Popis");
        if (string.IsNullOrEmpty(description))
            return;

        int? ico = ConsoleManager.ReadInt("IČO");
        if (ico == null)
            return;

        int? vatPayer = ConsoleManager.ReadInt("Je plátcem DPH? (0 pro NE, 1 pro ANO", 1);
        if (vatPayer == null)
            return;

        int? dic = null;
        if (vatPayer == 1)
        {
            dic = ConsoleManager.ReadInt("DIČ");
            if (dic == null)
                return;
        }

        string? street = ConsoleManager.ReadLine("Ulice");
        if (string.IsNullOrEmpty(street))
            return;

        int? psc = ConsoleManager.ReadInt("PSč");
        if (psc == null)
            return;

        string? city = ConsoleManager.ReadLine("Ulice");
        if (string.IsNullOrEmpty(city))
            return;

        string? number = ConsoleManager.ReadLine("Ulice");
        if (string.IsNullOrEmpty(number))
            return;

        string? email = ConsoleManager.ReadLine("Kontaktní email");
        if (string.IsNullOrEmpty(email))
            return;

        int? hasBankAccount = ConsoleManager.ReadInt("Chcete přidat i bankovní účet?");
        if (hasBankAccount == null) return;

        BankAccount? bankAccount = null;
        if (hasBankAccount == 1)
        {
            long? accountNumber = ConsoleManager.ReadLong("Číslo účtu");
            if (accountNumber == null)
                return;

            int? bankNumber = ConsoleManager.ReadInt("Číslo banky");
            if (bankNumber == null)
                return;

            int? hasPrefix = ConsoleManager.ReadInt("Přejete si zadat i předčíslí účtu? (0 pro NE, 1 pro ANO", 0);
            if (hasPrefix == null)
                return;

            int? prefix = null;
            if (hasPrefix == 1)
            {
                prefix = ConsoleManager.ReadInt("DIČ");
                if (prefix == null)
                    return;
            }

            bankAccount = new BankAccount()
            {
                Prefix = prefix,
                AccountNumber = accountNumber.Value,
                BankNumber = bankNumber.Value,
            };
        }


        int? hasHourRate = ConsoleManager.ReadInt("Přejete si zadat výchozí hodinovou sazbu? (0 pro NE, 1 pro ANO", 1);
        if (hasHourRate == null)
            return;

        int? hourRate = null;
        if (hasHourRate == 1)
        {
            hourRate = ConsoleManager.ReadInt("DIČ");
            if (hourRate == null)
                return;
        }

        Address address = new Address()
        {
            City = city,
            Street = street,
            Number = number,
            PSC = psc.Value,
        };

        Supplier suplier = new Supplier()
        {
            Id = suplierRepository.NextId(),
            Name = name,
            Description = description,
            Address = address,
            HourRate = hourRate,
            BankAccount = bankAccount,
            DIC = dic,
            Email = email,
            ICO = ico.Value,
            VatPayer = vatPayer == 1,
        };

        if (suplierRepository.Create(suplier) != null)
        {
            ConsoleManager.Success($"Dodavatel '{name}' uložen (ID {id}).");
        }
        else
        {
        }
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

        Supplier? supplier = suplierRepository.Get(id.Value);
        if (supplier == null) return;

        string? name = ConsoleManager.ReadLine("Název", supplier.Name);
        if (string.IsNullOrEmpty(name))
            return;

        string? description = ConsoleManager.ReadLine("Popis", supplier.Description);
        if (string.IsNullOrEmpty(description))
            return;

        int? ico = ConsoleManager.ReadInt("IČO", supplier.ICO);
        if (ico == null)
            return;

        int? vatPayer = ConsoleManager.ReadInt("Je plátcem DPH? (0 pro NE, 1 pro ANO", supplier.VatPayer ? 1 : 0);
        if (vatPayer == null)
            return;

        int? dic = null;
        if (vatPayer == 1)
        {
            dic = ConsoleManager.ReadInt("DIČ", supplier.DIC);
            if (dic == null)
                return;
        }

        string? street = ConsoleManager.ReadLine("Ulice", supplier.Address.Street);
        if (string.IsNullOrEmpty(street))
            return;

        int? psc = ConsoleManager.ReadInt("PSč", supplier.Address.PSC);
        if (psc == null)
            return;

        string? city = ConsoleManager.ReadLine("Ulice", supplier.Address.City);
        if (string.IsNullOrEmpty(city))
            return;

        string? number = ConsoleManager.ReadLine("Číslo popisné", supplier.Address.Number);
        if (string.IsNullOrEmpty(number))
            return;

        string? email = ConsoleManager.ReadLine("Kontaktní email", supplier.Email);
        if (string.IsNullOrEmpty(email))
            return;

        int? hasBankAccount = ConsoleManager.ReadInt("Chcete přidat i bankovní účet?");
        if (hasBankAccount == null) return;

        BankAccount? bankAccount = null;
        if (hasBankAccount == 1)
        {
            long? accountNumber = ConsoleManager.ReadLong("Číslo účtu", supplier.BankAccount?.AccountNumber);
            if (accountNumber == null)
                return;

            int? bankNumber = ConsoleManager.ReadInt("Číslo banky", supplier.BankAccount?.BankNumber);
            if (bankNumber == null)
                return;

            int? hasPrefix = ConsoleManager.ReadInt("Přejete si zadat i předčíslí účtu? (0 pro NE, 1 pro ANO", 0);
            if (hasPrefix == null)
                return;

            int? prefix = null;
            if (hasPrefix == 1)
            {
                prefix = ConsoleManager.ReadInt("DIČ", supplier.BankAccount?.Prefix);
                if (prefix == null)
                    return;
            }

            bankAccount = new BankAccount()
            {
                Prefix = prefix,
                AccountNumber = accountNumber.Value,
                BankNumber = bankNumber.Value,
            };
        }


        int? hasHourRate = ConsoleManager.ReadInt("Přejete si zadat výchozí hodinovou sazbu? (0 pro NE, 1 pro ANO", 1);
        if (hasHourRate == null)
            return;

        decimal? hourRate = null;
        if (hasHourRate == 1)
        {
            hourRate = ConsoleManager.ReadDecimal("DIČ", supplier.HourRate);
            if (hourRate == null)
                return;
        }

        Address address = new Address()
        {
            City = city,
            Street = street,
            Number = number,
            PSC = psc.Value,
        };

        Supplier suplier = new Supplier()
        {
            Id = suplierRepository.NextId(),
            Name = name,
            Description = description,
            Address = address,
            HourRate = hourRate,
            BankAccount = bankAccount,
            DIC = dic,
            Email = email,
            ICO = ico.Value,
            VatPayer = vatPayer == 1,
        };

        if (suplierRepository.Update(id.Value, suplier) != null)
        {
            ConsoleManager.Success("Dodavatel upraven.");
        }
        else
        {
        }
    }

    void DeleteSupplier()
    {
        ListSuppliers();
        if (suplierRepository.Items.Count == 0) return;
        var id = ConsoleManager.ReadInt("ID dodavatele ke smazání");

        if (id == null)
            return;

        if (suplierRepository.Delete(id.Value) != null)
        {
            ConsoleManager.Success("Dodavatel smazán.");
        }
        else
        {
        }
    }

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
        ConsoleManager.Header("Nový odběratel");
        var id = clientRepository.NextId();

        string? name = ConsoleManager.ReadLine("Název");
        if (string.IsNullOrEmpty(name))
            return;

        string? description = ConsoleManager.ReadLine("Popis");
        if (string.IsNullOrEmpty(description))
            return;

        int? ico = ConsoleManager.ReadInt("IČO");
        if (ico == null)
            return;

        int? vatPayer = ConsoleManager.ReadInt("Je plátcem DPH? (0 pro NE, 1 pro ANO", 1);
        if (vatPayer == null)
            return;

        int? dic = null;
        if (vatPayer == 1)
        {
            dic = ConsoleManager.ReadInt("DIČ");
            if (dic == null)
                return;
        }

        string? street = ConsoleManager.ReadLine("Ulice");
        if (string.IsNullOrEmpty(street))
            return;

        int? psc = ConsoleManager.ReadInt("PSč");
        if (psc == null)
            return;

        string? city = ConsoleManager.ReadLine("Ulice");
        if (string.IsNullOrEmpty(city))
            return;

        string? number = ConsoleManager.ReadLine("Ulice");
        if (string.IsNullOrEmpty(number))
            return;

        string? email = ConsoleManager.ReadLine("Kontaktní email");
        if (string.IsNullOrEmpty(email))
            return;

        Address address = new Address()
        {
            City = city,
            Street = street,
            Number = number,
            PSC = psc.Value,
        };

        Client suplier = new Client()
        {
            Id = suplierRepository.NextId(),
            Name = name,
            Description = description,
            Address = address,
            DIC = dic,
            Email = email,
            ICO = ico.Value,
            VatPayer = vatPayer == 1,
        };

        if (clientRepository.Create(suplier) != null)
        {
            ConsoleManager.Success($"Odběratel '{name}' uložen (ID {id}).");
        }
        else
        {
        }
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
        var id = ConsoleManager.ReadInt("ID dodavatele k úpravě");
        if (id == null)
            return;

        Client? supplier = clientRepository.Get(id.Value);
        if (supplier == null) return;

        string? name = ConsoleManager.ReadLine("Název", supplier.Name);
        if (string.IsNullOrEmpty(name))
            return;

        string? description = ConsoleManager.ReadLine("Popis", supplier.Description);
        if (string.IsNullOrEmpty(description))
            return;

        int? ico = ConsoleManager.ReadInt("IČO", supplier.ICO);
        if (ico == null)
            return;

        int? vatPayer = ConsoleManager.ReadInt("Je plátcem DPH? (0 pro NE, 1 pro ANO", supplier.VatPayer ? 1 : 0);
        if (vatPayer == null)
            return;

        int? dic = null;
        if (vatPayer == 1)
        {
            dic = ConsoleManager.ReadInt("DIČ", supplier.DIC);
            if (dic == null)
                return;
        }

        string? street = ConsoleManager.ReadLine("Ulice", supplier.Address.Street);
        if (string.IsNullOrEmpty(street))
            return;

        int? psc = ConsoleManager.ReadInt("PSč", supplier.Address.PSC);
        if (psc == null)
            return;

        string? city = ConsoleManager.ReadLine("Ulice", supplier.Address.City);
        if (string.IsNullOrEmpty(city))
            return;

        string? number = ConsoleManager.ReadLine("Číslo popisné", supplier.Address.Number);
        if (string.IsNullOrEmpty(number))
            return;

        string? email = ConsoleManager.ReadLine("Kontaktní email", supplier.Email);
        if (string.IsNullOrEmpty(email))
            return;

        Address address = new Address()
        {
            City = city,
            Street = street,
            Number = number,
            PSC = psc.Value,
        };

        Supplier suplier = new Supplier()
        {
            Id = suplierRepository.NextId(),
            Name = name,
            Description = description,
            Address = address,
            DIC = dic,
            Email = email,
            ICO = ico.Value,
            VatPayer = vatPayer == 1,
        };

        if (suplierRepository.Update(id.Value, suplier) != null)
        {
            ConsoleManager.Success("Odběratel upraven.");
        }
        else
        {
        }
    }

    void DeleteClient()
    {
        ListClients();
        if (clientRepository.Items.Count == 0) return;
        var id = ConsoleManager.ReadInt("ID odběratele ke smazání");

        if (id == null)
            return;

        if (clientRepository.Delete(id.Value) != null)
        {
            ConsoleManager.Success("Odběratel smazán.");
        }
        else
        {
        }
    }

    void ShowOrGenerateInvoices()
    {
        // while (true)
        // {
        // }
    }
}