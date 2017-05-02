using GalaSoft.MvvmLight.Messaging;
using Popcorn.Messaging;
using Popcorn.Models.Media;
using System;

namespace Popcorn.Services.Download
{
    public class DownloadMediaService<T> : DownloadService<T> where T : MediaFile
    {
        protected override void BroadcastMediaBuffered(T media, Progress<double> reportDownloadProgress)
        {
            Messenger.Default.Send(new PlayMediaMessage(media.FilePath, reportDownloadProgress));
        }
    }
}