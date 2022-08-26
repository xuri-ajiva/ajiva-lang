namespace ajivac_lib;

public struct SourceSpan
{
    public uint Position;
    public uint Length;
    public string Source;
    public static SourceSpan Empty { get; } = new SourceSpan { Source = " " };

    public string GetValue()
    {
        var res = Source.AsSpan().Slice((int)Position, (int)Length);
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
