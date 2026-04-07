using System.Globalization;
using System.Text;
using Invoicr.Objects;

namespace Invoicr.Repositories;

public class InvoiceRepository : Repository<Invoice, int>
{
    private const string path = "invoices.csv";

    public InvoiceRepository() : base(path)
    {
    }
}