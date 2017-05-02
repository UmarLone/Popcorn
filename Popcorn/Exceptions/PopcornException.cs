using System;

namespace Popcorn.Exceptions
{
    /// <summary>
    /// Popcorn exception
    /// </summary>
    public class PopcornException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public PopcornException()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Message</param>
        public PopcornException(string message) : base(message)
        {
        }
    }
}