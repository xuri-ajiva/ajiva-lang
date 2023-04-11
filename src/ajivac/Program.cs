using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ajivac_il;
using ajivac_lib;
using ajivac_llvm;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

AnsiConsole.Write(new FigletText("Ajiva Compiler").Color(Color.Blue));
var app = new CommandApp();
app.Configure(c =>
{
    c.SetApplicationName("Ajiva Compiler");
    c.SetApplicationVersion("1.0.0");
    c.AddCommand<RunCommand>("run");
    c.AddCommand<CompileCommand>("compile");
});
return app.Run(args);

internal sealed class RunCommand : Command<RunCommand.Settings>
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
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var source = Ext.SourceFile(settings.InPath, out var name);
        var compiler = new Compiler(source, new SpectreDiagnostics());
        if (settings.PrintSource)
            source.Print();

        compiler.CompileWithOutput(name);

        if (settings.PrintTree)
            compiler.Ast.PrintTree();

        var interpreter = new Interpreter(s => Debug.WriteLine(s));
        interpreter.Load(compiler.RuntimeState);
        var ilGenerator = new ILCodeGenerator(name, interpreter);
        ilGenerator.GenerateWithOutput(compiler, name);

        ilGenerator.RunWithOutput(name);
        if (settings.OutPath is not null)
            ilGenerator.SaveWithOutput(settings.OutPath, name);

        AnsiConsole.MarkupLine($"[green]Compiled[/] {name}");
        return 0;
    }
}
internal sealed class CompileCommand : Command<CompileCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Path to the file to compile")]
        [CommandArgument(0, "[file.aj]")]
        public string InPath { get; init; }

        [Description("Path where to save the compiled file")]
        [CommandArgument(1, "[file.dll]")]
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
        var source = Ext.SourceFile(settings.InPath, out var name);
        var compiler = new Compiler(source, new SpectreDiagnostics());
        if (settings.PrintSource)
            source.Print();

        compiler.CompileWithOutput(name);

        if (settings.PrintTree)
            compiler.Ast.PrintTree();

        var interpreter = new Interpreter(s => Debug.WriteLine(s));
        interpreter.Load(compiler.RuntimeState);
        var ilGenerator = new ILCodeGenerator(name, interpreter);
        ilGenerator.GenerateWithOutput(compiler, name);

        if (settings.Run)
            ilGenerator.RunWithOutput(name);
        if (settings.OutPath is not null)
            ilGenerator.SaveWithOutput(settings.OutPath, name);

        AnsiConsole.MarkupLine($"[green]Compiled[/] {name}");
        return 0;
    }
}
