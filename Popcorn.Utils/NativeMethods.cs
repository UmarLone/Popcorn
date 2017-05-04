using System.Runtime.InteropServices;

namespace Popcorn.Utils
{
    /// <summary>
    /// Provide Windows native methods
    /// </summary>
    public static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SleepMode.ExecutionState SetThreadExecutionState(SleepMode.ExecutionState esFlags);
    }
}
