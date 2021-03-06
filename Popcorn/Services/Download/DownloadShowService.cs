﻿using GalaSoft.MvvmLight.Messaging;
using Popcorn.Messaging;
using Popcorn.Models.Episode;
using System;
using Popcorn.Models.Bandwidth;
using Popcorn.Models.Media;

namespace Popcorn.Services.Download
{
    /// <summary>
    /// Movie download service for torrent download
    /// </summary>
    /// <typeparam name="T"><see cref="IMediaFile"/></typeparam>
    public class DownloadShowService<T> : DownloadService<T> where T : EpisodeShowJson
    {
        /// <summary>
        /// Action to execute when a show has been buffered
        /// </summary>
        /// <param name="media"><see cref="IMediaFile"/></param>
        /// <param name="reportDownloadProgress">Download progress</param>
        /// <param name="reportBandwidthRate">The bandwidth rate</param>
        protected override void BroadcastMediaBuffered(T media, Progress<double> reportDownloadProgress, Progress<BandwidthRate> reportBandwidthRate)
        {
            Messenger.Default.Send(new PlayShowEpisodeMessage(media, reportDownloadProgress, reportBandwidthRate));
        }
    }
}
