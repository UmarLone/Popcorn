using GalaSoft.MvvmLight.Messaging;
using Popcorn.Messaging;
using Popcorn.Models.Movie;
using System;

namespace Popcorn.Services.Download
{
    public class DownloadMovieService<T> : DownloadService<T> where T : MovieJson
    {
        protected override void BroadcastMediaBuffered(T media, Progress<double> reportDownloadProgress)
        {
            Messenger.Default.Send(new PlayMovieMessage(media, reportDownloadProgress));
        }
    }
}
