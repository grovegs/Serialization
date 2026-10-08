using System.Runtime.InteropServices;

namespace GroveGames.Serialization;

internal readonly struct InputSegment
{
    public readonly byte[] Buffer;
    public readonly int Start;
    public readonly int End;

    public InputSegment(ReadOnlyMemory<byte> data)
    {
        if (MemoryMarshal.TryGetArray(data, out var segment) && segment.Array != null)
        {
            Buffer = segment.Array;
            Start = segment.Offset;
            End = segment.Offset + segment.Count;
        }
        else
        {
            Buffer = data.ToArray();
            Start = 0;
            End = Buffer.Length;
        }
    }
}
