using System.Text.RegularExpressions;
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
    //trochu hack, ale vzhledem k vybrané architektuře je to docela čisté řešení.

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
    
    /// <summary>
    /// Override metody ToString() pro snažší výpis v konzoli.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return
            $"| {Id,-4} | {Name,-25} | {ICO,-10} | {(VatPayer ? "Ano" : "Ne"),-3} | {Address.ToString(),-45} | {Email,-25} |";
    }
}