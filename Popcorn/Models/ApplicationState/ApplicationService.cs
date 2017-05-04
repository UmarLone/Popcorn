using GalaSoft.MvvmLight;

namespace Popcorn.Models.ApplicationState
{
    public class ApplicationService : ObservableObject, IApplicationService
    {
        /// <summary>
        /// Specify if a connection error has occured
        /// </summary>
        private bool _isConnectionInError;

        /// <summary>
        /// Indicates if application is fullscreen
        /// </summary>
        private bool _isFullScreen;

        /// <summary>
        /// Indicates if a movie is playing
        /// </summary>
        private bool _isMoviePlaying;

        /// <summary>
        /// Indicates if a movie is playing
        /// </summary>
        public bool IsMediaPlaying
        {
            get => _isMoviePlaying;
            set { Set(() => IsMediaPlaying, ref _isMoviePlaying, value); }
        }

        /// <summary>
        /// Specify if a connection error has occured
        /// </summary>
        public bool IsConnectionInError
        {
            get => _isConnectionInError;
            set { Set(() => IsConnectionInError, ref _isConnectionInError, value); }
        }

        /// <summary>
        /// Indicates if application is fullscreen
        /// </summary>
        public bool IsFullScreen
        {
            get => _isFullScreen;
            set { Set(() => IsFullScreen, ref _isFullScreen, value); }
        }
    }
}