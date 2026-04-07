namespace Invoicr.Objects;

//dodavatel dědí od třídy LegalPerson, protože psát boilerplate kód je neefektivní
public class Supplier : LegalPerson
{
    public BankAccount? BankAccount { get; set; }

    internal long? AccountNumber
    {
        get => BankAccount?.AccountNumber;
        set
        {
            if (value == null) return;
            BankAccount ??= new BankAccount();
            BankAccount.AccountNumber = value.Value;
        }
    }

    internal int? BankNumber
    {
        get => BankAccount?.BankNumber;
        set
        {
            if (value == null) return;
            BankAccount ??= new BankAccount();
            BankAccount.BankNumber = value.Value;
        }
    }

    internal int? BankPrefix
    {
        get => BankAccount?.Prefix;
        set
        {
            if (value == null) return;
            BankAccount ??= new BankAccount();
            BankAccount.Prefix = value;
        }
    }
    public decimal? HourRate { get; set; }

    public Currency? HourRateCurrency { get; set; }

    public override string ToString()
    {
        string baseData = base.ToString().TrimEnd('|');
        return $"{baseData} | {HourRate,8:N2} {HourRateCurrency,-3} | {BankAccount?.ToString() ?? "N/A",-25} |";
    }
}