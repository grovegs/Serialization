using System.Diagnostics.CodeAnalysis;

namespace GroveGames.Serialization;

public sealed class FormatterRegistry
{
    private static readonly Lazy<FormatterRegistry> s_default = new(static () => new FormatterRegistryBuilder().AddFormatters().Build());

    private readonly Dictionary<Type, object> _registrations;

    internal FormatterRegistry(Dictionary<Type, object> registrations)
    {
        _registrations = registrations;
    }

    public static FormatterRegistry Default => s_default.Value;

    public IFormatter<T> GetFormatter<T>()
    {
        return GetRegistration<T>().Formatter;
    }

    public bool TryGetFormatter<T>([NotNullWhen(true)] out IFormatter<T>? formatter)
    {
        if (_registrations.TryGetValue(typeof(T), out var registration))
        {
            formatter = ((TypeRegistration<T>)registration).Formatter;
            return true;
        }

        formatter = null;
        return false;
    }

    public TypeSchema GetSchema<T>()
    {
        return TryGetSchema<T>(out var schema) ? schema : throw new InvalidOperationException($"The formatter for {typeof(T)} has no schema.");
    }

    public bool TryGetSchema<T>([NotNullWhen(true)] out TypeSchema? schema)
    {
        if (_registrations.TryGetValue(typeof(T), out var registration) && ((TypeRegistration<T>)registration).Formatter is ISchemaFormatter<T> schemaFormatter)
        {
            schema = schemaFormatter.Schema;
            return true;
        }

        schema = null;
        return false;
    }

    public int GetVersion<T>()
    {
        return GetRegistration<T>().Version;
    }

    internal TypeRegistration<T> GetRegistration<T>()
    {
        if (_registrations.TryGetValue(typeof(T), out var registration))
        {
            return (TypeRegistration<T>)registration;
        }

        throw new InvalidOperationException($"No formatter is registered for {typeof(T)}.");
    }
}
