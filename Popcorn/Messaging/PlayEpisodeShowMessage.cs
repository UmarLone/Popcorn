using GalaSoft.MvvmLight.Messaging;
using Popcorn.Models.Episode;

namespace Popcorn.Messaging
{
    /// <summary>
    /// Play an episode of a show
    /// </summary>
    public class PlayEpisodeShowMessage : MessageBase
    {
        /// <summary>
        /// Episode
        /// </summary>
        public readonly EpisodeShowJson Episode;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="episode">Episode</param>
        public PlayEpisodeShowMessage(EpisodeShowJson episode)
        {
            Episode = episode;
        }
    }
}
