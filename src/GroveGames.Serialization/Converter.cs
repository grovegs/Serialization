namespace GroveGames.Serialization;

public sealed class Converter : IConverter
{
    private readonly IFormat _from;
    private readonly IFormat _to;
    private readonly SerializerRegistry _registry;

    public Converter(ISerializer from, ISerializer to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        _from = from as IFormat ?? throw new ArgumentException($"{from.GetType()} cannot be converted from.", nameof(from));
        _to = to as IFormat ?? throw new ArgumentException($"{to.GetType()} cannot be converted to.", nameof(to));

        if (!ReferenceEquals(from.Registry, to.Registry))
        {
            throw new ArgumentException("Both serializers must use the same registry.", nameof(to));
        }

        _registry = from.Registry;
    }

    public byte[] Convert<T>(ReadOnlyMemory<byte> data)
    {
        var output = ScratchBuffers.Output();
        _from.Convert<T>(data, _to, output, _registry);
        return output.ToArray();
    }

    public void Convert<T>(ReadOnlyMemory<byte> data, ByteBuffer output)
    {
        ArgumentNullException.ThrowIfNull(output);
        _from.Convert<T>(data, _to, output, _registry);
    }
}
