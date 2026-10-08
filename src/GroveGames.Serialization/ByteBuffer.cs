using System.Buffers;

namespace GroveGames.Serialization;

public sealed class ByteBuffer : IBufferWriter<byte>
{
    private byte[] _buffer;
    private int _length;

    public ByteBuffer() : this(256)
    {
    }

    public ByteBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _buffer = new byte[capacity];
        _length = 0;
    }

    public int Length => _length;

    public ReadOnlySpan<byte> WrittenSpan => new(_buffer, 0, _length);

    public ReadOnlyMemory<byte> WrittenMemory => new(_buffer, 0, _length);

    public void Reset()
    {
        _length = 0;
    }

    public byte[] ToArray()
    {
        return WrittenSpan.ToArray();
    }

    public Span<byte> Take(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        EnsureCapacity(count);
        var span = new Span<byte>(_buffer, _length, count);
        _length += count;
        return span;
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);
        EnsureCapacity(Math.Max(sizeHint, 1));
        return new Span<byte>(_buffer, _length, _buffer.Length - _length);
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);
        EnsureCapacity(Math.Max(sizeHint, 1));
        return new Memory<byte>(_buffer, _length, _buffer.Length - _length);
    }

    public void Advance(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, _buffer.Length - _length);
        _length += count;
    }

    public void Write(byte value)
    {
        EnsureCapacity(1);
        _buffer[_length++] = value;
    }

    public void Write(ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(Take(bytes.Length));
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        stream.Write(_buffer, 0, _length);
    }

    public void ReadFrom(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        while (true)
        {
            var read = stream.Read(GetSpan(4096));

            if (read <= 0)
            {
                return;
            }

            _length += read;
        }
    }

    internal Span<byte> Slice(int start, int length)
    {
        return new Span<byte>(_buffer, start, length);
    }

    private void EnsureCapacity(int count)
    {
        if (_length + count > _buffer.Length)
        {
            Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, _length + count));
        }
    }
}
