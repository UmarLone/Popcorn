using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using Popcorn.Dialogs;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Models.ApplicationState;
using Popcorn.Services.Movies.History;
using Popcorn.ViewModels.Pages.Home;
using Popcorn.ViewModels.Pages.Home.Anime;
using Popcorn.ViewModels.Pages.Home.Movie;
using Popcorn.ViewModels.Pages.Home.Show;
using Popcorn.ViewModels.Pages.Player;
using Squirrel;

namespace Popcorn.ViewModels.Windows
{
    /// <summary>
    /// Window applcation's viewmodel
    /// </summary>
    public class WindowViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Used to define the dialog context
        /// </summary>
        private readonly IDialogCoordinator _dialogCoordinator;

        /// <summary>
        /// Specify if an exception is curently managed
        /// </summary>
        private bool _isManagingException;

        /// <summary>
        /// Specify if movie flyout is open
        /// </summary>
        private bool _isMovieFlyoutOpen;

        /// <summary>
        /// Specify if show flyout is open
        /// </summary>
        private bool _isShowFlyoutOpen;

        /// <summary>
        /// Specify if settings flyout is open
        /// </summary>
        private bool _isSettingsFlyoutOpen;

        /// <summary>
        /// Application state
        /// </summary>
        private IApplicationService _applicationService;

        /// <summary>
        /// The movie history service
        /// </summary>
        private readonly IMovieHistoryService _movieHistoryService;

        /// <summary>
        /// <see cref="MediaPlayer"/>
        /// </summary>
        private MediaPlayerViewModel _mediaPlayer;

        /// <summary>
        /// <see cref="PageUri"/>
        /// </summary>
        private string _pageUri;

        /// <summary>
        /// Initializes a new instance of the WindowViewModel class.
        /// </summary>
        /// <param name="applicationService">Instance of Application state</param>
        /// <param name="movieHistoryService">Instance of movie history service</param>
        public WindowViewModel(IApplicationService applicationService, IMovieHistoryService movieHistoryService)
        {
            _movieHistoryService = movieHistoryService;
            _dialogCoordinator = DialogCoordinator.Instance;
            _applicationService = applicationService;
            RegisterMessages();
            RegisterCommands();
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            PageUri = "/Pages/HomePage.xaml";
        }

        /// <summary>
        /// Current page uri
        /// </summary>
        public string PageUri
        {
            get => _pageUri;
            set { Set(() => PageUri, ref _pageUri, value); }
        }

        /// <summary>
        /// Application state
        /// </summary>
        public IApplicationService ApplicationService
        {
            get => _applicationService;
            set { Set(() => ApplicationService, ref _applicationService, value); }
        }

        /// <summary>
        /// Specify if settings flyout is open
        /// </summary>
        public bool IsSettingsFlyoutOpen
        {
            get => _isSettingsFlyoutOpen;
            set { Set(() => IsSettingsFlyoutOpen, ref _isSettingsFlyoutOpen, value); }
        }

        /// <summary>
        /// Specify if movie flyout is open
        /// </summary>
        public bool IsMovieFlyoutOpen
        {
            get => _isMovieFlyoutOpen;
            set { Set(() => IsMovieFlyoutOpen, ref _isMovieFlyoutOpen, value); }
        }

        /// <summary>
        /// Specify if show flyout is open
        /// </summary>
        public bool IsShowFlyoutOpen
        {
            get => _isShowFlyoutOpen;
            set { Set(() => IsShowFlyoutOpen, ref _isShowFlyoutOpen, value); }
        }

        /// <summary>
        /// Media player
        /// </summary>
        public MediaPlayerViewModel MediaPlayer
        {
            get => _mediaPlayer;
            set { Set(() => MediaPlayer, ref _mediaPlayer, value); }
        }

        /// <summary>
        /// Command used to close movie page
        /// </summary>
        public RelayCommand CloseMoviePageCommand { get; private set; }

        /// <summary>
        /// Command used to close show page
        /// </summary>
        public RelayCommand CloseShowPageCommand { get; private set; }

        /// <summary>
        /// Command used to close the application
        /// </summary>
        public RelayCommand MainWindowClosingCommand { get; private set; }

        /// <summary>
        /// Command used to open application settings
        /// </summary>
        public RelayCommand OpenSettingsCommand { get; private set; }

        /// <summary>
        /// Command used to load tabs
        /// </summary>
        public RelayCommand InitializeAsyncCommand { get; private set; }

        /// <summary>
        /// Register messages
        /// </summary>
        private void RegisterMessages()
        {
            Messenger.Default.Register<ManageExceptionMessage>(this, e => ManageException(e.UnHandledException));

            Messenger.Default.Register<LoadMovieMessage>(this, e => IsMovieFlyoutOpen = true);

            Messenger.Default.Register<LoadShowMessage>(this, e => IsShowFlyoutOpen = true);

            Messenger.Default.Register<PlayMovieMessage>(this, message => DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                MediaPlayer = new MediaPlayerViewModel(message.Movie.FilePath, message.Movie.Title,
                    () =>
                    {
                        Messenger.Default.Send(new StopPlayingMovieMessage());
                    },
                    async () =>
                    {
                        await _movieHistoryService.SetHasBeenSeenMovieAsync(message.Movie);
                        Messenger.Default.Send(new ChangeHasBeenSeenMovieMessage());
                        Messenger.Default.Send(new StopPlayingMovieMessage());
                    },
                    message.Movie.SelectedSubtitle?.FilePath);

                ApplicationService.IsMediaPlaying = true;
                IsMovieFlyoutOpen = false;
                PageUri = "/Pages/PlayerPage.xaml";
            }));

            Messenger.Default.Register<PlayTrailerMessage>(this, message => DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                MediaPlayer = new MediaPlayerViewModel(message.TrailerUrl, message.MovieTitle,
                    message.TrailerStoppedAction, message.TrailerEndedAction);
                ApplicationService.IsMediaPlaying = true;
                IsMovieFlyoutOpen = false;
                PageUri = "/Pages/PlayerPage.xaml";
            }));

            Messenger.Default.Register<StopPlayingTrailerMessage>(this, message =>
            {
                ApplicationService.IsMediaPlaying = false;
                IsMovieFlyoutOpen = true;
                PageUri = "/Pages/HomePage.xaml";
            });

            Messenger.Default.Register<StopPlayingMovieMessage>(
                this,
                message =>
                {
                    ApplicationService.IsMediaPlaying = false;
                    IsMovieFlyoutOpen = true;
                    PageUri = "/Pages/HomePage.xaml";
                });

            Messenger.Default.Register<ChangeLanguageMessage>(
                this,
                message =>
                {
                    var pages = SimpleIoc.Default.GetInstance<PagesViewModel>();
                    foreach (var page in pages.Pages)
                    {
                        if (page is MoviePageViewModel)
                        {
                            page.Caption = LocalizationProviderHelper.GetLocalizedValue<string>("MoviesLabel");
                        }
                        else if (page is AnimePageViewModel)
                        {
                            page.Caption = LocalizationProviderHelper.GetLocalizedValue<string>("AnimesLabel");
                        }
                        else if (page is ShowPageViewModel)
                        {
                            page.Caption = LocalizationProviderHelper.GetLocalizedValue<string>("ShowsLabel");
                        }
                    }
                });

            Messenger.Default.Register<UnhandledExceptionMessage>(this, message => ManageException(message.Exception));
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            CloseMoviePageCommand = new RelayCommand(() =>
            {
                Messenger.Default.Send(new StopPlayingTrailerMessage());
                IsMovieFlyoutOpen = false;
            });

            CloseShowPageCommand = new RelayCommand(() =>
            {
                IsShowFlyoutOpen = false;
            });

            MainWindowClosingCommand = new RelayCommand(() =>
            {
                if (!Directory.Exists(Constants.MovieDownloads)) return;
                foreach (
                    var filePath in Directory.GetDirectories(Constants.MovieDownloads)
                )
                {
                    try
                    {
                        Logger.Debug(
                            $"Deleting directory: {filePath}");
                        Directory.Delete(filePath, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error while deleting directory: {ex.Message}.");
                    }
                }

                if (!Directory.Exists(Constants.MovieTorrentDownloads)) return;
                foreach (
                    var filePath in Directory.GetFiles(Constants.MovieTorrentDownloads, "*.*", SearchOption.AllDirectories)
                )
                {
                    try
                    {
                        Logger.Debug(
                            $"Deleting file: {filePath}");
                        File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error while deleting file: {ex.Message}.");
                    }
                }
            });

            OpenSettingsCommand = new RelayCommand(() => IsSettingsFlyoutOpen = !IsSettingsFlyoutOpen);

            InitializeAsyncCommand = new RelayCommand(async () =>
            {
#if !DEBUG
                await StartUpdateProcessAsync();
#endif
            });
        }

        /// <summary>
        /// Look for update then download and apply if any
        /// </summary>
        private async Task StartUpdateProcessAsync()
        {
            var watchStart = Stopwatch.StartNew();

            Logger.Info(
                "Looking for updates...");
            try
            {
                using (var updateManager = await UpdateManager.GitHubUpdateManager(Constants.GithubRepository))
                {
                    var updateInfo = await updateManager.CheckForUpdate();
                    if (updateInfo == null)
                    {
                        Logger.Error(
                            "Problem while trying to check new updates.");
                        return;
                    }

                    if (updateInfo.ReleasesToApply.Any())
                    {
                        Logger.Info(
                            $"A new update has been found!\n Currently installed version: {updateInfo.CurrentlyInstalledVersion?.Version?.Version.Major}.{updateInfo.CurrentlyInstalledVersion?.Version?.Version.Minor}.{updateInfo.CurrentlyInstalledVersion?.Version?.Version.Build} - New update: {updateInfo.FutureReleaseEntry?.Version?.Version.Major}.{updateInfo.FutureReleaseEntry?.Version?.Version.Minor}.{updateInfo.FutureReleaseEntry?.Version?.Version.Build}");

                        await updateManager.DownloadReleases(updateInfo.ReleasesToApply, x => Logger.Info(
                            $"Downloading new update... {x}%"));

                        var latestExe = await updateManager.ApplyReleases(updateInfo, x => Logger.Info(
                            $"Applying... {x}%"));

                        Logger.Info(
                            "A new update has been applied.");

                        var releaseInfos = string.Empty;
                        foreach (var releaseInfo in updateInfo.FetchReleaseNotes())
                        {
                            var info = releaseInfo.Value;

                            var pFrom = info.IndexOf("<p>", StringComparison.InvariantCulture) + "<p>".Length;
                            var pTo = info.LastIndexOf("</p>", StringComparison.InvariantCulture);

                            releaseInfos = string.Concat(releaseInfos, info.Substring(pFrom, pTo - pFrom),
                                Environment.NewLine);
                        }

                        var updateDialog =
                            new UpdateDialog(
                                new UpdateDialogSettings(
                                    LocalizationProviderHelper.GetLocalizedValue<string>("NewUpdateLabel"),
                                    LocalizationProviderHelper.GetLocalizedValue<string>("NewUpdateDescriptionLabel"),
                                    releaseInfos));
                        await _dialogCoordinator.ShowMetroDialogAsync(this, updateDialog);
                        var updateDialogResult = await updateDialog.WaitForButtonPressAsync();
                        await _dialogCoordinator.HideMetroDialogAsync(this, updateDialog);

                        if (!updateDialogResult.Restart) return;

                        Logger.Info(
                            "Restarting...");

                        Process.Start($@"{latestExe}\Popcorn.exe");
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        Logger.Info(
                            "No update available.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Something went wrong when trying to update app. {ex.Message}");
            }

            watchStart.Stop();
            var elapsedStartMs = watchStart.ElapsedMilliseconds;
            Logger.Info(
                $"Finished looking for updates in {elapsedStartMs}.");
        }

        /// <summary>
        /// Display a dialog on unhandled exception
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                Logger.Fatal(ex);
                ManageException(new Exception(LocalizationProviderHelper.GetLocalizedValue<string>("FatalError")));
            }
        }

        /// <summary>
        /// Manage an exception
        /// </summary>
        /// <param name="exception">The exception to manage</param>
        private void ManageException(Exception exception)
        {
            if (_isManagingException)
                return;

            _isManagingException = true;
            IsMovieFlyoutOpen = false;
            IsSettingsFlyoutOpen = false;

            if (exception is WebException || exception is SocketException)
                _applicationService.IsConnectionInError = true;

            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                var exceptionDialog =
                    new ExceptionDialog(
                        new ExceptionDialogSettings(
                            LocalizationProviderHelper.GetLocalizedValue<string>("EmbarrassingError"),
                            exception.Message));
                await _dialogCoordinator.ShowMetroDialogAsync(this, exceptionDialog);
                await exceptionDialog.WaitForButtonPressAsync();
                _isManagingException = false;
                await _dialogCoordinator.HideMetroDialogAsync(this, exceptionDialog);
            });
        }
    }
}