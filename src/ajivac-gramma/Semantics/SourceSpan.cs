namespace ajivac_lib.Semantics;

public struct SourceSpan
{
    public uint Position;
    public uint Length;
    public string Source;
    public string File;

    public static SourceSpan Empty { get; } = new SourceSpan {
        Source = " "
    };

    public string FullLocation()
    {
        //calculate line and column
        var line = 1;
        var iter = Source.AsSpan(0, Position >=Source.Length ? Source.Length : (int)Position )
            .EnumerateLines();

        while (iter.MoveNext())
        {
            line++;
        }
        var column = iter.Current.Length;
        return $"{File}({line},{column})";
    }

    public string GetValue()
    {
        //check bounds
        if(Position >= Source.Length)
            return string.Empty;
        var res =
            Position + Length <= Source.Length ? 
            Source.AsSpan().Slice((int)Position, (int)Length) :
            Source.AsSpan()[(int)Position..];

        //replace \n with \\n
        Span<char> tmp = stackalloc char[res.Length * 2];
        var i = 0;
        foreach (var t in res)
        {
            switch (t)
            {
                case '\n':
                    tmp[i++] = '\\';
                    tmp[i++] = 'n';
                    break;
                case '\r':
                    tmp[i++] = '\\';
                    tmp[i++] = 'r';
                    break;
                default:
                    tmp[i++] = t;
                    break;
            }
        }
        return new string(tmp[..i]);
    }

    public SourceSpan Append(SourceSpan other)
    {
        return this with {
            Length = other.Position + other.Length - Position
        };
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"[{Position}:{Length}] '{GetValue()}'";
    }
}
