using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;

namespace Popcorn.Messaging
{
    public class PlayMediaMessage : MessageBase
    {
        /// <summary>
        /// The buffer progress
        /// </summary>
        public readonly Progress<double> BufferProgress;

        /// <summary>
        /// The media path
        /// </summary>
        public readonly string MediaPath;

        /// <summary>
        /// Initialize a new instance of PlayMediaMessage class
        /// </summary>
        /// <param name="mediaPath">The media path</param>
        /// <param name="bufferProgress">The buffer progress</param>
        public PlayMediaMessage(string mediaPath, Progress<double> bufferProgress)
        {
            MediaPath = mediaPath;
            BufferProgress = bufferProgress;
        }
    }
}
