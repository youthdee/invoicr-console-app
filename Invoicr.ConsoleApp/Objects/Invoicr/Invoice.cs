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

    public Currency Currency { get; set; }
    public string Note { get; set; }
}