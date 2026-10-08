namespace GroveGames.Serialization.MessagePack;

internal sealed class MessagePackStack
{
    public const int MaxDepth = 63;

    [ThreadStatic]
    private static MessagePackStack[]? s_free;

    [ThreadStatic]
    private static int s_freeCount;

    private MessagePackStack()
    {
        Positions = new int[MaxDepth + 1];
        Counts = new int[MaxDepth + 1];
        IsArray = new bool[MaxDepth + 1];
    }

    public int[] Positions { get; }

    public int[] Counts { get; }

    public bool[] IsArray { get; }

    public static MessagePackStack Rent()
    {
        if (s_freeCount > 0)
        {
            return s_free![--s_freeCount];
        }

        return new MessagePackStack();
    }

    public static void Return(MessagePackStack stack)
    {
        var free = s_free ??= new MessagePackStack[4];

        for (var i = 0; i < s_freeCount; i++)
        {
            if (ReferenceEquals(free[i], stack))
            {
                return;
            }
        }

        if (s_freeCount < free.Length)
        {
            free[s_freeCount++] = stack;
        }
    }
}
