namespace ajivac_lib.Semantics;

public record SourceFile(string Text, string Path)
{
    public void Print(TextWriter writer)
    {
        writer.WriteLine(Text.Replace("\r\n", "\r\n>  "));
    }
    
    public SourceFile(FileInfo file)
        : this(File.ReadAllText(file.FullName), file.FullName)
    {
    }
}
