using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls.Dialogs;
using Popcorn.Messaging;
using Popcorn.Utils;
using Popcorn.Services.Download;
using Popcorn.Models.Media;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

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
        /// <param name="torrentPath">The torrent filePath</param>
        public DropTorrentDialogSettings(string torrentPath)
        {
            TorrentPath = torrentPath;
        }

        /// <summary>
        /// The torrent file path
        /// </summary>
        public string TorrentPath { get; }
    }

    /// <summary>
    /// Logique d'interaction pour DropTorrentDialog.xaml
    /// </summary>
    public partial class DropTorrentDialog : INotifyPropertyChanged, IDisposable
    {
        private readonly IDownloadService<MediaFile> _downloadService;

        private string _torrentPath;

        private double _downloadProgress;

        private double _downloadRate;

        private int _nbPeers;

        private int _nbSeeders;

        private bool _disposed;

        private CancellationTokenSource CancellationDownloadingToken { get; set; }

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

        public string TorrentPath
        {
            get => _torrentPath;
            set
            {
                _torrentPath = value;
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
        public DropTorrentDialog(DropTorrentDialogSettings settings)
        {
            InitializeComponent();
            _downloadService = new DownloadMediaService<MediaFile>();
            CancellationDownloadingToken = new CancellationTokenSource();
            TorrentPath = settings.TorrentPath;
            CancelCommand = new RelayCommand(() =>
            {
                CancellationDownloadingToken.Cancel(true);
            });

            Messenger.Default.Register<StopPlayMediaMessage>(this, e =>
            {
                CancelCommand.Execute(null);
            });
        }

        public async Task Download(int uploadLimit, int downloadLimit, Action buffered, Action cancelled)
        {
            TorrentType torrentType;
            torrentType = File.ReadLines(TorrentPath).Any(line => line.Contains("magnet"))
                ? TorrentType.Magnet
                : TorrentType.File;

            var media = new MediaFile();
            var downloadProgress = new Progress<double>(e =>
            {
                DownloadProgress = e;
            });

            var downloadRateProgress = new Progress<double>(e =>
            {
                DownloadRate = e;
            });

            var nbSeedsProgress = new Progress<int>(e =>
            {
                NbSeeders = e;
            });

            var nbPeersProgress = new Progress<int>(e =>
            {
                NbPeers = e;
            });

            await _downloadService.Download(media, torrentType, MediaType.Unkown, TorrentPath, uploadLimit,
                downloadLimit, downloadProgress, downloadRateProgress, nbSeedsProgress, nbPeersProgress, buffered,
                cancelled, CancellationDownloadingToken);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                CancellationDownloadingToken?.Dispose();
            }

            _disposed = true;
        }
    }
}