using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ajivac_il;
using ajivac_lib;
using ajivac_lib.AST;
using ajivac_lib.Semantics;
using ajivac_llvm;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

AnsiConsole.Write(new FigletText("Ajiva Compiler").Color(Color.Blue));
var app = new CommandApp<AjivaCompiler>();
return app.Run(args);

internal sealed class AjivaCompiler : Command<AjivaCompiler.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Path to the file to compile")]
        [CommandArgument(0, "[file.aj]")]
        public string InPath { get; init; }

        [Description("Path where to save the compiled file")]
        [CommandOption("-o|--out <file>")]
        public string? OutPath { get; init; }

        [Description("Print the Source")]
        [CommandOption("-s|--print-source")]
        [DefaultValue(false)]
        public bool PrintSource { get; set; }

        [Description("Print the AST")]
        [CommandOption("-t|--print-tree")]
        [DefaultValue(false)]
        public bool PrintTree { get; set; }

        [Description("Run the Program")]
        [CommandOption("-r|--run")]
        [DefaultValue(false)]
        public bool Run { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var source = new SourceFile(File.ReadAllText(settings.InPath), settings.InPath);
        var name = Path.GetFileNameWithoutExtension(settings.InPath);
        var compiler = new Compiler(source, new SpectreDiagnostics());
        if (settings.PrintSource)
            AnsiConsole.Write(new Panel(new Text(source.Text).Fold())
                .Header("Source"));
        AnsiConsole.Status().AutoRefresh(true)
            .Spinner(Spinner.Known.Dots).Start("Compiling..."
                , ctx =>
                {
                    AnsiConsole.MarkupLine($"[green]Compiling {name}[/]");
                    ctx.Status("Parsing...");
                    compiler.ParseAll();
                    AnsiConsole.MarkupLine($"[green]Parsing Done[/]");
                    ctx.Status("Semantic Analysis...");
                    compiler.Analyze();
                    AnsiConsole.MarkupLine($"[green]Semantic Analysis Done[/]");
                });

        if (settings.PrintTree)
        {
            AnsiConsole.MarkupLine("[green]AST[/]");
            PrintTree(compiler.Ast);
        }
        var interpreter = new Interpreter(s => Debug.WriteLine(s));
        interpreter.Load(compiler.RuntimeState);
        var ilGenerator = new ILCodeGenerator(name, interpreter);
        ilGenerator.Visit((RootNode)compiler.Ast);
        ilGenerator.Finish();
        if (settings.Run)
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
        if (settings.OutPath is not null)
        {
            var save = Path.GetFullPath(settings.OutPath);
            AnsiConsole.MarkupLine($"[green]Saving[/] {name} to {save}");
            ilGenerator.Save(save);
        }
        AnsiConsole.MarkupLine($"[green]Compiled[/] {name}");
        return 0;
    }

    private void PrintTree(IAstNode compilerAst)
    {
        var root = compilerAst.ToTree().First();
        AnsiConsole.Write(root);
    }
}
internal class SpectreDiagnostics : Diagnostics
{
    public SpectreDiagnostics() : base(null, Sensitivity.Info)
    {
    }

    public override void ReportError(SourceSpan location, string message, Sensitivity sensitivity)
    {
        const int max = 10;
        AnsiConsole.MarkupLine($"[red]{sensitivity}[/]: {location.FullLocation()}");
        AnsiConsole.WriteLine(Extent(location, max));
        AnsiConsole.WriteLine($"{"",(max - 2)}~~^~~ {message}");
    }

    private string Extent(SourceSpan location, int max)
    {
        return (location with {
            Position = (uint)Math.Max(0, location.Position - max),
            Length = (uint)Math.Min(location.Length + max * 2, location.Source.Length - location.Position)
        }).GetValue();
    }
}
public static class Ext
{
    public static IEnumerable<Tree> ToTree(this IAstNode node)
    {
        var tree = new Tree(node.GetType().ToString());
        foreach (var child in node.Children)
            tree.AddNodes(child.ToTree());
        yield return tree;
    }
}
