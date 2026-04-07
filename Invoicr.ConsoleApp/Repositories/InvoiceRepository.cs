using Invoicr.Objects;

namespace Invoicr.Repositories;

public class InvoiceRepository : Repository<Invoice, int>
{
    private const string file = "invoices.csv";

    public InvoiceRepository(string basePath) : base(Path.Combine(basePath, file))
    {
    }
}