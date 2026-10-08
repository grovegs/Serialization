using System.Buffers;

namespace GroveGames.Serialization;

public sealed class Converter : IConverter
{
    private readonly IFormat _from;
    private readonly IFormat _to;
    private readonly SerializerRegistry _registry;

    public Converter(ISerializer from, ISerializer to)
    {
        _from = FormatOperations.GetFormat(from, nameof(from));
        _to = FormatOperations.GetFormat(to, nameof(to));

        if (!ReferenceEquals(from.Registry, to.Registry))
        {
            throw new ArgumentException("Both serializers must use the same registry.", nameof(to));
        }

        _registry = from.Registry;
    }

    public void Convert<T>(ReadOnlyMemory<byte> data, IBufferWriter<byte> output)
    {
        FormatOperations.Convert<T>(_from, _to, data, output, _registry, _registry.GetVersion<T>(), versioned: false);
    }

    public void Convert<T>(ReadOnlyMemory<byte> data, int version, IBufferWriter<byte> output)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        FormatOperations.Convert<T>(_from, _to, data, output, _registry, version, versioned: false);
    }
}
