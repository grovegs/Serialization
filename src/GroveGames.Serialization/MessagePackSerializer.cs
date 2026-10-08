using System.Buffers;
using GroveGames.Serialization.MessagePack;

namespace GroveGames.Serialization;

public sealed class MessagePackSerializer : ISerializer, IFormat
{
    public void Serialize<T>(T? value, IBufferWriter<byte> output)
    {
        FormatOperations.Serialize(this, value, output);
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> data)
    {
        return ((IFormat)this).Deserialize<T>(data);
    }

    void IFormat.Serialize<T>(T? value, ByteBuffer output) where T : default
    {
        var stack = MessagePackStack.Rent();

        try
        {
            var writer = new MessagePackWriter(output, stack);
            Pipeline.Write(ref writer, value);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }

    T? IFormat.Deserialize<T>(ReadOnlyMemory<byte> data) where T : default
    {
        var stack = MessagePackStack.Rent();

        try
        {
            var reader = new MessagePackReader(data, stack);
            return Pipeline.Read<T, MessagePackReader>(ref reader);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }

    void IFormat.Convert<T>(ReadOnlyMemory<byte> data, IFormat target, ByteBuffer output)
    {
        var stack = MessagePackStack.Rent();

        try
        {
            var reader = new MessagePackReader(data, stack);
            target.ConvertFrom<T, MessagePackReader>(ref reader, output);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }

    void IFormat.ConvertFrom<T, TReader>(ref TReader reader, ByteBuffer output)
    {
        var stack = MessagePackStack.Rent();

        try
        {
            var writer = new MessagePackWriter(output, stack);
            Pipeline.Convert<T, TReader, MessagePackWriter>(ref reader, ref writer);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }
}
