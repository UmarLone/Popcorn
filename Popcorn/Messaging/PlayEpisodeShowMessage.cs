using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using Popcorn.Models.Episode;

namespace Popcorn.Messaging
{
    /// <summary>
    /// Play an episode of a show
    /// </summary>
    public class PlayShowEpisodeMessage : MessageBase
    {
        /// <summary>
        /// Episode
        /// </summary>
        public readonly EpisodeShowJson Episode;

        /// <summary>
        /// The buffer progress
        /// </summary>
        public readonly Progress<double> BufferProgress;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="episode">Episode</param>
        /// <param name="bufferProgress">The buffer progress</param>
        public PlayShowEpisodeMessage(EpisodeShowJson episode, Progress<double> bufferProgress)
        {
            Episode = episode;
            BufferProgress = bufferProgress;
        }
    }
}
