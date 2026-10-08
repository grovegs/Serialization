using System.Buffers;

namespace GroveGames.Serialization;

public sealed class Converter : IConverter
{
    private readonly IFormat _from;
    private readonly IFormat _to;

    public Converter(ISerializer from, ISerializer to)
    {
        _from = FormatOperations.GetFormat(from, nameof(from));
        _to = FormatOperations.GetFormat(to, nameof(to));
    }

    public void Convert<T>(ReadOnlyMemory<byte> data, IBufferWriter<byte> output)
    {
        FormatOperations.Convert<T>(_from, _to, data, output);
    }
}
