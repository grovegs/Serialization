namespace GroveGames.Serialization;

public static class ConverterExtensions
{
    public static byte[] Convert<T>(this IConverter converter, ReadOnlyMemory<byte> data)
    {
        var output = ScratchBuffers.Output();
        converter.Convert<T>(data, output);
        return output.ToArray();
    }
}
