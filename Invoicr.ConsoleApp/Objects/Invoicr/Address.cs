namespace Invoicr.Objects;

public class Address
{
    // Adresa
    public string Street { get; set; }
    // Směrovací číslo
    public int PSC { get; set; }
    // Město
    public string City { get; set; }
    //Number jako číslo popisné, může obsahovat i znak
    public string Number { get; set; }

    /// <summary>
    /// Override metody ToString() pro snažší výpis v konzoli.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => $"{PSC} {City}, {Street} {Number}";
}