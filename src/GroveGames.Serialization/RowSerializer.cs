using System.Buffers;
using GroveGames.Serialization.MessagePack;
using GroveGames.Serialization.Rows;

namespace GroveGames.Serialization;

public sealed class RowSerializer
{
    public RowSerializer(FormatterRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        Registry = registry;
    }

    public FormatterRegistry Registry { get; }

    public FieldTable GetLayout<T>()
    {
        var fields = Registry.GetSchema<T>().Fields;
        var names = new string[fields.Count];

        for (var i = 0; i < names.Length; i++)
        {
            names[i] = fields[i].Name;
        }

        return new FieldTable(names);
    }

    public void Serialize<T>(T? value, IBufferWriter<byte> output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var registration = Registry.GetRegistration<T>();
        var buffer = output as ByteBuffer ?? ScratchBuffers.Output();
        var stack = MessagePackStack.Rent();

        try
        {
            var writer = new RowWriter(buffer, stack);
            registration.Formatter.Write(ref writer, value, Registry);
        }
        finally
        {
            MessagePackStack.Return(stack);
        }

        if (!ReferenceEquals(output, buffer))
        {
            output.Write(buffer.WrittenSpan);
        }
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> row, FieldTable layout)
    {
        return Deserialize<T>(row, layout, Registry.GetVersion<T>());
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> row, FieldTable layout, int version)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        var registration = Registry.GetRegistration<T>();

        if (version > registration.Version)
        {
            throw new NotSupportedException($"{typeof(T).Name} data is v{version}, but this build only knows up to v{registration.Version}.");
        }

        var stack = MessagePackStack.Rent();

        try
        {
            var reader = new RowReader(row, layout, stack);
            T? result;

            if (version == registration.Version)
            {
                result = registration.Formatter.Read(ref reader, Registry);
            }
            else
            {
                var value = DataValue.Read(ref reader);
                registration.Migrate(value, version);
                var valueReader = new DataValueReader(value);
                result = registration.Formatter.Read(ref valueReader, Registry);
            }

            reader.EndDocument();
            return result;
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }

    public void SerializeLayout(FieldTable layout, IBufferWriter<byte> output)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(output);
        var buffer = output as ByteBuffer ?? ScratchBuffers.Output();
        var stack = MessagePackStack.Rent();

        try
        {
            var writer = new MessagePackWriter(buffer, stack);
            writer.BeginArray(layout.Count);

            for (var i = 0; i < layout.Count; i++)
            {
                writer.WriteStringUtf8(layout[i]);
            }

            writer.EndArray();
        }
        finally
        {
            MessagePackStack.Return(stack);
        }

        if (!ReferenceEquals(output, buffer))
        {
            output.Write(buffer.WrittenSpan);
        }
    }

    public FieldTable DeserializeLayout(ReadOnlyMemory<byte> data)
    {
        var stack = MessagePackStack.Rent();

        try
        {
            var reader = new MessagePackReader(data, stack);
            var names = new List<string>();
            reader.ReadArrayStart();

            while (reader.TryReadNextElement())
            {
                names.Add(reader.ReadString() ?? throw new FormatException("A column name is null."));
            }

            reader.EndDocument();

            try
            {
                return new FieldTable([.. names]);
            }
            catch (ArgumentException exception)
            {
                throw new FormatException(exception.Message, exception);
            }
        }
        finally
        {
            MessagePackStack.Return(stack);
        }
    }
}
