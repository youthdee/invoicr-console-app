namespace Invoicr.Objects.AppSettings;

public class AppSettings
{
    //prefix faktur
    public string InvoicePrefix { get; set; }

    //starovní číslo faktur - podle roku třeba - 2026001
    public int InvoiceStartNumber { get; set; }

    //krokování faktur
    public int InvoiceStep { get; set; }

    // Složka výtupu vygenerovaných PDFek.
    public string PdfOutputFolder { get; set; }

    // Interní složka datových souborů. Dalo by se říct, že to je taková hodně lehká databáze.
    public string CsvFolder { get; set; }
}