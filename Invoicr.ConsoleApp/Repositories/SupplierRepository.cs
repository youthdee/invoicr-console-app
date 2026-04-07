using Invoicr.Objects;

namespace Invoicr.Repositories;

public class SupplierRepository : Repository<Supplier>
{
    private const string file = "suppliers.csv";

    public SupplierRepository(string basePath) : base(Path.Combine(basePath, file))
    {
    }
}