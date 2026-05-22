using System;
using System.Diagnostics;

namespace Genesys.UI.Components.Diagnostics
{
    internal static class GenesysTrace
    {
        private static int sequence;

        [Conditional("DEBUG")]
        public static void Log(string source, string message)
        {
            Debug.WriteLine(
                $"[{++sequence:000}] {DateTime.Now:HH:mm:ss.fff} [{source}] {message}");
        }
    }
}