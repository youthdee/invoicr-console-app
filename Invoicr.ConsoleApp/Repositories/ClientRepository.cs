using Invoicr.Objects;

namespace Invoicr.Repositories;

/// <summary>
/// Repozitář odběratelů. Dědí z base třídy Repository, díky generice využívá jejich metod a funkcionality.
/// </summary>
public class ClientRepository : Repository<Client>
{
    //nastavení názvu souboru ("databáze") do konstanty
    private const string file = "clients.csv";

    //konstruktor předávající plnou cestu k CSV souboru zděděné třídě.
    public ClientRepository(string basePath) : base(Path.Combine(basePath, file))
    {
    }
}