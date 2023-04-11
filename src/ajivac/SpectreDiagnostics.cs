using ajivac_lib;
using ajivac_lib.Semantics;
using Spectre.Console;

internal class SpectreDiagnostics : Diagnostics
{
    public SpectreDiagnostics(Sensitivity minSensitivity = Sensitivity.Info) : base(null, minSensitivity)
    {
    }

    protected override void WriteReportError(SourceSpan location, string message, Sensitivity sensitivity)
    {
        switch (sensitivity)
        {
            case Sensitivity.Diagnostic:
                try
                {
                    throw new Exception(message);
                }
                catch (Exception e)
                {
                    AnsiConsole.Markup("[red]Diagnostic[/]");
                    AnsiConsole.WriteLine(location.FullLocation());
                    AnsiConsole.WriteException(e);
                }
                break;
            case Sensitivity.Debug:
                AnsiConsole.MarkupLine($"[gray]{sensitivity}[/]: {message}");
                break;
            case Sensitivity.Info:
                AnsiConsole.MarkupLine($"[green]{sensitivity}[/]: {message}");
                break;
            case Sensitivity.Warning:
                AnsiConsole.MarkupLine($"[yellow]{sensitivity}[/] {location.FullLocation()}: {message}");
                break;
            case Sensitivity.Error:
                const int max = 10;
                AnsiConsole.MarkupLine($"[red]{sensitivity}[/]: {location.FullLocation()}");
                AnsiConsole.WriteLine(Extent(location, max));
                AnsiConsole.WriteLine($"{"",(max - 2)}{new string('^', (int)Math.Max(location.Length, 5))} {message}");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sensitivity), sensitivity, null);
        }
        if (sensitivity == Sensitivity.Info)
        {
            return;
        }
    }

    private string Extent(SourceSpan location, int max)
    {
        return (location with {
            Position = (uint)Math.Max(0, location.Position - max),
            Length = (uint)Math.Max(Math.Min(location.Length + max * 2, location.Source.Length - location.Position), 0)
        }).GetValue();
    }
}
