using System.Globalization;
using System.Reflection;
using System.Text;
using Invoicr.Managers;

namespace Invoicr.Repositories;

public interface IObjectWithId<ID> where ID : notnull
{
    public ID Id { get; set; }
}

public abstract class Repository<Item> where Item : class, IObjectWithId<int>, new()
{
    protected readonly string FullPath;

    public List<Item> Items { get; set; } //schválně neinicializuji, abych měl jistotu že jsou data načtená


    public Repository(string path, bool preload = true)
    {
        //když soubory a nebo cesta neexistuje, tak ji vytvořím (pokud to systém dovolí)
        if (!File.Exists(path))
        {
            //pro jistotu zkusíme vytvořit i složku, pokud už existuje tak se nic nestane
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.Create(path).Close();
        }

        this.FullPath = path;
        if (preload)
            Load();
    }

    //CSV operace repozitáře
    public virtual void Load()
    {
        if (!File.Exists(FullPath)) return;

        //získání netřídových properties generického Itemu (do CSV neukládáme vnořené třídy, cheme objekty typu string, int apod...)
        var properties = GetSimpleProperties();

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
                    object convertedValue = ConvertValue(value, prop.PropertyType);
                    prop.SetValue(item, convertedValue);
                }

                return item;
            })
            .ToList();
    }

    public void Save()
    {
        //získání netřídových properties generického Itemu (do CSV neukládáme vnořené třídy, cheme objekty typu string, int apod...)
        var properties = GetSimpleProperties();

        var lines = new List<string>();

        // 1. Hlavička - názvy vlastností
        lines.Add(CsvManager.Row(properties.Select(p => p.Name).ToArray()));

        // 2. Data
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

    //pomocí LINQ si vytáhnu jednoduché properties objektu a pro jistotu je seřadím, protože to .NET negarantuje.
    // Pomocná metoda pro získání vlastností, které nejsou vnořené třídy
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

    // Formátování pro zápis do CSV
    private string FormatValue(object? val)
    {
        if (val == null) return "";
        if (val is DateTime dt) return dt.ToString("yyyy-MM-dd");
        if (val is IFormattable formattable) return formattable.ToString(null, CultureInfo.InvariantCulture);
        return val.ToString() ?? "";
    }

    // Převod z CSV stringu na typ vlastnosti
    protected object? ConvertValue(string value, Type targetType)
    {
        Type? underlyingType = Nullable.GetUnderlyingType(targetType);
        Type actualType = underlyingType ?? targetType;

        if (string.IsNullOrWhiteSpace(value))
        {
            return underlyingType != null ? null : Activator.CreateInstance(actualType);
        }

        if (actualType == typeof(DateTime))
        {
            // Tady bacha na formát, musí odpovídat FormatValue
            return DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (actualType == typeof(decimal))
        {
            return decimal.Parse(value, CultureInfo.InvariantCulture);
        }

        return Convert.ChangeType(value, actualType, CultureInfo.InvariantCulture);
    }
    //CRUD metody

    //Metoda pro vytvoření Itemu, vrátí null při chybě
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

    //Metoda pro update Itemu, vrátí null při chybě
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

    //Metoda pro odstranění Itemu, vrátí null při chybě
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

    //Metoda pro získání Item podle id, vrátí null při nenalezení
    public Item? Get(int id) => Items.SingleOrDefault(x => x.Id.Equals(id));

    public int NextId() => Items.Count > 0 ? Items[Items.Count - 1].Id + 1 : 1;
}