using System.Text;
using Invoicr.Objects;
using Invoicr.Repositories;

namespace Invoicr.ConsoleApp.Repositories;

public class ClientRepository : Repository<Client, int>
{
    private const string path = "clients.csv";

    public ClientRepository() : base(path)
    {
    }
}