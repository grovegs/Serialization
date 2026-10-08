namespace GroveGames.Serialization;

internal static class Pipeline
{
    public static void Write<T, TWriter>(ref TWriter writer, T? value, SerializerRegistry registry)
        where TWriter : struct, IFormatWriter
    {
        var registration = registry.GetRegistration<T>();
        writer.BeginEnvelope(registration.Version);
        registration.Formatter.Write(ref writer, value, registry);
        writer.EndEnvelope();
    }

    public static T? Read<T, TReader>(ref TReader reader, SerializerRegistry registry)
        where TReader : struct, IFormatReader
    {
        var registration = registry.GetRegistration<T>();
        var stored = reader.ReadEnvelope();
        T? result;

        if (stored == registration.Version)
        {
            result = registration.Formatter.Read(ref reader, registry);
        }
        else if (stored < registration.Version)
        {
            var node = DataNode.Read(ref reader);
            registration.Migrate(node, stored);
            var nodeReader = new DataNodeReader(node);
            result = registration.Formatter.Read(ref nodeReader, registry);
        }
        else
        {
            throw Newer<T>(stored, registration.Version);
        }

        reader.EndEnvelope();
        return result;
    }

    public static void Convert<T, TReader, TWriter>(ref TReader reader, ref TWriter writer, SerializerRegistry registry)
        where TReader : struct, IFormatReader
        where TWriter : struct, IFormatWriter
    {
        var registration = registry.GetRegistration<T>();
        var stored = reader.ReadEnvelope();

        if (stored > registration.Version)
        {
            throw Newer<T>(stored, registration.Version);
        }

        writer.BeginEnvelope(registration.Version);

        if (stored == registration.Version)
        {
            registration.Formatter.Transcode(ref reader, ref writer, registry);
        }
        else
        {
            var node = DataNode.Read(ref reader);
            registration.Migrate(node, stored);
            var nodeReader = new DataNodeReader(node);
            registration.Formatter.Transcode(ref nodeReader, ref writer, registry);
        }

        writer.EndEnvelope();
        reader.EndEnvelope();
    }

    private static NotSupportedException Newer<T>(int stored, int current)
    {
        return new NotSupportedException($"{typeof(T).Name} data is v{stored}, but this build only knows up to v{current}.");
    }
}
