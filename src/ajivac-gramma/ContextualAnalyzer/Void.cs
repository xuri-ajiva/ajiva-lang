using System.Runtime.InteropServices;

namespace ajivac_lib.ContextualAnalyzer;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct Void
{
    public static Void Empty = new Void();
}
