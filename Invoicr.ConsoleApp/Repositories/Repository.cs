using System.Globalization;
using System.Reflection;
using System.Text;
using Invoicr.Managers;

namespace Invoicr.Repositories;

/// <summary>
/// Jednoduchý interface jasně garantující property Id typu ID.
/// </summary>
/// <typeparam name="ID"></typeparam>

// Bez toho interfacu bych nemohl udělat takto generickou base repozitřovou třídu.
// Jediné řešení by potom bylo přes reflexi.  
// Jelikož umím pracovat s touhle architekturou (Entity Framework, dotnet apod.) tak jsem zvolil tohle řešení.
public interface IObjectWithId<ID> where ID : notnull
{
    public ID Id { get; set; }
}

/// <summary>
/// Base repozitářová třída. Umí klasické CRUD operace.
/// Item musí implementovat interface IObjectWithId, který má property Id typu ID.
/// V našem případě je generika ID čistý int.
/// </summary>
/// <typeparam name="Item"></typeparam>
public abstract class Repository<Item> where Item : class, IObjectWithId<int>, new()
{
    // Cesta k CSV souboru. V našem případě převzatá z rodiče který tuto třídu dědí. 
    // Dalo by se říct, že to je takový connection string k DB.
    protected readonly string FullPath;

    //schválně neinicializuji, abych měl jistotu že jsou data načtená
    //Dalo by se říct, že to je takový IQueryable, neboli množina entit z databáze (definice select dotazu)
    //To nám hraje do karet, protože IQueryable a List mají LINQ. (queryable má ještě nadstavbu)
    public List<Item> Items { get; set; }

    /// <summary>
    /// Jednoduchý konstruktor. Nepovinný argument preload, který přednačte data do Items.
    /// Nepovinný je kvůli možnosti override metody Load. Jinak by se metoda Load předka volala předtím, než by se inicializoval předkův konstruktor.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="preload"></param>
    public Repository(string path, bool preload = true)
    {
        //když soubory a nebo cesta neexistuje, tak ji vytvořím (pokud to systém dovolí)
        if (!File.Exists(path))
        {
            //pro jistotu zkusíme vytvořit i složku, pokud už existuje tak se nic nestane
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.Create(path).Close();
        }

        //předání z konstruktoru do readonly proměnné
        this.FullPath = path;
        //pokud preload, tak načíst data už teď.
        if (preload)
            Load();
    }

    //CSV operace repozitáře
    /// <summary>
    /// Načte data z CSV do paměti (Items). Nepodporuje filtrování ani řazení.
    /// </summary>
    public virtual void Load()
    {
        //Pokud neexistuje CSV soubor, nemáme co načítat...
        if (!File.Exists(FullPath)) return;

        //získání netřídových properties generického Itemu (do CSV neukládáme vnořené třídy, cheme objekty typu string, int apod...)
        var properties = GetSimpleProperties();

        //Použijeme reflexi pro dynamické dosazení values do properties třídy Item. Abych to nemusel psát pro kažou třídu...
        Items = File.ReadAllLines(FullPath, Encoding.UTF8)
            .Skip(1) // První řádek je vždy hlavička, takže přeskočíme
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l =>
            {
                //Získáme rozparsovaný řádek CSVčka jako pole stringů (bez oddělovače)
                var values = CsvManager.ParseLine(l);
                var item = new Item();

                //tyto properties pak dosadíme ve stejném pořadí do properties objektu
                for (int i = 0; i < properties.Length && i < values.Length; i++)
                {
                    var prop = properties[i];
                    var value = values[i];
                    //musíme zpět konvertovat
                    object? convertedValue = ConvertValue(value, prop.PropertyType);
                    // a nakonec nastavíme value do property
                    prop.SetValue(item, convertedValue);
                }

                return item;
            })
            .ToList();
    }

    /// <summary>
    /// Metoda, která veme data (Items) a znovu je uloží do CSV. Tzn. přepíše.
    /// </summary>
    public void Save()
    {
        //získání netřídových properties generického Itemu (do CSV neukládáme vnořené třídy, cheme objekty typu string, int apod...)
        var properties = GetSimpleProperties();

        // řádky Itemů
        var lines = new List<string>();

        // 1. Hlavička - názvy vlastností
        lines.Add(CsvManager.Row(properties.Select(p => p.Name).ToArray()));

        // 2. Data
        //Jednoduchým selectem pro každý item konvertujeme jeho simple properties na "text", který je připravený být uložen do CSV.
        lines.AddRange(Items.Select(item =>
        {
            var values = properties.Select(p =>
            {
                var val = p.GetValue(item);
                return FormatValue(val);
            }).ToArray();

            return CsvManager.Row(values);
        }));

        File.WriteAllLines(FullPath, lines, Encoding.UTF8);
    }

    /// <summary>
    /// Pomocná metoda pro získání vlastností, které nejsou vnořené třídy
    /// </summary>
    /// <returns></returns>
    protected PropertyInfo[] GetSimpleProperties()
    {
        var allProperties =
            typeof(Item).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        return allProperties
            .Where(p =>
            {
                // Musí mít getter a musí být buď Public nebo Internal (IsAssembly)
                var getMethod = p.GetMethod;
                if (getMethod == null) return false;

                bool isPublicOrInternal = getMethod.IsPublic || getMethod.IsAssembly;
                if (!isPublicOrInternal) return false;

                // Rozbalíme Nullable typy (int? -> int)
                Type type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

                // Filtrujeme jen "jednoduché" typy - objekty (Address, BankAccount) to zahodí
                return type.IsPrimitive ||
                       type == typeof(string) ||
                       type == typeof(decimal) ||
                       type == typeof(DateTime);
            })
            .OrderBy(x => x.Name) // DŮLEŽITÉ: CSV musí odpovídat tomuto pořadí!
            .ToArray();
    }

    /// <summary>
    /// Formátování pro zápis do CSV
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    private string FormatValue(object? val)
    {
        if (val == null) return "";
        if (val is DateTime dt) return dt.ToString("dd.MM.yyyy");
        if (val is IFormattable formattable) return formattable.ToString(null, CultureInfo.InvariantCulture);
        return val.ToString() ?? "";
    }

    /// <summary>
    /// Převod z CSV stringu na typ vlastnosti
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <returns></returns>
    ///
    // funkce byla polovygenerována AI, resp. inspirována, protože jsem nevěděl, jak to udělat pro ty "jednoduché" properties.
    protected object? ConvertValue(string value, Type targetType)
    {
        // Pokusí se vytáhnout vnitřní typ z Nullable (např. z int? dostane int), jinak vrátí null.
        Type? underlyingType = Nullable.GetUnderlyingType(targetType);

        // Pokud byl typ Nullable, použije jeho vnitřní typ, jinak pracuje s původním typem.
        Type actualType = underlyingType ?? targetType;

        // Zkontroluje, jestli ze souboru nepřišel prázdný řetězec nebo jen mezery.
        if (string.IsNullOrWhiteSpace(value))
        {
            // Vrací null pro Nullable typy, ale pro běžné typy (jako int) vytvoří defaultní hodnotu (např. 0).
            return underlyingType != null ? null : Activator.CreateInstance(actualType);
        }

        // Ověří, jestli je cílový typ (i ten "rozbalený") typu DateTime.
        if (actualType == typeof(DateTime))
        {
            // Převede text na datum přesně podle českého formátu bez ohledu na nastavení počítače.
            return DateTime.ParseExact(value, "dd.MM.yyyy", CultureInfo.InvariantCulture);
        }

        // Zjistí, jestli je potřeba text převést na desetinné číslo (decimal).
        if (actualType == typeof(decimal))
        {
            // Rozparsuje číslo pomocí neutrální kultury, aby se nehádaly tečky s čárkami.
            return decimal.Parse(value, CultureInfo.InvariantCulture);
        }

        // Pro všechny ostatní základní typy (int, bool, long atd.) zkusí univerzální převodník .NETu.
        return Convert.ChangeType(value, actualType, CultureInfo.InvariantCulture);
    }
    //CRUD metody

    /// <summary>
    /// Metoda pro vytvoření Itemu, vrátí null při chybě
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public Item? Create(Item item)
    {
        Item? result = null;
        try
        {
            Items.Add(item);
            Save();
            result = item;
        }
        catch (Exception e)
        {
            Console.WriteLine(
                $"Nastala neočekávaná chyba při vytváření objektu třídy {nameof(Item)}. Zkuste prosím operaci znovu.\n {e.ToString()}");
            //throw; 
        }

        return result;
    }

    /// <summary>
    /// Metoda pro update Itemu, vrátí null při chybě
    /// </summary>
    /// <param name="id"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public Item? Update(int id, Item item)
    {
        if (Get(id) == null)
            return null;

        Item? result = null;
        try
        {
            int index = Items.FindIndex(x => x.Id.Equals(id));
            if (index != -1)
            {
                Items[index] = item;
                Save();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(
                $"Nastala neočekávaná chyba při aktualizaci objektu třídy {nameof(Item)}. Zkuste prosím operaci znovu.\n {e.ToString()}");
            //throw; 
        }

        return result;
    }

    /// <summary>
    /// Metoda pro odstranění Itemu, vrátí null při chybě
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool? Delete(int id)
    {
        if (Get(id) == null)
            return false;

        bool? result = null;
        try
        {
            Item? item = Items.SingleOrDefault(x => x.Id.Equals(id));
            if (item != null)
            {
                Items.Remove(item);
                Save();
            }
            else
            {
                //nenalezeno
                result = false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(
                $"Nastala neočekávaná chyba při vytváření objektu třídy {nameof(Item)}. Zkuste prosím operaci znovu.\n {e.ToString()}");
            //throw; 
        }

        return result;
    }

    /// <summary>
    /// Metoda pro získání Item podle id, vrátí null při nenalezení
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Item? Get(int id) => Items.SingleOrDefault(x => x.Id.Equals(id));

    /// <summary>
    /// Jednoduchá funkce pro inkrementaci Idček.
    /// </summary>
    /// <returns></returns>
    public int NextId() => Items.Count > 0 ? Items[Items.Count - 1].Id + 1 : 1;
}