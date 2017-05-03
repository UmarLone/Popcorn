using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.OSDB
{
    /// <summary>
    /// OSDB exception
    /// </summary>
    [Serializable]
    public class OSDBException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public OSDBException()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Message</param>
        public OSDBException(string message) : base(message)
        {
        }
    }
}
