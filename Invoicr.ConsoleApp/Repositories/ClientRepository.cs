using Invoicr.Objects;

namespace Invoicr.Repositories;

public class ClientRepository : Repository<Client>
{
    private const string file = "clients.csv";

    public ClientRepository(string basePath) : base(Path.Combine(basePath, file))
    {
    }
}