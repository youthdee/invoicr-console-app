namespace Invoicr.Objects;

//dodavatel dědí od třídy LegalPerson, protože psát boilerplate kód je neefektivní
public class Supplier : LegalPerson
{
    public BankAccount? BankAccount { get; set; } //dodavatel teoreticky nemusí mít účet, může dostat hotovost.
    public decimal? HourRate { get; set; } // dodavatel může mít přednastavenou hodinovku
}