using System.Globalization;
using System.Reflection;
using System.Text;
using Invoicr.Managers;

namespace Invoicr.Repositories;

public interface IObjectWithId<ID> where ID : notnull
{
    public ID Id { get; set; }
}

public abstract class Repository<Item, ID> where Item : class, IObjectWithId<ID>, new() where ID : notnull
{
    private readonly string Path;

    public List<Item> Items { get; private set; } = new();

    public Repository(string path)
    {
        //if (!Directory.Exists(path))
        //{
        //    //reálně vytvářím CSV, tahle metoda by překousla vytvoření složky pokud už existuje, ale jedná se o CSV a tam to padá na vyjímce.
        //    Directory.CreateDirectory(path);
        //}

        this.Path = path;
        Load();
    }

    //CSV operace repozitáře
    public void Load()
    {
        if (!File.Exists(Path)) return;

        //získání netřídových properties generického Itemu (do CSV neukládáme vnořené třídy, cheme objekty typu string, int apod...)
        var properties = GetSimpleProperties();

        Items = File.ReadAllLines(Path, Encoding.UTF8)
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

        File.WriteAllLines(Path, lines, Encoding.UTF8);
    }

    // Pomocná metoda pro získání vlastností, které nejsou vnořené třídy
    private PropertyInfo[] GetSimpleProperties()
    {
        //pomocí LINQ si vytáhnu jednoduché properties objektu a pro jistotu je seřadím, protože to .NET negarantuje.
        return typeof(Item).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(x => x.Name)
            .Where(p => p.PropertyType.IsPrimitive ||
                        p.PropertyType == typeof(string) ||
                        p.PropertyType == typeof(decimal) ||
                        p.PropertyType == typeof(DateTime))
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
    private object ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(DateTime))
            return DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        if (targetType == typeof(decimal))
            return decimal.Parse(value, CultureInfo.InvariantCulture);

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
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
    public Item? Update(ID id, Item item)
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
    public bool? Delete(ID id)
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
    public Item? Get(ID id) => Items.SingleOrDefault(x => x.Id.Equals(id));

    public ID NextId() => Items.Count > 0 ? Items[Items.Count - 1].Id : default!;
}