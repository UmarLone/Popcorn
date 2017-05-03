using System;
using GalaSoft.MvvmLight.CommandWpf;
using NLog;
using Popcorn.Models.Bandwidth;
using Popcorn.Utils;

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
        /// The download rate
        /// </summary>
        public readonly Progress<BandwidthRate> BandwidthRate;

        /// <summary>
        /// The media type
        /// </summary>
        public readonly MediaType MediaType;

        /// <summary>
        /// Initializes a new instance of the MediaPlayerViewModel class.
        /// </summary>
        /// <param name="mediaPath">Media path</param>
        /// <param name="type">Media type</param>
        /// <param name="mediaStoppedAction">Media action to execute when media has been stopped</param>
        /// <param name="mediaEndedAction">Media action to execute when media has ended</param>
        /// <param name="bufferProgress">The buffer progress</param>
        /// <param name="bandwidthRate">THe bandwidth rate</param>
        /// <param name="subtitleFilePath">Subtitle file path</param>
        public MediaPlayerViewModel(string mediaPath, MediaType type, Action mediaStoppedAction,
            Action mediaEndedAction, Progress<double> bufferProgress = null, Progress<BandwidthRate> bandwidthRate = null, string subtitleFilePath = null)
        {
            Logger.Info(
                $"Loading media : {mediaPath}.");
            RegisterCommands();

            MediaPath = mediaPath;
            MediaType = type;
            _mediaStoppedAction = mediaStoppedAction;
            _mediaEndedAction = mediaEndedAction;
            SubtitleFilePath = subtitleFilePath;
            BufferProgress = bufferProgress;
            BandwidthRate = bandwidthRate;

            // Prevent windows from sleeping
            Utils.SleepMode.PreventWindowsFromSleeping();
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