using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ajivac_lib;

public static class ThrowHelper
{
    [DebuggerStepThrough]
    [DoesNotReturn]
    public static void ThrowArgumentNullException(string nodeName)
    {
        throw new ArgumentNullException(nodeName);
    }
}
