namespace Popcorn.Utils
{
    /// <summary>
    /// Provide some useful methods to manage Windows sleep mode
    /// </summary>
    public class SleepMode
    {
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
        }

        /// <summary>
        /// Prevent Windows from sleeping
        /// </summary>
        public static void PreventWindowsFromSleeping()
        {
            NativeMethods.SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED);
        }
    }
}
