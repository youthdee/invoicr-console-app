using System.Text;
using Invoicr.ConsoleApp.Repositories;
using Invoicr.Objects;

namespace Invoicr.Repositories;

public class SupplierRepository : Repository<Supplier, int>
{
    private const string path = "suppliers.csv";

    public SupplierRepository() : base(path)
    {
    }
}