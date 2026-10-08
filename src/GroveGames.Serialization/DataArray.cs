namespace GroveGames.Serialization;

public sealed class DataArray
{
    private readonly List<DataValue> _items;

    public DataArray()
    {
        _items = [];
    }

    public int Count => _items.Count;

    public DataValue this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }

    public void Add(DataValue item)
    {
        _items.Add(item);
    }

    public void Insert(int index, DataValue item)
    {
        _items.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        _items.RemoveAt(index);
    }
}
