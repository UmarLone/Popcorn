using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Popcorn.Helpers
{
    public static class DownloadFileHelper
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Downloads a file from a specified Internet address.
        /// </summary>
        /// <param name="remotePath">Internet address of the file to download.</param>
        /// <param name="localPath">
        /// Local file name where to store the content of the download, if null a temporary file name will
        /// be generated.
        /// </param>
        /// <param name="timeOut">Duration in miliseconds before cancelling the  operation.</param>
        /// <param name="progress">Report the progress of the download</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task<Tuple<string, string, Exception>> DownloadFileTaskAsync(string remotePath,
            string localPath = null, int timeOut = int.MaxValue, IProgress<long> progress = null,
            CancellationTokenSource ct = null)
        {
            var watch = Stopwatch.StartNew();

            try
            {
                if (remotePath == null)
                {
                    Logger.Debug("DownloadFileTaskAsync (null remote path): skipping");
                    throw new ArgumentNullException(nameof(remotePath));
                }

                if (localPath == null)
                {
                    Logger.Debug(
                        $"DownloadFileTaskAsync (null local path): generating a temporary file name for {remotePath}");
                    localPath = Path.GetTempFileName();
                }

                if (File.Exists(localPath))
                {
                    var fileInfo = new FileInfo(localPath).Length;
                    if (fileInfo != 0)
                        return new Tuple<string, string, Exception>(remotePath, localPath, null);
                }

                var direcory = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(direcory) && !Directory.Exists(direcory))
                    Directory.CreateDirectory(direcory);

                // Sometimes YTS requires an authentication to access movies. Here is an already authenticated cookie valid until December 2017
                var cookieJar = new CookieContainer();
                cookieJar.Add(new Cookie("PHPSESSID", "ncrmefe4s79la9d2mba0n538o6", "/", "yts.ag"));
                cookieJar.Add(new Cookie("__cfduid", "d6c81b283d74b436fec66f02bcb99c04d1481020053", "/", ".yts.ag"));
                cookieJar.Add(new Cookie("bgb2", "1", "/", "yts.ag"));
                cookieJar.Add(new Cookie("uhh", "60a8c98fd72e7a79b731f1ea09a5d09d", "/", ".yts.ag"));
                cookieJar.Add(new Cookie("uid", "2188423", "/", ".yts.ag"));
                using (var client = new CookieAwareWebClient(cookieJar))
                {
                    if (progress != null)
                        client.DownloadProgressChanged +=
                            (sender, e) => progress.Report(e.BytesReceived / e.TotalBytesToReceive);

                    TimerCallback timerCallback = c =>
                    {
                        var webClient = (WebClient)c;
                        if (!webClient.IsBusy) return;
                        webClient.CancelAsync();
                        Logger.Debug($"DownloadFileTaskAsync (time out due): {remotePath}");
                    };

                    try
                    {
                        using (new Timer(timerCallback, client, timeOut, Timeout.Infinite))
                        {
                            if (ct != null)
                                await client.DownloadFileTaskAsync(remotePath, localPath, ct.Token);
                            else
                                await client.DownloadFileTaskAsync(remotePath, localPath);
                        }
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        watch.Stop();
                        Logger.Debug(
                            "DownloadFileTaskAsync cancelled.");
                        return new Tuple<string, string, Exception>(remotePath, null, exception);
                    }
                    catch (ObjectDisposedException exception)
                    {
                        watch.Stop();
                        Logger.Info(
                            $"DownloadFileTaskAsync (can't cancel download, it has finished previously): {remotePath}");
                        return new Tuple<string, string, Exception>(remotePath, null, exception);
                    }
                    catch (WebException exception)
                    {
                        watch.Stop();
                        Logger.Error($"DownloadFileTaskAsync: {exception.Message}");
                        return new Tuple<string, string, Exception>(remotePath, null, exception);
                    }
                }
            }
            catch (Exception ex)
            {
                watch.Stop();
                Logger.Error(
                    $"DownloadFileTaskAsync (download failed): {remotePath} Additional informations : {ex.Message}");
                return new Tuple<string, string, Exception>(remotePath, null, ex);
            }
            finally
            {
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Logger.Debug($"DownloadFileTaskAsync (downloaded in {elapsedMs} ms): {remotePath}");
            }

            return new Tuple<string, string, Exception>(remotePath, localPath, null);
        }
    }
}