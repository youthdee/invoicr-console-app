namespace Invoicr.Objects;

public class BankAccount
{
    //číslo účtu bez předčíslí a čísla banky
    public long AccountNumber { get; set; }

    //číslo banky (např. O300, uloží se ale bez nuly.)
    public int BankNumber { get; set; }

    //předčíslí účtu, nepovinné
    public int? Prefix { get; set; }

    public string GetFullAccountNumber() =>
        $"{(Prefix.HasValue ? $"{Prefix}-" : string.Empty)}{AccountNumber}/{BankNumber:D4}";
    //D4 je formát čísla, který číslo zformátuje na počet číslic. 2 -> 0002 apod.
}