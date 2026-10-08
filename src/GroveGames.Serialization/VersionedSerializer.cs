using System.Buffers;

namespace GroveGames.Serialization;

public sealed class VersionedSerializer : IVersionedSerializer
{
    public VersionedSerializer(ISerializer serializer)
    {
        Format = FormatOperations.GetFormat(serializer, nameof(serializer));
        Registry = serializer.Registry;
    }

    public SerializerRegistry Registry { get; }

    internal IFormat Format { get; }

    public void Serialize<T>(T? value, IBufferWriter<byte> output)
    {
        FormatOperations.Serialize(Format, value, output, Registry, versioned: true);
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> data)
    {
        return Format.Deserialize<T>(data, Registry, version: null);
    }
}
