using GroveGames.Serialization;

[assembly: FormatterModule(typeof(CoreFormatterModule))]

namespace GroveGames.Serialization;

[Preserve]
internal sealed class CoreFormatterModule : IFormatterModule
{
    [Preserve]
    public CoreFormatterModule()
    {
    }

    public void Register(FormatterRegistrar registrar)
    {
        registrar.AddFormatter(new DataValueFormatter());
        registrar.AddFormatter(new ListFormatter<DataValue>());
    }
}
