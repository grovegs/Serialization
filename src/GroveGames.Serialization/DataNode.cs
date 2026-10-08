using System.Globalization;

namespace GroveGames.Serialization;

public sealed class DataNode
{
    private static readonly List<DataNode> s_noItems = [];
    private static readonly List<DataField> s_noFields = [];

    private readonly NodeKind _kind;
    private readonly long _integer;
    private readonly double _number;
    private readonly string? _text;
    private readonly bool _boolean;
    private readonly List<DataNode>? _items;
    private readonly List<DataField>? _fields;

    private DataNode(NodeKind kind, long integer, double number, string? text, bool boolean, List<DataNode>? items, List<DataField>? fields)
    {
        _kind = kind;
        _integer = integer;
        _number = number;
        _text = text;
        _boolean = boolean;
        _items = items;
        _fields = fields;
    }

    public NodeKind Kind => _kind;

    public IReadOnlyList<DataNode> Items => _items ?? s_noItems;

    public IReadOnlyList<DataField> Fields => _fields ?? s_noFields;

    public long AsInt64 => _kind switch
    {
        NodeKind.Int => _integer,
        NodeKind.Float => (long)_number,
        NodeKind.Text when long.TryParse(_text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) => value,
        _ => throw Mismatch("an integer")
    };

    public double AsDouble => _kind switch
    {
        NodeKind.Float => _number,
        NodeKind.Int => _integer,
        NodeKind.Text when double.TryParse(_text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) => value,
        NodeKind.String when TryParseNonFinite(_text, out var value) => value,
        _ => throw Mismatch("a number")
    };

    public bool AsBool => _kind switch
    {
        NodeKind.Bool => _boolean,
        NodeKind.Text when _text == "true" => true,
        NodeKind.Text when _text == "false" => false,
        _ => throw Mismatch("a bool")
    };

    public string? AsString => _kind switch
    {
        NodeKind.String or NodeKind.Text => _text,
        NodeKind.Null => null,
        _ => throw Mismatch("a string")
    };

    public static DataNode Null()
    {
        return new DataNode(NodeKind.Null, 0, 0, null, false, null, null);
    }

    public static DataNode FromBool(bool value)
    {
        return new DataNode(NodeKind.Bool, 0, 0, null, value, null, null);
    }

    public static DataNode FromInt(long value)
    {
        return new DataNode(NodeKind.Int, value, 0, null, false, null, null);
    }

    public static DataNode FromFloat(double value)
    {
        return new DataNode(NodeKind.Float, 0, value, null, false, null, null);
    }

    public static DataNode FromString(string? value)
    {
        return value == null ? Null() : new DataNode(NodeKind.String, 0, 0, value, false, null, null);
    }

    public static DataNode FromText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new DataNode(NodeKind.Text, 0, 0, value, false, null, null);
    }

    public static DataNode NewArray()
    {
        return new DataNode(NodeKind.Array, 0, 0, null, false, [], null);
    }

    public static DataNode NewObject()
    {
        return new DataNode(NodeKind.Object, 0, 0, null, false, null, []);
    }

    public DataNode? this[string name]
    {
        get
        {
            var index = Find(name);
            return index >= 0 ? ObjectFields[index].Value : null;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            var field = new DataField(name, value);
            var index = Find(name);

            if (index >= 0)
            {
                ObjectFields[index] = field;
            }
            else
            {
                ObjectFields.Add(field);
            }
        }
    }

    public bool Has(string name)
    {
        return Find(name) >= 0;
    }

    public void Remove(string name)
    {
        var index = Find(name);

        if (index >= 0)
        {
            ObjectFields.RemoveAt(index);
        }
    }

    public void Rename(string from, string to)
    {
        var index = Find(from);

        if (index >= 0)
        {
            ObjectFields[index] = new DataField(to, ObjectFields[index].Value);
        }
    }

    public void Add(DataNode item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArrayItems.Add(item);
    }

    internal static DataNode Read<TReader>(ref TReader reader) where TReader : struct, IFormatReader
    {
        switch (reader.Peek())
        {
            case TokenType.BeginObject:
                var node = NewObject();
                reader.ReadObjectStart();

                while (reader.TryReadFieldName(out var name))
                {
                    node.ObjectFields.Add(new DataField(name, Read(ref reader)));
                }

                return node;
            case TokenType.BeginArray:
                var array = NewArray();
                reader.ReadArrayStart();

                while (reader.TryReadNextElement())
                {
                    array.ArrayItems.Add(Read(ref reader));
                }

                return array;
            case TokenType.Integer:
                return FromInt(reader.ReadInt64());
            case TokenType.Float:
                return FromFloat(reader.ReadDouble());
            case TokenType.String:
                return FromString(reader.ReadString());
            case TokenType.Text:
                return FromText(reader.ReadString() ?? string.Empty);
            case TokenType.Bool:
                return FromBool(reader.ReadBool());
            default:
                reader.Skip();
                return Null();
        }
    }

    private List<DataField> ObjectFields => _fields ?? throw Mismatch("an object");

    private List<DataNode> ArrayItems => _items ?? throw Mismatch("an array");

    private int Find(string name)
    {
        var fields = ObjectFields;

        for (var i = 0; i < fields.Count; i++)
        {
            if (string.Equals(fields[i].Name, name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private FormatException Mismatch(string expected)
    {
        return new FormatException($"Node is {_kind}, not {expected}.");
    }

    private static bool TryParseNonFinite(string? text, out double value)
    {
        switch (text)
        {
            case "NaN":
                value = double.NaN;
                return true;
            case "Infinity":
                value = double.PositiveInfinity;
                return true;
            case "-Infinity":
                value = double.NegativeInfinity;
                return true;
            default:
                value = 0;
                return false;
        }
    }
}
