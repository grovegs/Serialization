namespace GroveGames.Serialization;

internal static class Pipeline
{
    public static void Write<T, TWriter>(ref TWriter writer, T? value, FormatterRegistry registry)
        where TWriter : struct, IDocumentWriter
    {
        var registration = registry.GetRegistration<T>();
        var versioned = registration.Version > 1;

        if (versioned)
        {
            writer.BeginEnvelope(registration.Version);
        }

        registration.Formatter.Write(ref writer, value, registry);

        if (versioned)
        {
            writer.EndEnvelope();
        }
    }

    public static T? Read<T, TReader>(ref TReader reader, FormatterRegistry registry)
        where TReader : struct, IDocumentReader
    {
        var registration = registry.GetRegistration<T>();
        var enveloped = reader.TryReadEnvelope(out var stored);
        T? result;

        if (stored == registration.Version)
        {
            result = registration.Formatter.Read(ref reader, registry);
        }
        else if (stored < registration.Version)
        {
            var node = DataValue.Read(ref reader);
            registration.Migrate(node, stored);
            var nodeReader = new DataValueReader(node);
            result = registration.Formatter.Read(ref nodeReader, registry);
        }
        else
        {
            throw Newer<T>(stored, registration.Version);
        }

        if (enveloped)
        {
            reader.EndEnvelope();
        }

        reader.EndDocument();
        return result;
    }

    public static void Convert<T, TReader, TWriter>(ref TReader reader, ref TWriter writer, FormatterRegistry registry)
        where TReader : struct, IDocumentReader
        where TWriter : struct, IDocumentWriter
    {
        var registration = registry.GetRegistration<T>();
        var enveloped = reader.TryReadEnvelope(out var stored);
        var versioned = registration.Version > 1;

        if (stored > registration.Version)
        {
            throw Newer<T>(stored, registration.Version);
        }

        if (versioned)
        {
            writer.BeginEnvelope(registration.Version);
        }

        if (stored == registration.Version)
        {
            registration.Formatter.Transcode(ref reader, ref writer, registry);
        }
        else
        {
            var node = DataValue.Read(ref reader);
            registration.Migrate(node, stored);
            var nodeReader = new DataValueReader(node);
            registration.Formatter.Transcode(ref nodeReader, ref writer, registry);
        }

        if (versioned)
        {
            writer.EndEnvelope();
        }

        if (enveloped)
        {
            reader.EndEnvelope();
        }

        reader.EndDocument();
    }

    private static NotSupportedException Newer<T>(int stored, int current)
    {
        return new NotSupportedException($"{typeof(T).Name} data is v{stored}, but this build only knows up to v{current}.");
    }
}
