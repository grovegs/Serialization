using System.Globalization;

namespace GroveGames.Serialization;

public readonly struct DataValue : IEquatable<DataValue>
{
    private readonly long _bits;
    private readonly object? _reference;
    private readonly DataKind _kind;

    private DataValue(DataKind kind, long bits, object? reference)
    {
        _kind = kind;
        _bits = bits;
        _reference = reference;
    }

    public static DataValue Null => default;

    public DataKind Kind => _kind;

    public bool IsNull => _kind == DataKind.Null;

    public long AsInt64 => _kind switch
    {
        DataKind.Integer => _bits,
        DataKind.Float => (long)BitConverter.Int64BitsToDouble(_bits),
        DataKind.Text when long.TryParse((string)_reference!, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) => value,
        _ => throw Mismatch("an integer")
    };

    public double AsDouble => _kind switch
    {
        DataKind.Float => BitConverter.Int64BitsToDouble(_bits),
        DataKind.Integer => _bits,
        DataKind.Text when double.TryParse((string)_reference!, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) => value,
        DataKind.String when TryParseNonFinite((string)_reference!, out var value) => value,
        _ => throw Mismatch("a number")
    };

    public bool AsBool => _kind switch
    {
        DataKind.Bool => _bits != 0,
        DataKind.Text when (string)_reference! == "true" => true,
        DataKind.Text when (string)_reference! == "false" => false,
        _ => throw Mismatch("a bool")
    };

    public string? AsString => _kind switch
    {
        DataKind.String or DataKind.Text => (string)_reference!,
        DataKind.Null => null,
        _ => throw Mismatch("a string")
    };

    public DataArray AsArray => _kind == DataKind.Array ? (DataArray)_reference! : throw Mismatch("an array");

    public DataObject AsObject => _kind == DataKind.Object ? (DataObject)_reference! : throw Mismatch("an object");

    public static DataValue FromBool(bool value)
    {
        return new DataValue(DataKind.Bool, value ? 1 : 0, null);
    }

    public static DataValue FromInteger(long value)
    {
        return new DataValue(DataKind.Integer, value, null);
    }

    public static DataValue FromFloat(double value)
    {
        return new DataValue(DataKind.Float, BitConverter.DoubleToInt64Bits(value), null);
    }

    public static DataValue FromString(string? value)
    {
        return value == null ? default : new DataValue(DataKind.String, 0, value);
    }

    public static DataValue FromText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new DataValue(DataKind.Text, 0, value);
    }

    public static DataValue FromArray(DataArray value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new DataValue(DataKind.Array, 0, value);
    }

    public static DataValue FromObject(DataObject value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new DataValue(DataKind.Object, 0, value);
    }

    public static implicit operator DataValue(bool value)
    {
        return FromBool(value);
    }

    public static implicit operator DataValue(int value)
    {
        return FromInteger(value);
    }

    public static implicit operator DataValue(long value)
    {
        return FromInteger(value);
    }

    public static implicit operator DataValue(float value)
    {
        return FromFloat(value);
    }

    public static implicit operator DataValue(double value)
    {
        return FromFloat(value);
    }

    public static implicit operator DataValue(string? value)
    {
        return FromString(value);
    }

    public static implicit operator DataValue(DataArray value)
    {
        return FromArray(value);
    }

    public static implicit operator DataValue(DataObject value)
    {
        return FromObject(value);
    }

    public static bool operator ==(DataValue left, DataValue right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DataValue left, DataValue right)
    {
        return !left.Equals(right);
    }

    public bool Equals(DataValue other)
    {
        if (_kind != other._kind)
        {
            return false;
        }

        return _kind switch
        {
            DataKind.Null => true,
            DataKind.Bool or DataKind.Integer or DataKind.Float => _bits == other._bits,
            DataKind.String or DataKind.Text => string.Equals((string)_reference!, (string)other._reference!, StringComparison.Ordinal),
            _ => ReferenceEquals(_reference, other._reference)
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is DataValue other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _kind switch
        {
            DataKind.Null => 0,
            DataKind.Bool or DataKind.Integer or DataKind.Float => HashCode.Combine(_kind, _bits),
            DataKind.String or DataKind.Text => HashCode.Combine(_kind, StringComparer.Ordinal.GetHashCode((string)_reference!)),
            _ => HashCode.Combine(_kind, _reference)
        };
    }

    internal static DataValue Read<TReader>(ref TReader reader) where TReader : struct, IFormatReader
    {
        switch (reader.Peek())
        {
            case TokenType.BeginObject:
                var obj = new DataObject();
                reader.ReadObjectStart();

                while (reader.TryReadFieldName(out var name))
                {
                    obj.Add(name, Read(ref reader));
                }

                return FromObject(obj);
            case TokenType.BeginArray:
                var array = new DataArray();
                reader.ReadArrayStart();

                while (reader.TryReadNextElement())
                {
                    array.Add(Read(ref reader));
                }

                return FromArray(array);
            case TokenType.Integer:
                return FromInteger(reader.ReadInt64());
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
                return default;
        }
    }

    private FormatException Mismatch(string expected)
    {
        return new FormatException($"Value is {_kind}, not {expected}.");
    }

    private static bool TryParseNonFinite(string text, out double value)
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
