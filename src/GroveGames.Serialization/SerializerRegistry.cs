using System.Diagnostics.CodeAnalysis;

namespace GroveGames.Serialization;

public sealed class SerializerRegistry
{
    private readonly Dictionary<Type, object> _registrations;

    internal SerializerRegistry(Dictionary<Type, object> registrations)
    {
        _registrations = registrations;
    }

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
