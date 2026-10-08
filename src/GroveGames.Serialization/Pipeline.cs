namespace GroveGames.Serialization;

internal static class Pipeline
{
    public static void Write<T, TWriter>(ref TWriter writer, T? value)
        where TWriter : struct, IDocumentWriter
    {
        var registration = Formatters.GetRegistration<T>();
        var versioned = registration.Version > 1;

        if (versioned)
        {
            writer.BeginEnvelope(registration.Version);
        }

        registration.Formatter.Write(ref writer, value);

        if (versioned)
        {
            writer.EndEnvelope();
        }
    }

    public static T? Read<T, TReader>(ref TReader reader)
        where TReader : struct, IDocumentReader
    {
        var registration = Formatters.GetRegistration<T>();
        var enveloped = reader.TryReadEnvelope(out var stored);
        T? result;

        if (stored == registration.Version)
        {
            result = registration.Formatter.Read(ref reader);
        }
        else if (stored < registration.Version)
        {
            var node = DataValue.Read(ref reader);
            registration.Migrate(node, stored);
            var nodeReader = new DataValueReader(node);
            result = registration.Formatter.Read(ref nodeReader);
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

    public static void Convert<T, TReader, TWriter>(ref TReader reader, ref TWriter writer)
        where TReader : struct, IDocumentReader
        where TWriter : struct, IDocumentWriter
    {
        var registration = Formatters.GetRegistration<T>();
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
            registration.Formatter.Transcode(ref reader, ref writer);
        }
        else
        {
            var node = DataValue.Read(ref reader);
            registration.Migrate(node, stored);
            var nodeReader = new DataValueReader(node);
            registration.Formatter.Transcode(ref nodeReader, ref writer);
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
