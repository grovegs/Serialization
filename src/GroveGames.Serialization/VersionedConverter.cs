using System.Buffers;

namespace GroveGames.Serialization;

public sealed class VersionedConverter : IConverter
{
    private readonly IFormat _from;
    private readonly IFormat _to;
    private readonly SerializerRegistry _registry;

    public VersionedConverter(IVersionedSerializer from, IVersionedSerializer to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        _from = (from as VersionedSerializer)?.Format ?? throw new ArgumentException($"{from.GetType()} is not a built-in serializer.", nameof(from));
        _to = (to as VersionedSerializer)?.Format ?? throw new ArgumentException($"{to.GetType()} is not a built-in serializer.", nameof(to));

        if (!ReferenceEquals(from.Registry, to.Registry))
        {
            throw new ArgumentException("Both serializers must use the same registry.", nameof(to));
        }

        _registry = from.Registry;
    }

    public void Convert<T>(ReadOnlyMemory<byte> data, IBufferWriter<byte> output)
    {
        FormatOperations.Convert<T>(_from, _to, data, output, _registry, version: null, versioned: true);
    }
}
