using System.Text;

namespace GroveGames.Serialization.Generator;

internal sealed class CodeBuilder
{
    private readonly StringBuilder _builder;
    private int _indent;

    public CodeBuilder()
    {
        _builder = new StringBuilder();
        _indent = 0;
    }

    public CodeBuilder Line(string text = "")
    {
        if (text.Length > 0)
        {
            _builder.Append(' ', _indent * 4);
            _builder.Append(text);
        }

        _builder.Append('\n');
        return this;
    }

    public CodeBuilder Open(string text)
    {
        Line(text);
        Line("{");
        _indent++;
        return this;
    }

    public CodeBuilder Block()
    {
        Line("{");
        _indent++;
        return this;
    }

    public CodeBuilder Close(string suffix = "")
    {
        _indent--;
        return Line("}" + suffix);
    }

    public override string ToString()
    {
        return _builder.ToString();
    }
}
