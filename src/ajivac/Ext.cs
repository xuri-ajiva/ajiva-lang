using System.Diagnostics;
using ajivac_il;
using ajivac_lib;
using ajivac_lib.AST;
using ajivac_lib.ContextualAnalyzer.HelperStructs;
using ajivac_lib.Semantics;
using Spectre.Console;

public static class Ext
{
    public static void Statistics(this SourceFile src)
    {
        AnsiConsole.MarkupLine($"[green]Statistics[/] for {src.Path}");
        AnsiConsole.MarkupLine($"[gray]Bytes[/]: {src.Text.Length}");
    }

    public static SourceFile SourceFile(string path, out string name)
    {
        var source = new SourceFile(File.ReadAllText(path), path);
        name = Path.GetFileNameWithoutExtension(path);
        return source;
    }

    public static void Print(this SourceFile source)
    {
        AnsiConsole.Write(new Panel(new Text(source.Text).Fold())
            .Header("Source"));
    }

    public static void RunWithOutput(this ILCodeGenerator ilGenerator, string name)
    {
        AnsiConsole.MarkupLine($"[green]Running[/] {name}");
        try
        {
            var start = Start();
            ilGenerator.Run();
            StopAndPrint(start, "Running");
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
        }
        finally
        {
            AnsiConsole.MarkupLine($"[green]Finished[/] {name}");
        }
    }

    public static void CompileWithOutput(this Compiler compiler, string name)
    {
        AnsiConsole.Status().AutoRefresh(true)
            .Spinner(Spinner.Known.Dots).Start("Compiling..."
                , ctx =>
                {
                    AnsiConsole.MarkupLine($"[green]Compiling {name}[/]");
                    ctx.Status("Parsing...");
                    var start = Start();
                    compiler.ParseAll();
                    StopAndPrint(start, "Parsing");
                    ctx.Status("Semantic Analysis...");
                    start = Start();
                    compiler.Analyze();
                    StopAndPrint(start, "Semantic Analysis");
                });
    }

    public static void GenerateWithOutput(this ILCodeGenerator generator, Compiler compiler, string name)
    {
        AnsiConsole.Status().AutoRefresh(true)
            .Spinner(Spinner.Known.Dots).Start("Generating..."
                , ctx =>
                {
                    AnsiConsole.MarkupLine($"[green]Generating {name}[/]");
                    ctx.Status("Generating...");
                    var start = Start();
                    compiler.Ast.Accept(generator, ref IlFrame.Empty);
                    StopAndPrint(start, "Generating");
                    ctx.Status("Finishing...");
                    start = Start();
                    generator.Finish();
                    StopAndPrint(start, "Finishing");
                });
    }

    public static long Start() => Stopwatch.GetTimestamp();

    public static void StopAndPrint(long start, string name)
    {
        var ts = Stopwatch.GetElapsedTime(start);
        AnsiConsole.MarkupLine($"[gray]{name}[/] took {ts.TotalMilliseconds}ms");
    }

    public static void SaveWithOutput(this ILCodeGenerator ilGenerator, string path, string name)
    {
        AnsiConsole.Status().AutoRefresh(true)
            .Spinner(Spinner.Known.Dots).Start("Saving..."
                , ctx =>
                {
                    var save = Path.GetFullPath(path);
                    AnsiConsole.MarkupLine($"[green]Saving {name}[/]");
                    var start = Start();
                    ilGenerator.Save(save);
                    StopAndPrint(start, "Saving");
                    //write .runtimeconfig.json
                    var runtimeConfigPath = Path.ChangeExtension(save, ".runtimeconfig.json");
                    ctx.Status("[green]Writing runtimeconfig.json[/]");
                    var runtimeConfig = """
                                        {
                                          "runtimeOptions": {
                                            "tfm": "net7.0",
                                            "framework": {
                                              "name": "Microsoft.NETCore.App",
                                              "version": "7.0.0"
                                            }
                                          }
                                        }
                                        """;
                    start = Start();
                    File.WriteAllText(runtimeConfigPath, runtimeConfig);
                    StopAndPrint(start, "Writing runtimeconfig.json");
                });
    }

    public static void PrintTree(this IAstNode compilerAst)
    {
        AnsiConsole.MarkupLine("[green]AST[/]");
        TreeBuildVisitor visitor = new();
        var root = compilerAst.Accept(visitor, ref NonRef.Empty);
        //var root = compilerAst.ToTree().First();
        AnsiConsole.Write(root.ToTree());
    }

    public static Tree ToTree(this TreeRef node)
    {
        var tree = new Tree("[blue]" + node.Text + "[/]"
                            + (node.Type is not null ? $" [yellow]{node.Type}[/]" : ""));
        if (node.Name is not null)
            tree.AddNode("[green]" + node.Name + "[/]");
        foreach (var child in node.Childiren)
            tree.AddNodes(child.ToTree());
        return tree;
    }
}

public struct TreeRef
{
    public List<TreeRef> Childiren;
    public string? Name;
    public string? Type;
    public string Text;

    public TreeRef(string text)
    {
        Childiren = new();
        Text = text;
    }

    public TreeRef(string text, TypeReference type, string? name = null)
    {
        Childiren = new();
        Name = name;
        Type = type.ToString();
        Text = text;
    }
}
