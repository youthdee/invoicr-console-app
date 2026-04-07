namespace Invoicr.Objects;

//odběratel také dědí od právní osoby, technicky by nemusel a mohl by být rovnou právní osobou. Ale pro přehlednost jsem tuto abstrakci zanechal.
public class Client : LegalPerson
{
}