using System.Buffers;
using GroveGames.Serialization.MessagePack;

namespace GroveGames.Serialization;

public sealed class MessagePackSerializer : ISerializer, IFormat
{
    private MessagePackStack? _stack;

    public MessagePackSerializer()
    {
        _stack = null;
    }

    public void Serialize<T>(T? value, IBufferWriter<byte> output)
    {
        FormatOperations.Serialize(this, value, output);
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> data)
    {
        return ((IFormat)this).Deserialize<T>(data);
    }

    public int ReadVersion(ReadOnlyMemory<byte> data)
    {
        var stack = RentStack();

        try
        {
            var reader = new MessagePackReader(data, stack);
            reader.TryReadEnvelope(out var version);
            return version;
        }
        finally
        {
            ReturnStack(stack);
        }
    }

    void IFormat.Serialize<T>(T? value, ByteBuffer output) where T : default
    {
        var stack = RentStack();

        try
        {
            var writer = new MessagePackWriter(output, stack);
            Pipeline.Write(ref writer, value);
        }
        finally
        {
            ReturnStack(stack);
        }
    }

    T? IFormat.Deserialize<T>(ReadOnlyMemory<byte> data) where T : default
    {
        var stack = RentStack();

        try
        {
            var reader = new MessagePackReader(data, stack);
            return Pipeline.Read<T, MessagePackReader>(ref reader);
        }
        finally
        {
            ReturnStack(stack);
        }
    }

    void IFormat.Convert<T>(ReadOnlyMemory<byte> data, IFormat target, ByteBuffer output)
    {
        var stack = RentStack();

        try
        {
            var reader = new MessagePackReader(data, stack);
            target.ConvertFrom<T, MessagePackReader>(ref reader, output);
        }
        finally
        {
            ReturnStack(stack);
        }
    }

    void IFormat.ConvertFrom<T, TReader>(ref TReader reader, ByteBuffer output)
    {
        var stack = RentStack();

        try
        {
            var writer = new MessagePackWriter(output, stack);
            Pipeline.Convert<T, TReader, MessagePackWriter>(ref reader, ref writer);
        }
        finally
        {
            ReturnStack(stack);
        }
    }

    private MessagePackStack RentStack()
    {
        return Interlocked.Exchange(ref _stack, null) ?? MessagePackStack.Rent();
    }

    private void ReturnStack(MessagePackStack stack)
    {
        if (Interlocked.CompareExchange(ref _stack, stack, null) != null)
        {
            MessagePackStack.Return(stack);
        }
    }
}
