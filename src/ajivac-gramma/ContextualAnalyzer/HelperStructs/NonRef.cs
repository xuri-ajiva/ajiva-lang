using System.Runtime.InteropServices;

namespace ajivac_lib.ContextualAnalyzer.HelperStructs;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct NonRef
{
    public static NonRef Empty = new NonRef();
}
