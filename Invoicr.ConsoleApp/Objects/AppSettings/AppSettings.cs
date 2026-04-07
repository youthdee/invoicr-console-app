namespace Invoicr.Objects.AppSettings;

public class AppSettings
{
    //prefix faktur
    public string InvoicePrefix { get; set; } = "FAK";

    //starovní číslo faktur - podle roku třeba - 2026001
    public int InvoiceStartNumber { get; set; } = 1;

    //krokování faktur
    public int InvoiceStep { get; set; } = 1;

    // Složka výtupu vygenerovaných PDFek.
    public string PdfOutputFolder { get; set; } = "output/pdf";

    // Interní složka datových souborů. Dalo by se říct, že to je taková hodně lehká databáze.
    public string CsvFolder { get; set; } = "data/csv";
}