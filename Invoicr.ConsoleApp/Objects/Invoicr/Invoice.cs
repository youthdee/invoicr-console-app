using System.Text;
using Invoicr.Repositories;

namespace Invoicr.Objects;

public class Invoice : IObjectWithId<int>
{
    //primární idetifikátor faktury
    public int Id { get; set; }

    //číslo faktury, přednastavené v nastavení aplikace, nemusí být nutně číslo!
    public string Number { get; set; }

    //Identifikátor a objekt dodavatele, faktura bez něho nemá smysl
    public int SupplierId { get; set; }

    public Supplier Supplier { get; set; }

    //Identifikátor a objekt odběratele, faktura bez něho nemá smysl
    public int ClientId { get; set; }

    public Client Client { get; set; }

    //datum vystavení faktury
    public DateTime IssueDate { get; set; }

    //datum splatnosti faktury
    public DateTime DueDate { get; set; }

    //Počet odpracovaných hodin
    public decimal HoursWorked { get; set; }

    /*
     * backing field musí být nullable, aby šlo poznat "nezadáno", protože defautlně chceme,
     * aby byla hodinovka na faktuře použita z dodavatele, ale jenom pokud ji má. V opačném případě je prostě unset.
     * A nemůže být null, protože by pak faktura nedávala smysl.
     */
    private decimal? _hourRate;

    //hodinovka, defualtně bude přednastavena na dodavatelovi
    public decimal HourRate
    {
        get => _hourRate ?? Supplier?.HourRate ?? 0; //0 jako unset hodnota decimal. Nullable být nemůže.
        set => _hourRate = value;
    }

    // Měna jako enum pro přehlednost a typovou kontrolu.
    public Currency Currency { get; set; }

    // Popis faktury, defaultně datum vytvoření.
    public string? Note { get; set; }

    /// <summary>
    /// Override metody ToString() pro snažší výpis v konzoli.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        decimal total = HoursWorked * HourRate;
        return
            $"| {Id,-4} | {Number,-12} | {IssueDate:dd.MM.yyyy} | {DueDate:dd.MM.yyyy} | {total,28:N2} {Currency,-3} | {Supplier.Name,-20} | {Client.Name,-20} |";
    }

    /// <summary>
    /// Metoda pro hezké vypsání faktury jako textový výstup.
    /// </summary>
    /// <returns></returns>
    public string GetTextOutput()
    {
        StringBuilder sb = new();

        // hlavička 
        sb.AppendLine($"Faktura: {Number}");
        sb.AppendLine($"Poznámka: {Note}");

        //obecné informace faktury (datumy)
        sb.AppendLine(new string('=', 50));
        sb.AppendLine($"Datum vystavení: {IssueDate.ToString("dd.MM.yyyy")}");
        sb.AppendLine($"Datum splatnosti: {DueDate.ToString("dd.MM.yyyy")}");

        //odběratel
        sb.AppendLine(new string('=', 50));
        sb.AppendLine($"Odběratel: {Client.Name}");
        sb.AppendLine($"IČO: {Client.ICO}");
        sb.AppendLine($"Plátce DPH: {Client.VatPayer}");
        if (Client.VatPayer)
        {
            sb.AppendLine($"DIČ: {Client.DIC}");
        }

        sb.AppendLine();
        sb.AppendLine($"Adresa: {Client.Address.ToString()}");
        sb.AppendLine($"Email: {Client.Email}");
        sb.AppendLine();
        sb.AppendLine($"Poznámka: {Client.Description}");

        //dodavatel
        sb.AppendLine(new string('=', 50));
        sb.AppendLine($"Dodavatel: {Supplier.Name}");
        sb.AppendLine($"IČO: {Supplier.ICO}");
        sb.AppendLine($"Plátce DPH: {Supplier.VatPayer}");
        if (Client.VatPayer)
        {
            sb.AppendLine($"DIČ: {Supplier.DIC}");
        }

        sb.AppendLine();
        sb.AppendLine($"Adresa: {Supplier.Address.ToString()}");
        sb.AppendLine($"Email: {Supplier.Email}");
        if (Supplier.BankAccount != null)
        {
            sb.AppendLine();
            sb.AppendLine($"Bankovní účet: {Supplier.BankAccount?.ToString()}");
        }

        sb.AppendLine();
        sb.AppendLine($"Poznámka: {Supplier.Description}");

        sb.AppendLine(new string('=', 50));
        sb.AppendLine($"Celkem k úhradě: {HourRate * HoursWorked} {Currency.ToString()}");
        sb.AppendLine($"Celkem položek (odpracovaných hodin): {HoursWorked}");
        sb.AppendLine($"Celkem cena za položku vč. DPH: {HourRate}");

        sb.AppendLine();
        sb.AppendLine(
            $"Fakturujeme Vám zboží dle objednávky: " +
            $"{HourRate * HoursWorked} {Currency.ToString()}, " +
            $"{(Supplier.BankAccount is not null ? $"převodem na bank. účet {Supplier?.BankAccount?.ToString()}" : "hotově")}.");

        return sb.ToString();
    }
}