namespace GroveGames.Serialization;

public sealed class DataObject
{
    private readonly List<DataField> _fields;

    public DataObject()
    {
        _fields = [];
    }

    public int Count => _fields.Count;

    public DataField this[int index] => _fields[index];

    public DataValue this[string name]
    {
        get
        {
            var index = IndexOf(name);
            return index >= 0 ? _fields[index].Value : default;
        }
        set
        {
            var index = IndexOf(name);

            if (index >= 0)
            {
                _fields[index] = new DataField(name, value);
            }
            else
            {
                Add(name, value);
            }
        }
    }

    public bool Contains(string name)
    {
        return IndexOf(name) >= 0;
    }

    public bool TryGetValue(string name, out DataValue value)
    {
        var index = IndexOf(name);

        if (index < 0)
        {
            value = default;
            return false;
        }

        value = _fields[index].Value;
        return true;
    }

    public bool Remove(string name)
    {
        var index = IndexOf(name);

        if (index < 0)
        {
            return false;
        }

        _fields.RemoveAt(index);
        return true;
    }

    public bool Rename(string from, string to)
    {
        ArgumentException.ThrowIfNullOrEmpty(to);
        var index = IndexOf(from);

        if (index < 0)
        {
            return false;
        }

        _fields[index] = new DataField(to, _fields[index].Value);
        return true;
    }

    internal void Add(string name, DataValue value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        _fields.Add(new DataField(name, value));
    }

    private int IndexOf(string name)
    {
        for (var i = 0; i < _fields.Count; i++)
        {
            if (string.Equals(_fields[i].Name, name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}
