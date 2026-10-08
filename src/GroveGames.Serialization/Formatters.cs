using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace GroveGames.Serialization;

public static class Formatters
{
    private static readonly object s_lock = new();
    private static readonly HashSet<Assembly> s_loaded = [];
    private static bool s_scanned;

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

    internal static TypeRegistration<T> GetRegistration<T>()
    {
        return Volatile.Read(ref FormatterCache<T>.Registration) ?? Load<T>() ?? throw new InvalidOperationException($"No formatter is registered for {typeof(T)}. Mark it with [Schema], or mark a formatter for it with [Formatter].");
    }

    internal static void Load(Assembly assembly)
    {
        lock (s_lock)
        {
            LoadAssembly(assembly);
        }
    }

    internal static void Register(Action<FormatterRegistrar> configure)
    {
        lock (s_lock)
        {
            var registrar = new FormatterRegistrar();
            configure(registrar);
            registrar.Commit();
        }
    }

    private static TypeRegistration<T>? Load<T>()
    {
        lock (s_lock)
        {
            LoadAssemblies(typeof(T));

            if (FormatterCache<T>.Registration == null && !s_scanned)
            {
                s_scanned = true;
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Array.Sort(assemblies, static (left, right) => string.CompareOrdinal(left.FullName, right.FullName));

                foreach (var assembly in assemblies)
                {
                    LoadAssembly(assembly);
                }
            }

            return FormatterCache<T>.Registration;
        }
    }

    private static void LoadAssemblies(Type type)
    {
        LoadAssembly(type.Assembly);

        if (type.IsArray)
        {
            LoadAssemblies(type.GetElementType()!);
        }
        else if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                LoadAssemblies(argument);
            }
        }
    }

    private static void LoadAssembly(Assembly assembly)
    {
        if (assembly.IsDynamic || !s_loaded.Add(assembly))
        {
            return;
        }

        foreach (var attribute in assembly.GetCustomAttributes<FormatterModuleAttribute>())
        {
            if (Activator.CreateInstance(attribute.Type) is not IFormatterModule module)
            {
                throw new InvalidOperationException($"{attribute.Type} in {assembly.GetName().Name} is not an {nameof(IFormatterModule)}.");
            }

            var registrar = new FormatterRegistrar();
            module.Register(registrar);

            try
            {
                registrar.Commit();
            }
            catch (InvalidOperationException exception)
            {
                throw new InvalidOperationException($"{assembly.GetName().Name}: {exception.Message}", exception);
            }
        }
    }
}
