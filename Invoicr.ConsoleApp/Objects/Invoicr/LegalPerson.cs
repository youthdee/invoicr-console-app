using Invoicr.Repositories;

namespace Invoicr.Objects;

public class LegalPerson : IObjectWithId<int>
{
    //Primární identifikátor
    public int Id { get; set; }

    //Název subjektu - může být jak firma, tak jméno - přijímení
    public string Name { get; set; }

    //Nepovinný popis, může nastat situace, kdy se střetnou dva stejnojmenné OSVČ.
    public string? Description { get; set; }

    //Sekundární identifikátor právní osoby
    public int ICO { get; set; }

    //Plátce DPH kvůli DIC, které je nullable
    public bool VatPayer { get; set; }

    public int? DIC { get; set; }

    //Cizí objekt adresy pro snažší manipulaci a čitelnost
    public Address Address { get; set; } = new Address();

    //internal  properties pro repozitář, aby uložil adresu do CSV. (neukládá složité objekty)
    //trochu hack, ale co se dá dělat.

    internal string Street
    {
        get => Address.Street;
        set => Address.Street = value;
    }

    internal string City
    {
        get => Address.City;
        set => Address.City = value;
    }

    internal int PSC
    {
        get => Address.PSC;
        set => Address.PSC = value;
    }

    internal string Number
    {
        get => Address.Number;
        set => Address.Number = value;
    }
    public string Email { get; set; }

    //Jednoduchá regex funkce, která bez výstupu zvaliduje email (Platný / neplatný).
    public bool ValidateEmail(string email)
    {
        return true;
    }
    public override string ToString() => $"|Id: {Id} |Název: {Name} |Ičo: {ICO} |Plátce DPH: {(VatPayer ? "Ano" : "Ne")} |Adresa: {Address.ToString()} |Email: {Email} |";
}