namespace GroveGames.Serialization;

public static class ConverterExtensions
{
    public static byte[] Convert<T>(this IConverter converter, ReadOnlyMemory<byte> data)
    {
        var output = ScratchBuffers.RentOutput();

        try
        {
            converter.Convert<T>(data, output);
            return output.ToArray();
        }
        finally
        {
            ScratchBuffers.ReturnOutput(output);
        }
    }
}
