using Invoicr.Managers;
using Invoicr.Objects;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Invoicr.Repositories;

public class InvoiceRepository : Repository<Invoice, int>
{
    private const string file = "invoices.csv";

    private readonly SupplierRepository supplierRepository;
    private readonly ClientRepository clientRepository;

    public InvoiceRepository(string basePath, SupplierRepository supplierRepository, ClientRepository clientRepository) : base(Path.Combine(basePath, file), false)
    {
        this.supplierRepository = supplierRepository;
        this.clientRepository = clientRepository;

        Load();
    }

    public override void Load()
    {
        if (!File.Exists(FullPath)) return;

        //získání netøídových properties generického Itemu (do CSV neukládáme vnoøené tøídy, cheme objekty typu string, int apod...)
        var properties = base.GetSimpleProperties();

        Items = File.ReadAllLines(FullPath, Encoding.UTF8)
            .Skip(1) // První øádek je vždy hlavièka, takže pøeskoèíme
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l =>
            {
                //Získáme rozparsovaný øádek CSVèka jako pole stringù (bez oddìlovaèe)
                var values = CsvManager.ParseLine(l);
                Invoice? item = new Invoice();

                //tyto properties pak dosadíme ve stejném poøadí do properties objektu
                for (int i = 0; i < properties.Length && i < values.Length; i++)
                {
                    var prop = properties[i];
                    var value = values[i];
                    //musíme zpìt konvertovat
                    object convertedValue = base.ConvertValue(value, prop.PropertyType);
                    prop.SetValue(item, convertedValue);
                }

                //naètu i FK objekty, abych je pak u faktur mohl hezky vypisovat.

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