namespace Invoicr.Objects;

public class Address
{
    public string Street { get; set; }
    public int PSC { get; set; }
    public string City { get; set; }
    public string Number { get; set; }

    public override string ToString() => $"{PSC} {City}, {Street} {Number}";
}