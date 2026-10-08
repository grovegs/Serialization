using GroveGames.Serialization.MessagePack;

namespace GroveGames.Serialization;

public sealed class MessagePackSerializer : ISerializer, IFormat
{
    public MessagePackSerializer(SerializerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        Registry = registry;
    }

    public SerializerRegistry Registry { get; }

    public byte[] Serialize<T>(T? value)
    {
        return SerializerStreams.Serialize(this, value);
    }

    public void Serialize<T>(T? value, ByteBuffer output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var stack = MessagePackStack.Rent();

        try
        {
            var writer = new MessagePackWriter(output, stack);
            Pipeline.Write(ref writer, value, Registry);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }

    public void Serialize<T>(T? value, Stream output)
    {
        SerializerStreams.Serialize(this, value, output);
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> data)
    {
        var stack = MessagePackStack.Rent();

        try
        {
            var reader = new MessagePackReader(data, stack);
            return Pipeline.Read<T, MessagePackReader>(ref reader, Registry);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }

    public T? Deserialize<T>(Stream input)
    {
        return SerializerStreams.Deserialize<T>(this, input);
    }

    void IFormat.Convert<T>(ReadOnlyMemory<byte> data, IFormat target, ByteBuffer output, SerializerRegistry registry)
    {
        var stack = MessagePackStack.Rent();

        try
        {
            var reader = new MessagePackReader(data, stack);
            target.ConvertFrom<T, MessagePackReader>(ref reader, output, registry);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }

    void IFormat.ConvertFrom<T, TReader>(ref TReader reader, ByteBuffer output, SerializerRegistry registry)
    {
        var stack = MessagePackStack.Rent();

        try
        {
            var writer = new MessagePackWriter(output, stack);
            Pipeline.Convert<T, TReader, MessagePackWriter>(ref reader, ref writer, registry);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }
}
