namespace Invoicr.Objects.AppSettings;

public class AppSettings
{
    //Required protože chci mít jistotu, že se app settings načtou. Jinak aplikace nemá smysl protože nemá kde najít pracovní soubory.
    //prefix faktur
    public required string InvoicePrefix { get; set; }

    //starovní číslo faktur - podle roku třeba - 2026001
    public required int InvoiceStartNumber { get; set; }

    //krokování faktur
    public required int InvoiceStep { get; set; }

    // Složka výtupu vygenerovaných PDFek.
    public required string PdfOutputFolder { get; set; }

    // Interní složka datových souborů. Dalo by se říct, že to je taková hodně lehká databáze.
    public required string CsvFolder { get; set; }
}