namespace GroveGames.Serialization;

internal static class ScratchBuffers
{
    [ThreadStatic]
    private static ByteBuffer? s_output;

    [ThreadStatic]
    private static ByteBuffer? s_input;

    [ThreadStatic]
    private static ByteBuffer? s_text;

    public static ByteBuffer Output()
    {
        return Reset(s_output ??= new ByteBuffer(1024));
    }

    public static ByteBuffer Input()
    {
        return Reset(s_input ??= new ByteBuffer(4096));
    }

    public static ByteBuffer Text()
    {
        return Reset(s_text ??= new ByteBuffer(256));
    }

    private static ByteBuffer Reset(ByteBuffer buffer)
    {
        buffer.Reset();
        return buffer;
    }
}
