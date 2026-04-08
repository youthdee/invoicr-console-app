namespace Invoicr.Objects;

//dodavatel dědí od třídy LegalPerson, protože psát boilerplate kód je neefektivní
public class Supplier : LegalPerson
{
    // Cizí objekt bankovního účtu
    // Nemá FK, protože nemá ani tabulku (CSV)
    // Slouží jen jako syntax sugar
    public BankAccount? BankAccount { get; set; }

    //internal  properties pro repozitář, aby uložil adresu do CSV. (neukládá složité objekty)
    //trochu hack, ale vzhledem k vybrané architektuře je to docela čisté řešení.
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

    // hodinová sazba která může být defaultně použita při tvorbě faktur
    public decimal? HourRate { get; set; }

    // měna hodinové sazby 
    public Currency? HourRateCurrency { get; set; }

    /// <summary>
    /// Override metody ToString() pro snažší výpis v konzoli.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        string baseData = base.ToString().TrimEnd('|');
        return $"{baseData} | {HourRate,8:N2} {HourRateCurrency,-3} | {BankAccount?.ToString() ?? "N/A",-25} |";
    }
}