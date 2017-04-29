using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using lt;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using Popcorn.Messaging;
using Popcorn.ViewModels.Windows.Settings;

namespace Popcorn.Dialogs
{
    /// <summary>
    /// Manage exception settings
    /// </summary>
    public class DropTorrentDialogSettings : MetroDialogSettings
    {
        /// <summary>
        /// Initialize a new instance of DropTorrentDialogSettings
        /// </summary>
        /// <param name="filePath">The torrent filePath</param>
        /// <param name="hideDialog">The hideDialog action</param>
        public DropTorrentDialogSettings(string filePath, Action<BaseMetroDialog> hideDialog)
        {
            FilePath = filePath;
            HideDialog = hideDialog;
        }

        /// <summary>
        /// The torrent file path
        /// </summary>
        public Action<BaseMetroDialog> HideDialog { get; }

        /// <summary>
        /// The torrent file path
        /// </summary>
        public string FilePath { get; }
    }

    /// <summary>
    /// Logique d'interaction pour DropTorrentDialog.xaml
    /// </summary>
    public partial class DropTorrentDialog : INotifyPropertyChanged
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private Action<BaseMetroDialog> _hideDialog;

        private string _filePath;

        private string _torrentTitle;

        private double _downloadProgress;

        private double _downloadRate;

        private int _nbPeers;

        private int _nbSeeders;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        private ICommand _cancelCommand;

        public ICommand CancelCommand
        {
            get => _cancelCommand;
            set
            {
                _cancelCommand = value;
                OnPropertyChanged();
            }
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged();
            }
        }

        public string TorrentTitle
        {
            get => _torrentTitle;
            set
            {
                _torrentTitle = value;
                OnPropertyChanged();
            }
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                _downloadProgress = value;
                OnPropertyChanged();
            }
        }

        public double DownloadRate
        {
            get => _downloadRate;
            set
            {
                _downloadRate = value;
                OnPropertyChanged();
            }
        }

        public int NbPeers
        {
            get => _nbPeers;
            set
            {
                _nbPeers = value;
                OnPropertyChanged();
            }
        }

        public int NbSeeders
        {
            get => _nbSeeders;
            set
            {
                _nbSeeders = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Initialize a new instance of ExceptionDialog
        /// </summary>
        /// <param name="settings">The dialog settings</param>
        internal DropTorrentDialog(DropTorrentDialogSettings settings)
        {
            InitializeComponent();
            FilePath = settings.FilePath;
            _hideDialog = settings.HideDialog;
            CancelCommand = new RelayCommand(() =>
            {
                _cts.Cancel();
            });

            Messenger.Default.Register<StopPlayMediaMessage>(this, e =>
            {
                CancelCommand.Execute(null);
            });
        }

        /// <summary>
        /// Asynchronous task, waiting for button press event to complete
        /// </summary>
        /// <returns></returns>
        internal async Task DownloadTorrentFile()
        {
            await Task.Run(async () =>
            {
                using (var session = new session())
                {
                    Logger.Info(
                        $"Start downloading : {FilePath}");

                    DownloadProgress = 0d;
                    DownloadRate = 0d;
                    NbSeeders = 0;
                    NbPeers = 0;

                    session.listen_on(6881, 6889);
                    using (var addParams = new add_torrent_params
                    {
                        save_path = Utils.Constants.MovieDownloads,
                        ti = new torrent_info(FilePath)
                    })
                    using (var handle = session.add_torrent(addParams))
                    {
                        var applicationSettingsViewModel =
                            SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
                        handle.set_upload_limit(applicationSettingsViewModel.UploadLimit * 1024);
                        handle.set_download_limit(applicationSettingsViewModel.DownloadLimit * 1024);
                        // We have to download sequentially, so that we're able to play the movie without waiting
                        handle.set_sequential_download(true);
                        var alreadyBuffered = false;
                        while (!_cts.IsCancellationRequested)
                        {
                            using (var status = handle.status())
                            {
                                var progress = status.progress * 100d;

                                NbSeeders = status.num_seeds;
                                NbPeers = status.num_peers;
                                DownloadProgress = progress;
                                DownloadRate = Math.Round(status.download_rate / 1024d, 0);
                                handle.flush_cache();
                                if (handle.need_save_resume_data())
                                    handle.save_resume_data(1);

                                if (progress >= Utils.Constants.MinimumMovieBuffering && !alreadyBuffered)
                                {
                                    // Get movie file
                                    foreach (
                                        var filePath in
                                        Directory
                                            .GetFiles(status.save_path, "*.*",
                                                SearchOption.AllDirectories)
                                            .Where(s => s.Contains(handle.torrent_file().name()) &&
                                                        (s.EndsWith(".mp4") || s.EndsWith(".mkv") ||
                                                         s.EndsWith(".mov") || s.EndsWith(".avi")))
                                    )
                                    {
                                        alreadyBuffered = true;
                                        Messenger.Default.Send(new PlayMediaMessage(filePath, new Progress<double>(e =>
                                        {
                                            DownloadProgress = e;
                                        })));
                                        _hideDialog.Invoke(this);
                                    }
                                }

                                try
                                {
                                    await Task.Delay(1000, _cts.Token);
                                }
                                catch (TaskCanceledException)
                                {
                                    _hideDialog.Invoke(this);
                                    break;
                                }
                            }
                        }
                    }
                }
            });
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}