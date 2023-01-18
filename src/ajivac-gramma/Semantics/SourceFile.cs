namespace ajivac_lib.Semantics;

public record SourceFile(string Text, string Path)
{
    public void Print()
    {
        Console.WriteLine(Text.Replace("\r\n", "\r\n>  "));
    }
    
    public SourceFile(FileInfo file)
        : this(File.ReadAllText(file.FullName), file.FullName)
    {
    }
}
