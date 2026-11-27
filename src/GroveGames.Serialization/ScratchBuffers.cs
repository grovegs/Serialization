namespace GroveGames.Serialization;

internal static class ScratchBuffers
{
    [ThreadStatic]
    private static ByteBuffer? s_output;

    [ThreadStatic]
    private static bool s_isOutputRented;

    [ThreadStatic]
    private static ByteBuffer? s_input;

    [ThreadStatic]
    private static ByteBuffer? s_text;

    // A serialize started from inside another one, such as by a formatter or a migration, gets its own buffer, so it
    // cannot reset the output the outer call is still writing and back-patching.
    public static ByteBuffer RentOutput()
    {
        if (s_isOutputRented)
        {
            return new ByteBuffer(1024);
        }

        s_isOutputRented = true;
        return Reset(s_output ??= new ByteBuffer(1024));
    }

    public static void ReturnOutput(ByteBuffer buffer)
    {
        if (ReferenceEquals(buffer, s_output))
        {
            s_isOutputRented = false;
        }
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
