using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GroveGames.Serialization;

public static class Formatters
{
    private static readonly object s_lock = new();
    private static bool s_coreRegistered;

    public static IFormatter<T> Get<T>()
    {
        return GetRegistration<T>().Formatter;
    }

    public static bool TryGet<T>([NotNullWhen(true)] out IFormatter<T>? formatter)
    {
        formatter = (Volatile.Read(ref FormatterCache<T>.Registration) ?? Load<T>())?.Formatter;
        return formatter != null;
    }

    public static int GetVersion<T>()
    {
        return GetRegistration<T>().Version;
    }

    public static TypeSchema GetSchema<T>()
    {
        return TryGetSchema<T>(out var schema) ? schema : throw new InvalidOperationException($"The formatter for {typeof(T)} has no schema.");
    }

    public static bool TryGetSchema<T>([NotNullWhen(true)] out TypeSchema? schema)
    {
        schema = TryGet<T>(out var formatter) && formatter is ISchemaFormatter<T> schemaFormatter ? schemaFormatter.Schema : null;
        return schema != null;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Register(Action<FormatterRegistrar> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        lock (s_lock)
        {
            var registrar = new FormatterRegistrar();
            configure(registrar);
            registrar.Commit();
        }
    }

    internal static TypeRegistration<T> GetRegistration<T>()
    {
        return Volatile.Read(ref FormatterCache<T>.Registration) ?? Load<T>() ?? throw Missing<T>();
    }

    private static TypeRegistration<T>? Load<T>()
    {
        lock (s_lock)
        {
            if (!s_coreRegistered)
            {
                s_coreRegistered = true;
                var registrar = new FormatterRegistrar();
                registrar.AddFormatter(new DataValueFormatter());
                registrar.AddFormatter(new ListFormatter<DataValue>());
                registrar.AddFormatter(new GuidFormatter());
                registrar.AddFormatter(new ListFormatter<Guid>());
                registrar.AddFormatter(new DateTimeFormatter());
                registrar.AddFormatter(new ListFormatter<DateTime>());
                registrar.AddFormatter(new DateTimeOffsetFormatter());
                registrar.AddFormatter(new ListFormatter<DateTimeOffset>());
                registrar.AddFormatter(new TimeSpanFormatter());
                registrar.AddFormatter(new ListFormatter<TimeSpan>());
                registrar.Commit();
            }
        }

        if (Volatile.Read(ref FormatterCache<T>.Registration) == null)
        {
            InitializeAssemblies(typeof(T));
        }

        return Volatile.Read(ref FormatterCache<T>.Registration);
    }

    private static void InitializeAssemblies(Type type)
    {
        RuntimeHelpers.RunModuleConstructor(type.Assembly.ManifestModule.ModuleHandle);

        if (type.IsArray)
        {
            InitializeAssemblies(type.GetElementType()!);
        }
        else if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                InitializeAssemblies(argument);
            }
        }
    }

    private static InvalidOperationException Missing<T>()
    {
        var type = typeof(T);
        var hint = type.Namespace != null && (type.Namespace.StartsWith("UnityEngine", StringComparison.Ordinal) || type.Namespace.StartsWith("Unity.Mathematics", StringComparison.Ordinal))
            ? " Install com.grovegames.serialization for Unity type formatters."
            : " Mark it with [Schema], or mark a formatter for it with [Formatter].";
        return new InvalidOperationException($"No formatter is registered for {type}.{hint}");
    }
}
