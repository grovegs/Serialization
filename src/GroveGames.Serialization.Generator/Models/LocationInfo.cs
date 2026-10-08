using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace GroveGames.Serialization.Generator.Models;

internal sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
    public static LocationInfo? From(Location? location)
    {
        if (location == null || location.SourceTree == null)
        {
            return null;
        }

        return new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
    }

    public Location ToLocation()
    {
        return Location.Create(FilePath, Span, LineSpan);
    }
}
