using GalaSoft.MvvmLight.Messaging;
using Popcorn.Messaging;
using Popcorn.Models.Episode;
using System;

namespace Popcorn.Services.Download
{
    public class DownloadShowService<T> : DownloadService<T> where T : EpisodeShowJson
    {
        protected override void BroadcastMediaBuffered(T media, Progress<double> reportDownloadProgress)
        {
            Messenger.Default.Send(new PlayShowEpisodeMessage(media, reportDownloadProgress));
        }
    }
}
