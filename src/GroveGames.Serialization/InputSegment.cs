using System.Runtime.InteropServices;

namespace GroveGames.Serialization;

internal readonly struct InputSegment
{
    private readonly byte[] _buffer;
    private readonly int _start;
    private readonly int _end;

    public byte[] Buffer => _buffer;
    public int Start => _start;
    public int End => _end;

    public InputSegment(ReadOnlyMemory<byte> data)
    {
        if (MemoryMarshal.TryGetArray(data, out var segment) && segment.Array != null)
        {
            _buffer = segment.Array;
            _start = segment.Offset;
            _end = segment.Offset + segment.Count;
        }
        else
        {
            _buffer = data.ToArray();
            _start = 0;
            _end = Buffer.Length;
        }
    }
}
