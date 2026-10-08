using System.Buffers;
using GroveGames.Serialization.MessagePack;

namespace GroveGames.Serialization;

public sealed class MessagePackSerializer : ISerializer, IFormat
{
    public MessagePackSerializer()
        : this(FormatterRegistry.Default)
    {
    }

    public MessagePackSerializer(FormatterRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        Registry = registry;
    }

    public FormatterRegistry Registry { get; }

    public void Serialize<T>(T? value, IBufferWriter<byte> output)
    {
        FormatOperations.Serialize(this, value, output, Registry);
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> data)
    {
        return ((IFormat)this).Deserialize<T>(data, Registry);
    }

    void IFormat.Serialize<T>(T? value, ByteBuffer output, FormatterRegistry registry) where T : default
    {
        var stack = MessagePackStack.Rent();

        try
        {
            var writer = new MessagePackWriter(output, stack);
            Pipeline.Write(ref writer, value, registry);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }

    T? IFormat.Deserialize<T>(ReadOnlyMemory<byte> data, FormatterRegistry registry) where T : default
    {
        var stack = MessagePackStack.Rent();

        try
        {
            var reader = new MessagePackReader(data, stack);
            return Pipeline.Read<T, MessagePackReader>(ref reader, registry);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }

    void IFormat.Convert<T>(ReadOnlyMemory<byte> data, IFormat target, ByteBuffer output, FormatterRegistry registry)
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

    void IFormat.ConvertFrom<T, TReader>(ref TReader reader, ByteBuffer output, FormatterRegistry registry)
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
