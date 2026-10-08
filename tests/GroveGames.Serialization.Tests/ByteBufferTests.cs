namespace GroveGames.Serialization.Tests;

public sealed class ByteBufferTests
{
    [Fact]
    public void Write_BeyondCapacity_Grows()
    {
        var buffer = new ByteBuffer(2);

        buffer.Write([1, 2, 3, 4, 5]);

        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, buffer.ToArray());
    }

    [Fact]
    public void Advance_BeyondCapacity_ThrowsArgumentOutOfRangeException()
    {
        var buffer = new ByteBuffer(4);

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Advance(5));
    }

    [Fact]
    public void Reset_AfterWrite_EmptiesBuffer()
    {
        var buffer = new ByteBuffer(4);
        buffer.Write(7);

        buffer.Reset();

        Assert.Equal(0, buffer.Length);
    }
}
