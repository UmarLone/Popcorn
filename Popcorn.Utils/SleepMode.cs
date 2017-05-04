namespace Popcorn.Utils
{
    /// <summary>
    /// Provide some useful methods to manage Windows sleep mode
    /// </summary>
    public class SleepMode
    {
        public enum ExecutionState : uint
        {
            EsAwaymodeRequired = 0x00000040,
            EsContinuous = 0x80000000,
            EsDisplayRequired = 0x00000002,
        }

        /// <summary>
        /// Prevent Windows from sleeping
        /// </summary>
        public static void PreventWindowsFromSleeping()
        {
            NativeMethods.SetThreadExecutionState(ExecutionState.EsDisplayRequired);
        }
    }
}
