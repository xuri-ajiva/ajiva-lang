using ajivac_il;
using ajivac_lib;
using ajivac_lib.AST;
using ajivac_lib.Semantics;
using Spectre.Console;

public static class Ext
{
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
            ilGenerator.Run();
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
                    compiler.ParseAll();
                    AnsiConsole.MarkupLine("[green]Parsing Done[/]");
                    ctx.Status("Semantic Analysis...");
                    compiler.Analyze();
                    AnsiConsole.MarkupLine("[green]Semantic Analysis Done[/]");
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
                    compiler.Ast.Accept(generator, ref IlFrame.Empty);
                    AnsiConsole.MarkupLine("[green]Generating Done[/]");
                    ctx.Status("Finishing...");
                    generator.Finish();
                    AnsiConsole.MarkupLine("[green]Finishing Done[/]");
                });
    }

    public static void SaveWithOutput(this ILCodeGenerator ilGenerator, string path, string name)
    {
        AnsiConsole.Status().AutoRefresh(true)
            .Spinner(Spinner.Known.Dots).Start("Saving..."
                , ctx =>
                {
                    var save = Path.GetFullPath(path);
                    AnsiConsole.MarkupLine($"[green]Saving {name}[/]");
                    ilGenerator.Save(save);
                    AnsiConsole.MarkupLine($"[green]Saved[/] {save}");
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
                    File.WriteAllText(runtimeConfigPath, runtimeConfig);
                    AnsiConsole.MarkupLine($"[green]Written[/] {runtimeConfigPath}");
                });
    }

    public static void PrintTree(this IAstNode compilerAst)
    {
        AnsiConsole.MarkupLine("[green]AST[/]");
        var root = compilerAst.ToTree().First();
        AnsiConsole.Write(root);
    }

    public static IEnumerable<Tree> ToTree(this IAstNode node)
    {
        var tree = new Tree(node.GetType().ToString());
        foreach (var child in node.Children)
            tree.AddNodes(child.ToTree());
        yield return tree;
    }
}
