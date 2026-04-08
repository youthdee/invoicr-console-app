using Invoicr.Managers;
using Invoicr.Objects;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Invoicr.Repositories;

/// <summary>
/// Repozitář faktur. Dědí z base třídy Repository, díky generice využívá jejich metod a funkcionality.
/// Metoda Load je overridnutá a to zdůvodu automatického "načtení" cizých tříd jako je Supplier a Client.
/// </summary>
public class InvoiceRepository : Repository<Invoice>
{
    private const string file = "invoices.csv";

    //nastavení názvu souboru ("databáze") do konstanty
    private readonly SupplierRepository supplierRepository;
    private readonly ClientRepository clientRepository;

    //konstruktor předávající plnou cestu k CSV souboru zděděné třídě.
    //zároveň nastaví repozitáře.
    public InvoiceRepository(string basePath, SupplierRepository supplierRepository, ClientRepository clientRepository)
        : base(Path.Combine(basePath, file), false)
    {
        this.supplierRepository = supplierRepository;
        this.clientRepository = clientRepository;

        //jelikož jsme overridnuli metodu load a vypnuli preload, musíme data načíst tady. 
        //vysvětlení proč jsem to tak udělal je v Repository samotném.
        Load();
    }

    public override void Load()
    {
        if (!File.Exists(FullPath)) return;

        //získání netřídových properties generického Itemu (do CSV neukládáme vnořené třídy, chceme objekty typu string, int apod...)
        var properties = base.GetSimpleProperties();

        Items = File.ReadAllLines(FullPath, Encoding.UTF8)
            .Skip(1) // První řádek je vždy hlavička, takže přeskočíme
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l =>
            {
                //Získáme rozparsovaný řádek CSVčka jako pole stringů (bez oddělovače)
                var values = CsvManager.ParseLine(l);
                Invoice? item = new Invoice();

                //tyto properties pak dosadíme ve stejněm pořadí do properties objektu
                for (int i = 0; i < properties.Length && i < values.Length; i++)
                {
                    var prop = properties[i];
                    var value = values[i];
                    //musíme zpět konvertovat
                    object convertedValue = base.ConvertValue(value, prop.PropertyType);
                    prop.SetValue(item, convertedValue);
                }

                //načtu i FK objekty, abych je pak u faktur mohl hezky vypisovat.

                //pokud se z nějakého důvodu nepodaří načíst, vrátím null (jsem v selectu) a na konci vyfiltruju nenullové objekty faktur....
                Supplier? supplier = supplierRepository.Get(item.SupplierId);
                if (supplier == null) return null;

                Client? client = clientRepository.Get(item.ClientId);
                if (client == null) return null;


                item.Supplier = supplier;
                item.Client = client;

                return item;
            })
            .Where(x => x != null)
            .ToList()!;
    }
}