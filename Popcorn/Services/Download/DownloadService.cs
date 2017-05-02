using Popcorn.Utils;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using lt;
using NLog;
using System.IO;
using GalaSoft.MvvmLight.Messaging;
using Popcorn.Messaging;
using Popcorn.Models.Media;

namespace Popcorn.Services.Download
{
    public class DownloadService<T> : IDownloadService<T> where T : IMediaFile
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected virtual void BroadcastMediaBuffered(T media, Progress<double> reportDownloadProgress)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Download torrent
        /// </summary>
        /// <returns></returns>
        public async Task Download(T media, TorrentType torrentType, MediaType mediaType, string torrentPath, int uploadLimit, int downloadLimit, IProgress<double> downloadProgress,
            IProgress<double> downloadRate, IProgress<int> nbSeeds, IProgress<int> nbPeers, Action buffered, Action cancelled,
            CancellationTokenSource cts)
        {
            Logger.Info(
                $"Start downloading : {torrentPath}");
            await Task.Run(async () =>
            {
                using (var session = new session())
                {
                    var settings = session.settings();
                    settings.anonymous_mode = true;
                    downloadProgress.Report(0d);
                    downloadRate.Report(0d);
                    nbSeeds.Report(0);
                    nbPeers.Report(0);
                    session.listen_on(Constants.TorrentMinPort, Constants.TorrentMaxPort);
                    string savePath = string.Empty;
                    switch (mediaType)
                    {
                        case MediaType.Movie:
                            savePath = Constants.MovieDownloads;
                            break;
                        case MediaType.Show:
                            savePath = Constants.ShowDownloads;
                            break;
                        case MediaType.Unkown:
                            savePath = Constants.DropFilesDownloads;
                            break;
                    }

                    if (torrentType == TorrentType.File)
                    {
                        using (var addParams = new add_torrent_params
                        {
                            save_path = savePath,
                            ti = new torrent_info(torrentPath)
                        })
                        using (var handle = session.add_torrent(addParams))
                        {
                            await HandleDownload(media, mediaType, uploadLimit, downloadLimit, downloadProgress, downloadRate, nbSeeds, nbPeers, handle, session, buffered, cancelled, cts);
                        }
                    }
                    else
                    {
                        var magnet = new magnet_uri();
                        using (var error = new error_code())
                        {
                            var addParams = new add_torrent_params
                            {
                                save_path = savePath,
                            };
                            magnet.parse_magnet_uri(torrentPath, addParams, error);
                            using (var handle = session.add_torrent(addParams))
                            {
                                await HandleDownload(media, mediaType, uploadLimit, downloadLimit, downloadProgress, downloadRate, nbSeeds, nbPeers, handle, session, buffered, cancelled, cts);
                            }
                        }
                    }
                }
            });
        }

        private async Task HandleDownload(T media, MediaType type, int uploadLimit, int downloadLimit, IProgress<double> downloadProgress,
            IProgress<double> downloadRate, IProgress<int> nbSeeds, IProgress<int> nbPeers, torrent_handle handle, session session, Action buffered, Action cancelled, CancellationTokenSource cts)
        {
            handle.set_upload_limit(uploadLimit * 1024);
            handle.set_download_limit(downloadLimit * 1024);
            handle.set_sequential_download(true);
            var alreadyBuffered = false;
            while (!cts.IsCancellationRequested)
            {
                using (var status = handle.status())
                {
                    var progress = status.progress * 100d;
                    nbSeeds.Report(status.num_seeds);
                    nbPeers.Report(status.num_peers);
                    downloadProgress.Report(progress);
                    downloadRate.Report(Math.Round(status.download_rate / 1024d, 0));
                    handle.flush_cache();
                    if (handle.need_save_resume_data())
                        handle.save_resume_data(1);

                    double minimumBuffering;
                    switch (type)
                    {
                        case MediaType.Movie:
                            minimumBuffering = Constants.MinimumMovieBuffering;
                            break;
                        case MediaType.Show:
                            minimumBuffering = Constants.MinimumShowBuffering;
                            break;
                        default:
                            minimumBuffering = Constants.MinimumMovieBuffering;
                            break;
                    }

                    if (progress >= minimumBuffering && !alreadyBuffered)
                    {
                        buffered.Invoke();
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
                            media.FilePath = filePath;
                            BroadcastMediaBuffered(media, new Progress<double>(
                                e => { downloadProgress.Report(e); }));

                            break;
                        }

                        if (!alreadyBuffered)
                        {
                            session.remove_torrent(handle, 0);
                            if (type == MediaType.Unkown)
                            {
                                Messenger.Default.Send(
                                    new UnhandledExceptionMessage(
                                        new Exception("There is no media file in the torrent you dropped in the UI.")));
                            }
                            else
                            {
                                Messenger.Default.Send(
                                    new UnhandledExceptionMessage(
                                        new Exception("There is no media file in the torrent.")));
                            }

                            break;
                        }
                    }

                    if (status.is_finished)
                    {
                        session.remove_torrent(handle, 0);
                        break;
                    }

                    try
                    {
                        await Task.Delay(1000, cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        cancelled.Invoke();
                        break;
                    }
                }
            }
        }
    }
}