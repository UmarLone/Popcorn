using System;
using GalaSoft.MvvmLight.CommandWpf;
using NLog;
using Popcorn.Models.Player;

namespace Popcorn.ViewModels.Pages.Player
{
    /// <summary>
    /// Manage media player
    /// </summary>
    public class MediaPlayerViewModel
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Command used to stop playing the media
        /// </summary>
        public RelayCommand StopPlayingMediaCommand { get; set; }

        /// <summary>
        /// Event fired on stopped playing the media
        /// </summary>
        public event EventHandler<EventArgs> StoppedPlayingMedia;

        /// <summary>
        /// The media path
        /// </summary>
        public readonly string MediaPath;

        /// <summary>
        /// The media name
        /// </summary>
        public readonly string MediaName;

        /// <summary>
        /// The media type
        /// </summary>
        public readonly MediaType MediaType;

        /// <summary>
        /// Media action to execute when media has ended
        /// </summary>
        private readonly Action _mediaEndedAction;

        /// <summary>
        /// Media action to execute when media has been stopped
        /// </summary>
        private readonly Action _mediaStoppedAction;

        /// <summary>
        /// Subtitle file path
        /// </summary>
        public readonly string SubtitleFilePath;

        /// <summary>
        /// The buffer progress
        /// </summary>
        public readonly Progress<double> BufferProgress;

        /// <summary>
        /// Initializes a new instance of the MediaPlayerViewModel class.
        /// </summary>
        /// <param name="mediaType">The media type</param>
        /// <param name="mediaPath">Media path</param>
        /// <param name="mediaName">Media name</param>
        /// <param name="mediaStoppedAction">Media action to execute when media has been stopped</param>
        /// <param name="mediaEndedAction">Media action to execute when media has ended</param>
        /// <param name="bufferProgress">The buffer progress</param>
        /// <param name="subtitleFilePath">Subtitle file path</param>
        public MediaPlayerViewModel(MediaType mediaType, string mediaPath, string mediaName, Action mediaStoppedAction,
            Action mediaEndedAction, Progress<double> bufferProgress = null, string subtitleFilePath = null)
        {
            Logger.Info(
                $"Loading media : {mediaName}.");

            MediaType = mediaType;
            MediaPath = mediaPath;
            MediaName = mediaName;
            _mediaStoppedAction = mediaStoppedAction;
            _mediaEndedAction = mediaEndedAction;
            SubtitleFilePath = subtitleFilePath;
            BufferProgress = bufferProgress;

            RegisterCommands();
        }

        /// <summary>
        /// When a media has been ended, invoke the <see cref="_mediaEndedAction"/>
        /// </summary>
        public void MediaEnded()
        {
            OnStoppedPlayingMedia(new EventArgs());
            _mediaEndedAction?.Invoke();
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
            =>
                StopPlayingMediaCommand =
                    new RelayCommand(() =>
                    {
                        OnStoppedPlayingMedia(new EventArgs());
                        _mediaStoppedAction?.Invoke();
                    });

        /// <summary>
        /// Fire StoppedPlayingMedia event
        /// </summary>
        /// <param name="e">Event args</param>
        private void OnStoppedPlayingMedia(EventArgs e)
        {
            Logger.Debug(
                "Stop playing a media");

            var handler = StoppedPlayingMedia;
            handler?.Invoke(this, e);
        }
    }
}