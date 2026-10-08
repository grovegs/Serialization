using System.Buffers;
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

    public void Serialize<T>(T? value, IBufferWriter<byte> output)
    {
        FormatOperations.Serialize(this, value, output, Registry, versioned: false);
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> data)
    {
        return ((IFormat)this).Deserialize<T>(data, Registry, Registry.GetVersion<T>());
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> data, int version)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        return ((IFormat)this).Deserialize<T>(data, Registry, version);
    }

    void IFormat.Serialize<T>(T? value, ByteBuffer output, SerializerRegistry registry, bool versioned) where T : default
    {
        var stack = MessagePackStack.Rent();

        try
        {
            var writer = new MessagePackWriter(output, stack);
            Pipeline.Write(ref writer, value, registry, versioned);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }

    T? IFormat.Deserialize<T>(ReadOnlyMemory<byte> data, SerializerRegistry registry, int? version) where T : default
    {
        var stack = MessagePackStack.Rent();

        try
        {
            var reader = new MessagePackReader(data, stack);
            return Pipeline.Read<T, MessagePackReader>(ref reader, registry, version);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }

    void IFormat.Convert<T>(ReadOnlyMemory<byte> data, IFormat target, ByteBuffer output, SerializerRegistry registry, int? version, bool versioned)
    {
        var stack = MessagePackStack.Rent();

        try
        {
            var reader = new MessagePackReader(data, stack);
            target.ConvertFrom<T, MessagePackReader>(ref reader, output, registry, version, versioned);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }

    void IFormat.ConvertFrom<T, TReader>(ref TReader reader, ByteBuffer output, SerializerRegistry registry, int? version, bool versioned)
    {
        var stack = MessagePackStack.Rent();

        try
        {
            var writer = new MessagePackWriter(output, stack);
            Pipeline.Convert<T, TReader, MessagePackWriter>(ref reader, ref writer, registry, version, versioned);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }
}
