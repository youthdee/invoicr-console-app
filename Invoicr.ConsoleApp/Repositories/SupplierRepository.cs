using Invoicr.Objects;

namespace Invoicr.Repositories;

/// <summary>
/// Repozitář dodavatelů. Dědí z base třídy Repository, díky generice využívá jejich metod a funkcionality.
/// </summary>
public class SupplierRepository : Repository<Supplier>
{
    //nastavení názvu souboru ("databáze") do konstanty
    private const string file = "suppliers.csv";

    //konstruktor předávající plnou cestu k CSV souboru zděděné třídě.
    public SupplierRepository(string basePath) : base(Path.Combine(basePath, file))
    {
    }
}