using Invoicr.ConsoleApp.Repositories;
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
    public Address Address { get; set; }
    public string Email { get; set; }

    //Jednoduchá regex funkce, která bez výstupu zvaliduje email (Platný / neplatný).
    public bool ValidateEmail(string email)
    {
        return true;
    }
}