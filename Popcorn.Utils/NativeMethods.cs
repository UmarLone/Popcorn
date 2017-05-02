using System.Runtime.InteropServices;

namespace Popcorn.Utils
{
    /// <summary>
    /// Provide Windows native methods
    /// </summary>
    internal class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SleepMode.EXECUTION_STATE SetThreadExecutionState(SleepMode.EXECUTION_STATE esFlags);
    }
}
