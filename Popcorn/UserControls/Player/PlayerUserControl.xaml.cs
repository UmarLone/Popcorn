using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Threading;
using Popcorn.ViewModels.Pages.Player;
using System.Threading;
using System.Diagnostics;
using GalaSoft.MvvmLight.Messaging;
using Popcorn.Messaging;
using Popcorn.Helpers;

namespace Popcorn.UserControls.Player
{
    /// <summary>
    /// Interaction logic for PlayerUserControl.xaml
    /// </summary>
    public partial class PlayerUserControl : IDisposable
    {
        /// <summary>
        /// If control is disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// False if player is not fully initialised
        /// </summary>
        private bool _isPlayerFullyInitialised;

        /// <summary>
        /// Mutex for player initialization
        /// </summary>
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Indicates if a media is playing
        /// </summary>
        private bool MediaPlayerIsPlaying { get; set; }

        /// <summary>
        /// Used to update the activity mouse and mouse position.
        /// </summary>
        private DispatcherTimer ActivityTimer { get; set; }

        /// <summary>
        /// Get or set the mouse position when inactive
        /// </summary>
        private Point InactiveMousePosition { get; set; } = new Point(0, 0);

        /// <summary>
        /// Indicate if user is manipulating the timeline player
        /// </summary>
        private bool UserIsDraggingMediaPlayerSlider { get; set; }

        /// <summary>
        /// Timer used for report time on the timeline
        /// </summary>
        private DispatcherTimer MediaPlayerTimer { get; set; }

        /// <summary>
        /// Report when dragging is used on media player
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">DragStartedEventArgs</param>
        private void MediaSliderProgressDragStarted(object sender, DragStartedEventArgs e)
            => UserIsDraggingMediaPlayerSlider = true;

        /// <summary>
        /// Identifies the <see cref="Volume" /> dependency property.
        /// </summary>
        internal static readonly DependencyProperty VolumeProperty = DependencyProperty.Register("Volume",
            typeof(int),
            typeof(PlayerUserControl), new PropertyMetadata(100, OnVolumeChanged));

        private bool _isMouseActivityCaptured;

        /// <summary>
        /// Initializes a new instance of the MoviePlayer class.
        /// </summary>
        public PlayerUserControl()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        /// <summary>
        /// Get or set the media volume
        /// </summary>
        public int Volume
        {
            get { return (int)GetValue(VolumeProperty); }

            set { SetValue(VolumeProperty, value); }
        }

        /// <summary>
        /// Free resources
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Subscribe to events and play the movie when control has been loaded
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void OnLoaded(object sender, EventArgs e)
        {
            var window = System.Windows.Window.GetWindow(this);
            if (window != null)
                window.Closing += (s1, e1) => Dispose();

            var vm = DataContext as MediaPlayerViewModel;
            if (vm == null) return;
            if (vm.MediaPath == null)
                return;

            // start the timer used to report time on MediaPlayerSliderProgress
            MediaPlayerTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            MediaPlayerTimer.Tick += MediaPlayerTimerTick;
            MediaPlayerTimer.Start();

            // start the activity timer used to manage visibility of the PlayerStatusBar
            ActivityTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            ActivityTimer.Tick += OnInactivity;
            ActivityTimer.Start();

            InputManager.Current.PreProcessInput += OnActivity;

            vm.StoppedPlayingMedia += OnStoppedPlayingMedia;

            Player.VlcMediaPlayer.EndReached += MediaPlayerEndReached;

            if (!string.IsNullOrEmpty(vm.SubtitleFilePath))
            {
                Player.LoadMediaWithOptions(vm.MediaPath, $@":sub-file={vm.SubtitleFilePath}");
            }
            else
            {
                Player.LoadMedia(vm.MediaPath);
            }

            PlayMedia();
        }

        /// <summary>
        /// When media's volume changed, update volume
        /// </summary>
        /// <param name="e">e</param>
        /// <param name="obj">obj</param>
        private static void OnVolumeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var moviePlayer = obj as PlayerUserControl;
            if (moviePlayer == null)
                return;

            var newVolume = (int)e.NewValue;
            moviePlayer.ChangeMediaVolume(newVolume);
        }

        /// <summary>
        /// Change the media's volume
        /// </summary>
        /// <param name="newValue">New volume value</param>
        private void ChangeMediaVolume(int newValue) => Player.Volume = newValue;

        /// <summary>
        /// When user uses the mousewheel, update the volume
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">MouseWheelEventArgs</param>
        private void MouseWheelMediaPlayer(object sender, MouseWheelEventArgs e)
        {
            if ((Volume <= 190 && e.Delta > 0) || (Volume >= 10 && e.Delta < 0))
                Volume += (e.Delta > 0) ? 10 : -10;
        }

        /// <summary>
        /// When a movie has been seen, save this information in the user data
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void MediaPlayerEndReached(object sender, EventArgs e)
            => DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                var vm = DataContext as MediaPlayerViewModel;
                if (vm == null)
                    return;

                vm.MediaEnded();
            });

        /// <summary>
        /// Play the movie
        /// </summary>
        private void PlayMedia()
        {
            MediaPlayerIsPlaying = true;

            MediaPlayerStatusBarItemPlay.Visibility = Visibility.Collapsed;
            MediaPlayerStatusBarItemPause.Visibility = Visibility.Visible;

            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                await Task.Delay(500);
                Player.Play();
            });
        }

        /// <summary>
        /// Pause the movie
        /// </summary>
        private void PauseMedia()
        {
            Player.Pause();
            MediaPlayerIsPlaying = false;

            MediaPlayerStatusBarItemPlay.Visibility = Visibility.Visible;
            MediaPlayerStatusBarItemPause.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// When media has finished playing, dispose the control
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void OnStoppedPlayingMedia(object sender, EventArgs e) => Dispose();

        /// <summary>
        /// Report the playing progress on the timeline
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void MediaPlayerTimerTick(object sender, EventArgs e)
        {
            if ((Player == null) || (UserIsDraggingMediaPlayerSlider)) return;
            MediaPlayerSliderProgress.Minimum = 0;
            MediaPlayerSliderProgress.Maximum = Player.Length.TotalSeconds;
            MediaPlayerSliderProgress.Value = Player.Time.TotalSeconds;
        }

        /// <summary>
        /// Each time the CanExecute play command change, update the visibility of Play/Pause buttons in the player
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">CanExecuteRoutedEventArgs</param>
        private void MediaPlayerPlayCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (MediaPlayerStatusBarItemPlay == null || MediaPlayerStatusBarItemPause == null) return;
            e.CanExecute = Player != null;
            if (MediaPlayerIsPlaying)
            {
                MediaPlayerStatusBarItemPlay.Visibility = Visibility.Collapsed;
                MediaPlayerStatusBarItemPause.Visibility = Visibility.Visible;
            }
            else
            {
                MediaPlayerStatusBarItemPlay.Visibility = Visibility.Visible;
                MediaPlayerStatusBarItemPause.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Each time the CanExecute play command change, update the visibility of Play/Pause buttons in the media player
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">CanExecuteRoutedEventArgs</param>
        private void MediaPlayerPauseCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (MediaPlayerStatusBarItemPlay == null || MediaPlayerStatusBarItemPause == null) return;
            e.CanExecute = MediaPlayerIsPlaying;
            if (MediaPlayerIsPlaying)
            {
                MediaPlayerStatusBarItemPlay.Visibility = Visibility.Collapsed;
                MediaPlayerStatusBarItemPause.Visibility = Visibility.Visible;
            }
            else
            {
                MediaPlayerStatusBarItemPlay.Visibility = Visibility.Visible;
                MediaPlayerStatusBarItemPause.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Report when user has finished dragging the media player progress
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">DragCompletedEventArgs</param>
        private void MediaSliderProgressDragCompleted(object sender, DragCompletedEventArgs e)
        {
            UserIsDraggingMediaPlayerSlider = false;
            Player.Time = TimeSpan.FromSeconds(MediaPlayerSliderProgress.Value);
        }

        /// <summary>
        /// Report runtime when trailer player progress changed
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">RoutedPropertyChangedEventArgs</param>
        private void MediaSliderProgressValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MoviePlayerTextProgressStatus.Text =
                TimeSpan.FromSeconds(MediaPlayerSliderProgress.Value)
                    .ToString(@"hh\:mm\:ss", CultureInfo.CurrentCulture) + " / " +
                TimeSpan.FromSeconds(Player.Length.TotalSeconds)
                    .ToString(@"hh\:mm\:ss", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Play media
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">ExecutedRoutedEventArgs</param>
        private void MediaPlayerPlayExecuted(object sender, ExecutedRoutedEventArgs e) => PlayMedia();

        /// <summary>
        /// Pause media
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">CanExecuteRoutedEventArgs</param>
        private void MediaPlayerPauseExecuted(object sender, ExecutedRoutedEventArgs e) => PauseMedia();

        /// <summary>
        /// Hide the PlayerStatusBar on mouse inactivity
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void OnInactivity(object sender, EventArgs e)
        {
            InactiveMousePosition = Mouse.GetPosition(Container);
            var window = System.Windows.Window.GetWindow(this);
            if (window != null)
            {
                window.Cursor = Cursors.None;
            }

            var opacityAnimation = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new EasingDoubleKeyFrame(0.0, KeyTime.FromPercent(1d), new PowerEase
                    {
                        EasingMode = EasingMode.EaseInOut
                    })
                }
            };

            PlayerStatusBar.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        /// <summary>
        /// Show the PlayerStatusBar on mouse activity
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private async void OnActivity(object sender, PreProcessInputEventArgs e)
        {
            if (_isMouseActivityCaptured)
                return;

            _isMouseActivityCaptured = true;

            var inputEventArgs = e.StagingItem.Input;
            if (!(inputEventArgs is MouseEventArgs) && !(inputEventArgs is KeyboardEventArgs))
            {
                _isMouseActivityCaptured = false;
                return;
            }
            var mouseEventArgs = e.StagingItem.Input as MouseEventArgs;

            // no button is pressed and the position is still the same as the application became inactive
            if (mouseEventArgs?.LeftButton == MouseButtonState.Released &&
                mouseEventArgs.RightButton == MouseButtonState.Released &&
                mouseEventArgs.MiddleButton == MouseButtonState.Released &&
                mouseEventArgs.XButton1 == MouseButtonState.Released &&
                mouseEventArgs.XButton2 == MouseButtonState.Released &&
                InactiveMousePosition == mouseEventArgs.GetPosition(Container))
            {
                _isMouseActivityCaptured = false;
                return;
            }

            var opacityAnimation = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.1)),
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(1.0), new PowerEase
                    {
                        EasingMode = EasingMode.EaseInOut
                    })
                }
            };

            PlayerStatusBar.BeginAnimation(OpacityProperty, opacityAnimation);
            var window = System.Windows.Window.GetWindow(this);
            if (window != null)
            {
                window.Cursor = Cursors.Arrow;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
            _isMouseActivityCaptured = false;
        }

        /// <summary>
        /// Dispose the control
        /// </summary>
        /// <param name="disposing">If a disposing is already processing</param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            Loaded -= OnLoaded;

            MediaPlayerTimer.Tick -= MediaPlayerTimerTick;
            MediaPlayerTimer.Stop();

            ActivityTimer.Tick -= OnInactivity;
            ActivityTimer.Stop();

            InputManager.Current.PreProcessInput -= OnActivity;

            Player.VlcMediaPlayer.EndReached -= MediaPlayerEndReached;
            MediaPlayerIsPlaying = false;
            Player.Stop();
            Player.Dispose();

            var window = System.Windows.Window.GetWindow(this);
            if (window != null)
            {
                window.Cursor = Cursors.Arrow;
            }

            var vm = DataContext as MediaPlayerViewModel;
            if (vm != null)
                vm.StoppedPlayingMedia -= OnStoppedPlayingMedia;

            _disposed = true;

            if (disposing)
                GC.SuppressFinalize(this);
        }

        /// <summary>
        /// On player length changed, make sure player have enough space to show fully
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnLengthChanged(object sender, EventArgs e)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                if (_isPlayerFullyInitialised) return;

                _isPlayerFullyInitialised = true;
                Player.Visibility = Visibility.Hidden;

                bool wasMaximized = false;
                var watcher = new Stopwatch();
                watcher.Start();
                while (Player.ActualHeight < 1d)
                {
                    if (Application.Current.MainWindow.WindowState != WindowState.Maximized)
                    {
                        Application.Current.MainWindow.Width -= 0.1d;
                    }
                    else
                    {
                        wasMaximized = true;
                        Application.Current.MainWindow.WindowState = WindowState.Normal;
                        Application.Current.MainWindow.Width -= 0.1d;
                    }

                    await Task.Delay(100);

                    // Check if we are waiting for more than 2 seconds for the player to initialize. If so, something weird happen, so break the loop
                    if (watcher.ElapsedMilliseconds > 2000)
                    {
                        watcher.Stop();
                        Messenger.Default.Send(
                            new ManageExceptionMessage(
                                new Exception(
                                    LocalizationProviderHelper.GetLocalizedValue<string>("TrailerNotAvailable"))));
                        Messenger.Default.Send(new StopPlayingTrailerMessage());
                        break;
                    }
                }

                if (wasMaximized)
                {
                    Application.Current.MainWindow.WindowState = WindowState.Maximized;
                }

                Player.Visibility = Visibility.Visible;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
}