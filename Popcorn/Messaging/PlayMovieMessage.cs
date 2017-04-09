using System;
using GalaSoft.MvvmLight.Messaging;
using Popcorn.Models.Movie;

namespace Popcorn.Messaging
{
    /// <summary>
    /// Used to play a movie
    /// </summary>
    public class PlayMovieMessage : MessageBase
    {
        /// <summary>
        /// The buffered movie
        /// </summary>
        public readonly MovieJson Movie;

        /// <summary>
        /// The buffer progress
        /// </summary>
        public readonly Progress<double> BufferProgress;

        /// <summary>
        /// Initialize a new instance of PlayMovieMessage class
        /// </summary>
        /// <param name="movie">The movie</param>
        /// <param name="bufferProgress">The buffer progress</param>
        public PlayMovieMessage(MovieJson movie, Progress<double> bufferProgress)
        {
            Movie = movie;
            BufferProgress = bufferProgress;
        }
    }
}