using System.Reflection.Metadata.Ecma335;
using System.Text;
using Invoicr.Managers;
using Invoicr.Objects;
using Invoicr.Objects.AppSettings;
using Invoicr.Repositories;
using System.Text.Json;

namespace Invoicr.ConsoleApp;

public class App
{
    //repozitáře
    private readonly SupplierRepository suplierRepository;
    private readonly ClientRepository clientRepository;
    private readonly InvoiceRepository invoiceRepository;

    //nastavení aplikace
    private readonly AppSettings appSettings;

    //konstruktor, inicializuje celou aplikaci
    public App()
    {
        //pokud neexsitují appsettings.json, tak zkusíme appsettings.template.json
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string filePath1 = Path.Combine(baseDir, "appsettings.json");
        string filePath2 = Path.Combine(baseDir, "appsettings.template.json");

        bool exists = File.Exists(filePath1) || File.Exists(filePath2);

        //pokud existují, tak parsneme json do objektu AppSettings
        if (exists)
        {
            //určíme, jaký soubor to tedy bude - prioritu mají appsettings
            string path = File.Exists(filePath1) ? filePath1 : filePath2;

            //načteme si json jako string
            string jsonString = File.ReadAllText(path);

            //optiony pro deserialiuzer ignorující velká a malá písmena
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            //zkusíme parsnout do appsettings třídy
            //system.text.json je v tomhle trochu horší než newtonsoft
            try
            {
                appSettings = JsonSerializer.Deserialize<AppSettings>(jsonString, options)
                              ?? throw new InvalidOperationException("Nepodařilo se načíst appsettings.json");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Kritická chyba: Soubor nastavení je neplatný! {ex.Message}");
                //rovnou ukončíme aplikaci protože bez appsettings nemá smysl (neví kde jsou CSV soubory nebo kde je má vytvořit)
                Environment.Exit(1);
                return;
            }
        }
        else
        {
            // appSettings = new()
            // {
            // CsvFolder = "data/csv",
            // InvoicePrefix = "FAK",
            // InvoiceStartNumber = 1,
            // InvoiceStep = 1,
            // PdfOutputFolder = "data/pdf",
            // };
            throw new NotImplementedException("Nepodařilo se najít appsettings.json / appsettings.template.json");
        }

        //inicializace repozitářů, jednotlivé vysvětlení jsou v nich
        suplierRepository = new SupplierRepository(appSettings.CsvFolder);
        clientRepository = new ClientRepository(appSettings.CsvFolder);
        invoiceRepository = new InvoiceRepository(appSettings.CsvFolder, suplierRepository, clientRepository);
    }

    /// <summary>
    /// Metoda, která aplikaci nastartuje po inicializaci (konstruktoru)
    /// </summary>
    public void Run()
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
        while (true)
        {
            ConsoleManager.Header("Fakturační systém Invoicr");
            ConsoleManager.MenuItem(1, "Vytvořit novou fakturu");
            ConsoleManager.MenuItem(2, "Zobrazit seznam faktur");
            ConsoleManager.MenuItem(3, "Upravit seznam dodavatelů");
            ConsoleManager.MenuItem(4, "Upravit seznam odběratelů");
            ConsoleManager.Separator();
            ConsoleManager.MenuItem(0, "Ukončit");

            int? choice = ConsoleManager.ReadChoice(0, 4);
            if (choice == null) return;
            switch (choice)
            {
                case 1: CreateInvoice(); break;
                case 2: ListInvoices(); break;
                case 3: MenuSuppliers(); break;
                case 4: MenuClients(); break;
                case 0: return;
            }
        }
    }

    /// <summary>
    /// Metoda která vyvolá vytvoření faktury.
    /// </summary>
    void CreateInvoice()
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
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
        int? si = ConsoleManager.ReadChoice(1, suplierRepository.Items.Count) - 1;
        if (si == null) return;
        Supplier supplier = suplierRepository.Items[si.Value];

        ConsoleManager.Info("Odběratelé:");
        for (int i = 0; i < clientRepository.Items.Count; i++)
            ConsoleManager.Info($"  [{i + 1}] {clientRepository.Items[i].Name} (IČO: {clientRepository.Items[i].ICO})");
        ConsoleManager.Info($"  Vyberte 1–{clientRepository.Items.Count}:");
        int? ci = ConsoleManager.ReadChoice(1, clientRepository.Items.Count) - 1;
        if (ci == null) return;
        Client client = clientRepository.Items[ci.Value];

        string number =
            $"{appSettings.InvoicePrefix}{(appSettings.InvoiceStartNumber + appSettings.InvoiceStep * invoiceRepository.Items.Count()):D4}";
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
        string? note = ConsoleManager.ReadLine("Poznámka", $"Vytvořeno {DateTime.Now.ToString("dd.MM.yyyy")}");
        if (string.IsNullOrEmpty(note))
            return;

        var invoice = new Invoice()
        {
            Id = invoiceRepository.NextId(),
            Number = number,
            IssueDate = issueDate.Value,
            DueDate = dueDate.Value,
            ClientId = client.Id,
            Client = client,
            Supplier = supplier,
            SupplierId = supplier.Id,
            Currency = (Currency)currency.Value,
            Note = note,
            HourRate = rate.Value,
            HoursWorked = hours.Value
        };

        if (invoiceRepository.Create(invoice) != null)
        {
            ConsoleManager.Success($"Faktura {number} uložena. Celkem: {hours * rate:N2} {currency}");

            //vytvořit výstup

            string fileName = Path.Combine(appSettings.PdfOutputFolder, $"{invoice.Number}.txt");

            string? directory = Path.GetDirectoryName(fileName);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                // Pokud složka neexistuje, vytvoříme ji (včetně všech podložek)
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fileName, invoice.GetTextOutput(), System.Text.Encoding.UTF8);

            ConsoleManager.Info($"PDF výstup naleznete v: {fileName}.");
        }
        else
        {
        }
    }

    /// <summary>
    /// Metoda, která vypíše faktury jako tabulku
    /// </summary>
    void ListInvoices()
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
        ConsoleManager.Header("Seznam faktur");
        if (invoiceRepository.Items.Count == 0)
        {
            ConsoleManager.Info("Žádné faktury.");
            return;
        }

        ConsoleManager.Separator();
        ConsoleManager.PrintTable("Faktury", invoiceRepository.Items,
            $"| {"ID",-4} | {"ČÍSLO",-12} | {"VYSTAVENO",-10} | {"SPLATNOST",-10} | {"CELKEM",-32} | {"DODAVATEL",-20} | {"ODBĚRATEL",-20} |",
            130);
    }

    /// <summary>
    /// Metoda, která zobrazí menu dodavatelů
    /// </summary>
    void MenuSuppliers()
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
        ListSuppliers();
        while (true)
        {
            ConsoleManager.Header("Dodavatelé");
            ConsoleManager.MenuItem(1, "Přidat dodavatele");
            ConsoleManager.MenuItem(2, "Zobrazit seznam");
            ConsoleManager.MenuItem(3, "Upravit dodavatele");
            ConsoleManager.MenuItem(4, "Smazat dodavatele");
            ConsoleManager.Separator();
            ConsoleManager.MenuItem(0, "Zpět");

            int? choice = ConsoleManager.ReadChoice(0, 4);
            if (choice == null) return;
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

    /// <summary>
    /// Metoda, která vyvolá přidání dodavatele
    /// </summary>
    void AddSupplier()
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
        ConsoleManager.Header("Nový dodavatel");

        Supplier? supplier = AddOrEditSupplier();
        if (supplier == null) return;

        supplier.Id = suplierRepository.NextId();

        if (suplierRepository.Create(supplier) != null)
        {
            ConsoleManager.Success($"Dodavatel '{supplier.Name}' uložen (ID {supplier.Id}).");
        }
        else
        {
        }
    }

    /// <summary>
    /// Metoda, která vypíše seznam dodavatelů jako tabulku
    /// </summary>
    void ListSuppliers()
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
        ConsoleManager.Header("Seznam dodavatelů");
        if (suplierRepository.Items.Count == 0)
        {
            ConsoleManager.Info("Žádní dodavatelé.");
            return;
        }

        ConsoleManager.Separator();
        ConsoleManager.PrintTable("Dodavatelé", suplierRepository.Items,
            $"| {"ID",-4} | {"NÁZEV",-25} | {"IČO",-10} | {"DPH",-3} | {"ADRESA",-45} | {"EMAIL",-26} | {"SAZBA",-29} | {"BANKOVNÍ ÚČET",-25} |",
            192);
    }

    /// <summary>
    /// Metoda, která vyvolá editaci dodavatele
    /// </summary>
    void EditSupplier()
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
        ListSuppliers();
        if (suplierRepository.Items.Count == 0) return;
        var id = ConsoleManager.ReadInt("ID dodavatele k úpravě");
        if (id == null)
            return;

        Supplier? supplierToUpdate = suplierRepository.Items.SingleOrDefault(x => x.Id == id);
        if (supplierToUpdate == null) return;
        Supplier? newSupplier = AddOrEditSupplier(supplierToUpdate);
        if (newSupplier == null) return;

        if (suplierRepository.Update(id.Value, newSupplier) != null)
        {
            ConsoleManager.Success("Dodavatel upraven.");
        }
        else
        {
        }
    }

    /// <summary>
    /// Metoda, která vyvolá smazání dodavatele
    /// </summary>
    void DeleteSupplier()
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
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

    /// <summary>
    /// Metoda, která vyvolá menu odběratelů
    /// </summary>
    void MenuClients()
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
        ListClients();
        while (true)
        {
            ConsoleManager.Header("Odběratelé");
            ConsoleManager.MenuItem(1, "Přidat odběratele");
            ConsoleManager.MenuItem(2, "Zobrazit seznam");
            ConsoleManager.MenuItem(3, "Upravit odběratele");
            ConsoleManager.MenuItem(4, "Smazat odběratele");
            ConsoleManager.Separator();
            ConsoleManager.MenuItem(0, "Zpět");

            int? choice = ConsoleManager.ReadChoice(0, 4);
            if (choice == null) return;
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

    /// <summary>
    /// Metoda, která vyvolá vytvoření odběratele
    /// </summary>
    void AddClient()
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
        ConsoleManager.Header("Nový odběratel");
        var id = clientRepository.NextId();

        Client? client = AddOrEditClient();
        if (client == null)
            return;

        client.Id = id;

        if (clientRepository.Create(client) != null)
        {
            ConsoleManager.Success($"Odběratel '{client.Name}' uložen (ID {id}).");
        }
        else
        {
        }
    }

    /// <summary>
    /// Metoda, která zobrazí seznam odběratelů v tabulce
    /// </summary>
    void ListClients()
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
        ConsoleManager.Header("Seznam odběratelů");
        if (clientRepository.Items.Count == 0)
        {
            ConsoleManager.Info("Žádní odběratelé.");
            return;
        }

        ConsoleManager.Separator();
        ConsoleManager.PrintTable("Odběratelé", clientRepository.Items,
            $"| {"ID",-4} | {"NÁZEV",-25} | {"IČO",-10} | {"DPH",-3} | {"ADRESA",-45} | {"EMAIL",-25} |",
            131);
    }

    /// <summary>
    /// Metoda, která umožní editaci odběratele
    /// </summary>
    void EditClient()
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
        ListClients();
        if (clientRepository.Items.Count == 0) return;
        var id = ConsoleManager.ReadInt("ID dodavatele k úpravě");
        if (id == null) return;

        Client? supplier = clientRepository.Get(id.Value);
        if (supplier == null) return;
        Client? newClient = AddOrEditClient(supplier);
        if (newClient == null) return;

        if (clientRepository.Update(id.Value, newClient) != null)
        {
            ConsoleManager.Success("Odběratel upraven.");
        }
        else
        {
        }
    }

    /// <summary>
    /// Metoda, která umožní odstranění odběratele
    /// </summary>
    void DeleteClient()
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
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

    /// <summary>
    /// Metoda, která vytvoří nebo upraví LegalPerson
    /// </summary>
    /// <param name="person"></param>
    /// <returns></returns>
    private LegalPerson? AddOrEditPerson(LegalPerson? person = null)
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
        string? name = ConsoleManager.ReadLine("Zadejte název (/q pro ukončení)", person?.Name);
        if (string.IsNullOrEmpty(name))
            return null;

        string? description = ConsoleManager.ReadLine("Zadejte popis (/q pro ukončení)", person?.Description);
        if (string.IsNullOrEmpty(description))
            return null;

        int? ico = ConsoleManager.ReadInt("Zadejte IČO (/q pro ukončení)", person?.ICO);
        if (ico == null)
            return null;

        int? vatPayer = ConsoleManager.ReadInt("Je plátcem DPH? (0 pro NE, 1 pro ANO) (/q pro ukončení)",
            person?.VatPayer is not null ? (person.VatPayer ? 1 : 0) : 1);
        if (vatPayer == null)
            return null;

        int? dic = null;
        if (vatPayer == 1)
        {
            dic = ConsoleManager.ReadInt("Zadejte DIČ (/q pro ukončení)", person?.DIC);
            if (dic == null)
                return null;
        }

        string? street = ConsoleManager.ReadLine("Zadejte ulici (/q pro ukončení)", person?.Address.Street);
        if (string.IsNullOrEmpty(street))
            return null;

        int? psc = ConsoleManager.ReadInt("Zadejte PSČ (/q pro ukončení)", person?.Address.PSC);
        if (psc == null)
            return null;

        string? city = ConsoleManager.ReadLine("Zadejte město (/q pro ukončení)", person?.Address.City);
        if (string.IsNullOrEmpty(city))
            return null;

        string? number = ConsoleManager.ReadLine("Zadejte číslo popisné (/q pro ukončení)", person?.Address.Number);
        if (string.IsNullOrEmpty(number))
            return null;

        string? email = ConsoleManager.ReadEmail("Zadejte kontaktní email (/q pro ukončení)", person?.Email);
        if (string.IsNullOrEmpty(email))
            return null;

        Address address = new Address()
        {
            City = city,
            Street = street,
            Number = number,
            PSC = psc.Value,
        };

        LegalPerson newPerson = new LegalPerson()
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

        return newPerson;
    }

    /// <summary>
    /// Metoda, která vytvoří nebo upraví dodavetele (za pomocí metody AddOrEditLegalPerson)
    /// </summary>
    /// <param name="supplier"></param>
    /// <returns></returns>
    private Supplier? AddOrEditSupplier(Supplier? supplier = null)
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
        LegalPerson? person = AddOrEditPerson(supplier);

        if (person == null)
            return null;


        int? hasBankAccount = ConsoleManager.ReadInt("Chcete přidat i bankovní účet?");
        if (hasBankAccount == null)
            return null;

        BankAccount? bankAccount = null;
        if (hasBankAccount == 1)
        {
            long? accountNumber = ConsoleManager.ReadLong("Zadejte číslo účtu (/q pro ukončení)",
                supplier?.BankAccount?.AccountNumber);
            if (accountNumber == null)
                return null;

            int? bankNumber = ConsoleManager.ReadInt("Zadejte číslo banky (/q pro ukončení)",
                supplier?.BankAccount?.BankNumber);
            if (bankNumber == null)
                return null;

            int? hasPrefix = ConsoleManager.ReadInt("Přejete si zadat i předčíslí účtu? (0 pro NE, 1 pro ANO", 0);
            if (hasPrefix == null)
                return null;

            int? prefix = null;
            if (hasPrefix == 1)
            {
                prefix = ConsoleManager.ReadInt($"Zadejte předčíslí k účtu {accountNumber} (/q pro ukončení)",
                    supplier?.BankAccount?.Prefix);
                if (prefix == null)
                    return null;
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
            return null;

        int? hourRate = null;
        int? hourRateCurreny = null;
        if (hasHourRate == 1)
        {
            hourRate = ConsoleManager.ReadInt("Zadejte hodinovou sazbu (/q pro ukončení)");
            if (hourRate == null)
                return null;

            hourRateCurreny = ConsoleManager.ReadInt("Zadejte měnu hodinové sazby (/q pro ukončení)",
                (int?)supplier?.HourRateCurrency ?? 0);
            if (hourRateCurreny == null)
                return null;
        }

        return new Supplier()
        {
            Address = person.Address,
            Description = person.Description,
            DIC = person.DIC,
            Email = person.Email,
            ICO = person.ICO,
            Name = person.Name,
            VatPayer = person.VatPayer,
            BankAccount = bankAccount,
            HourRate = hourRate,
            HourRateCurrency = (Currency?)hourRateCurreny,
        };
    }

    /// <summary>
    /// Metoda, která upraví nebo vytvoří odběratele (za pomocí metody AddOrEditLegalPerson)
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    private Client? AddOrEditClient(Client? client = null)
    {
        //všechny vysvětlivky jsou napsané přímo v metodách, abych to nemusel furt psát.
        LegalPerson? person = AddOrEditPerson(client);


        if (person == null)
            return null;

        return new Client()
        {
            Address = person.Address,
            Description = person.Description,
            DIC = person.DIC,
            Email = person.Email,
            VatPayer = person.VatPayer,
            ICO = person.ICO,
            Name = person.Name
        };
    }
}