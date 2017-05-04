using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Threading;
using Popcorn.ImageLoader.ImageLoaders;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Popcorn.ImageLoader
{
    internal sealed class Manager
    {
        private class LoadImageRequest
        {
            public bool IsCanceled { get; set; }
            public string Source { get; set; }
            public Stream Stream { get; set; }
            public Image Image { get; set; }
        }

        #region Properties

        private readonly Dictionary<Image, LoadImageRequest> _imagesLastRunningTask =
            new Dictionary<Image, LoadImageRequest>();

        private readonly Queue<LoadImageRequest> _loadThumbnailQueue = new Queue<LoadImageRequest>();
        private readonly Queue<LoadImageRequest> _loadNormalQueue = new Queue<LoadImageRequest>();

        private readonly AutoResetEvent _loaderThreadThumbnailEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _loaderThreadNormalSizeEvent = new AutoResetEvent(false);

        private readonly DrawingImage _loadingImage;
        private readonly DrawingImage _errorThumbnail;
        private readonly TransformGroup _loadingAnimationTransform;

        #endregion

        #region Singleton Implementation

        private Manager()
        {
            #region Creates Loading Threads

            var loaderThreadForThumbnails = new Thread(async () => { await LoaderThreadThumbnails(); })
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            // otherwise, the app won't quit with the UI...
            loaderThreadForThumbnails.Start();

            var loaderThreadForNormalSize = new Thread(async () => { await LoaderThreadNormalSize(); })
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            // otherwise, the app won't quit with the UI...
            loaderThreadForNormalSize.Start();

            #endregion

            #region Loading Images from Resources

            var resourceDictionary = new ResourceDictionary
            {
                Source = new Uri("Popcorn.ImageLoader;component/Resources.xaml", UriKind.Relative)
            };

            _loadingImage = resourceDictionary["ImageLoading"] as DrawingImage;
            _loadingImage.Freeze();
            _errorThumbnail = resourceDictionary["ImageError"] as DrawingImage;
            _errorThumbnail.Freeze();

            #endregion

            # region Create Loading Animation

            var scaleTransform = new ScaleTransform(0.5, 0.5);
            var skewTransform = new SkewTransform(0, 0);
            var rotateTransform = new RotateTransform(0);
            var translateTransform = new TranslateTransform(0, 0);

            var group = new TransformGroup();
            group.Children.Add(scaleTransform);
            group.Children.Add(skewTransform);
            group.Children.Add(rotateTransform);
            group.Children.Add(translateTransform);

            var doubleAnimation =
                new DoubleAnimation(0, 359, new TimeSpan(0, 0, 0, 1)) {RepeatBehavior = RepeatBehavior.Forever};

            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, doubleAnimation);

            _loadingAnimationTransform = group;

            #endregion
        }

        public static Manager Instance { get; } = new Manager();

        #endregion

        #region Public Methods

        public void LoadImage(string source, Image image)
        {
            var loadTask = new LoadImageRequest() {Image = image, Source = source};

            // Begin Loading
            BeginLoading(image, loadTask);

            lock (_loadThumbnailQueue)
            {
                _loadThumbnailQueue.Enqueue(loadTask);
            }

            _loaderThreadThumbnailEvent.Set();
        }

        #endregion

        #region Private Methods

        private void BeginLoading(Image image, LoadImageRequest loadTask)
        {
            lock (_imagesLastRunningTask)
            {
                if (_imagesLastRunningTask.ContainsKey(image))
                {
                    // Cancel previous loading...
                    _imagesLastRunningTask[image].IsCanceled = true;
                    _imagesLastRunningTask[image] = loadTask;
                }
                else
                {
                    _imagesLastRunningTask.Add(image, loadTask);
                }
            }

            image.Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                // Set IsLoading Pty
                Loader.SetIsLoading(image, true);

                if (Equals(image.RenderTransform, Transform.Identity))
                    // Don't apply loading animation if image already has transform...
                {
                    // Manage Waiting Image Parameter
                    if (Loader.GetDisplayWaitingAnimationDuringLoading(image))
                    {
                        image.Source = _loadingImage;
                        image.RenderTransformOrigin = new Point(0.5, 0.5);
                        image.RenderTransform = _loadingAnimationTransform;
                    }
                }
            }));
        }

        private void EndLoading(Image image, ImageSource imageSource, LoadImageRequest loadTask, bool markAsFinished)
        {
            lock (_imagesLastRunningTask)
            {
                if (_imagesLastRunningTask.ContainsKey(image))
                {
                    if (_imagesLastRunningTask[image].Source != loadTask.Source)
                        return; // if the last launched task for this image is not this one, abort it!

                    if (markAsFinished)
                        _imagesLastRunningTask.Remove(image);
                }
                else
                {
                    /* ERROR! */
                    System.Diagnostics.Debug.WriteLine(
                        "EndLoading() - unexpected condition: there is no running task for this image!");
                }

                image.Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    if (image.RenderTransform == _loadingAnimationTransform)
                    {
                        image.RenderTransform = Transform.Identity;
                    }

                    if (Loader.GetErrorDetected(image) && Loader.GetDisplayErrorThumbnailOnError(image))
                    {
                        imageSource = _errorThumbnail;
                    }

                    image.Source = imageSource;

                    if (markAsFinished)
                    {
                        // Set IsLoading Pty
                        Loader.SetIsLoading(image, false);
                    }
                }));
            }
        }

        private async Task<ImageSource> GetBitmapSource(LoadImageRequest loadTask, DisplayOptions loadType)
        {
            Image image = loadTask.Image;
            string source = loadTask.Source;

            ImageSource imageSource = null;

            if (!string.IsNullOrWhiteSpace(source))
            {
                Stream imageStream = null;
                var sourceType = SourceType.ExternalResource;

                image.Dispatcher.Invoke(new ThreadStart(delegate
                {
                    sourceType = Loader.GetSourceType(image);
                }));

                try
                {
                    if (loadTask.Stream == null)
                    {
                        var loader = LoaderFactory.CreateLoader(sourceType);
                        imageStream = await loader.Load(source);
                        loadTask.Stream = imageStream;
                    }
                    else
                    {
                        imageStream = new MemoryStream();
                        loadTask.Stream.Position = 0;
                        loadTask.Stream.CopyTo(imageStream);
                        imageStream.Position = 0;
                    }
                }
                catch (Exception)
                {
                    // TODO
                }

                if (imageStream != null)
                {
                    try
                    {
                        if (loadType == DisplayOptions.Preview)
                        {
                            var bitmapFrame = BitmapFrame.Create(imageStream);
                            imageSource = bitmapFrame.Thumbnail;

                            if (imageSource == null) // Preview it is not embedded into the file
                            {
                                // we'll make a thumbnail image then ... (too bad as the pre-created one is FAST!)
                                var thumbnail = new TransformedBitmap();
                                thumbnail.BeginInit();
                                thumbnail.Source = bitmapFrame;

                                // we'll make a reasonable sized thumnbail with a height of 400
                                var pixelH = bitmapFrame.PixelHeight;
                                var pixelW = bitmapFrame.PixelWidth;
                                var decodeH = 400;
                                var decodeW = (bitmapFrame.PixelWidth * decodeH) / pixelH;
                                var scaleX = decodeW / (double) pixelW;
                                var scaleY = decodeH / (double) pixelH;
                                var transformGroup = new TransformGroup();
                                transformGroup.Children.Add(new ScaleTransform(scaleX, scaleY));
                                thumbnail.Transform = transformGroup;
                                thumbnail.EndInit();

                                // this will disconnect the stream from the image completely ...
                                var writable = new WriteableBitmap(thumbnail);
                                writable.Freeze();
                                imageSource = writable;
                            }
                        }
                        else if (loadType == DisplayOptions.FullResolution)
                        {
                            var bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.StreamSource = imageStream;
                            bitmapImage.EndInit();
                            imageSource = bitmapImage;
                        }
                    }
                    catch (Exception)
                    {
                        // TODO
                    }
                }

                if (imageSource == null)
                {
                    await image.Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        Loader.SetErrorDetected(image, true);
                    }));
                }
                else
                {
                    imageSource.Freeze();

                    await image.Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        Loader.SetErrorDetected(image, false);
                    }));
                }
            }
            else
            {
                await image.Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Loader.SetErrorDetected(image, false);
                }));
            }

            return imageSource;
        }

        private async Task LoaderThreadThumbnails()
        {
            do
            {
                _loaderThreadThumbnailEvent.WaitOne();

                LoadImageRequest loadTask;

                do
                {

                    lock (_loadThumbnailQueue)
                    {
                        loadTask = _loadThumbnailQueue.Count > 0 ? _loadThumbnailQueue.Dequeue() : null;
                    }

                    if (loadTask != null && !loadTask.IsCanceled)
                    {
                        var displayOption = DisplayOptions.Preview;

                        loadTask.Image.Dispatcher.Invoke(new ThreadStart(delegate
                        {
                            displayOption = Loader.GetDisplayOption(loadTask.Image);
                        }));

                        var bitmapSource = await GetBitmapSource(loadTask, DisplayOptions.Preview);

                        if (displayOption == DisplayOptions.Preview)
                        {
                            EndLoading(loadTask.Image, bitmapSource, loadTask, true);
                        }
                        else if (displayOption == DisplayOptions.FullResolution)
                        {
                            EndLoading(loadTask.Image, bitmapSource, loadTask, false);

                            lock (_loadNormalQueue)
                            {
                                _loadNormalQueue.Enqueue(loadTask);
                            }

                            _loaderThreadNormalSizeEvent.Set();
                        }
                    }

                } while (loadTask != null);

            } while (true);
        }

        private async Task LoaderThreadNormalSize()
        {
            do
            {
                _loaderThreadNormalSizeEvent.WaitOne();

                LoadImageRequest loadTask;

                do
                {

                    lock (_loadNormalQueue)
                    {
                        loadTask = _loadNormalQueue.Count > 0 ? _loadNormalQueue.Dequeue() : null;
                    }

                    if (loadTask != null && !loadTask.IsCanceled)
                    {
                        var bitmapSource = await GetBitmapSource(loadTask, DisplayOptions.FullResolution);
                        EndLoading(loadTask.Image, bitmapSource, loadTask, true);
                    }

                } while (loadTask != null);

            } while (true);
        }

        #endregion
    }
}