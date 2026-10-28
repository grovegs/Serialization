namespace GroveGames.Serialization.MessagePack;

internal sealed class MessagePackStack
{
    private readonly int[] _positions;
    private readonly int[] _counts;
    private readonly bool[] _isArray;

    public const int MaxDepth = 63;

    [ThreadStatic]
    private static MessagePackStack[]? s_free;

    [ThreadStatic]
    private static int s_freeCount;

    private MessagePackStack()
    {
        _positions = new int[MaxDepth + 1];
        _counts = new int[MaxDepth + 1];
        _isArray = new bool[MaxDepth + 1];
    }

    public int[] Positions => _positions;

    public int[] Counts => _counts;

    public bool[] IsArray => _isArray;

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
