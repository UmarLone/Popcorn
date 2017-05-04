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

        private Dictionary<Image, LoadImageRequest> ImagesLastRunningTask { get; } =
            new Dictionary<Image, LoadImageRequest>();

        private Queue<LoadImageRequest> LoadThumbnailQueue { get; } = new Queue<LoadImageRequest>();
        private Queue<LoadImageRequest> LoadNormalQueue { get; } = new Queue<LoadImageRequest>();

        private AutoResetEvent LoaderThreadThumbnailEvent { get; } = new AutoResetEvent(false);
        private AutoResetEvent LoaderThreadNormalSizeEvent { get; } = new AutoResetEvent(false);

        private DrawingImage LoadingImage { get; }
        private DrawingImage ErrorThumbnail { get; }
        private TransformGroup LoadingAnimationTransform { get; }

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

            LoadingImage = resourceDictionary["ImageLoading"] as DrawingImage;
            LoadingImage.Freeze();
            ErrorThumbnail = resourceDictionary["ImageError"] as DrawingImage;
            ErrorThumbnail.Freeze();

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

            LoadingAnimationTransform = group;

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

            lock (LoadThumbnailQueue)
            {
                LoadThumbnailQueue.Enqueue(loadTask);
            }

            LoaderThreadThumbnailEvent.Set();
        }

        #endregion

        #region Private Methods

        private void BeginLoading(Image image, LoadImageRequest loadTask)
        {
            lock (ImagesLastRunningTask)
            {
                if (ImagesLastRunningTask.ContainsKey(image))
                {
                    // Cancel previous loading...
                    ImagesLastRunningTask[image].IsCanceled = true;
                    ImagesLastRunningTask[image] = loadTask;
                }
                else
                {
                    ImagesLastRunningTask.Add(image, loadTask);
                }
            }

            image.Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                // Set IsLoading Pty
                Loader.SetIsLoading(image, true);

                if (Equals(image.RenderTransform, Transform.Identity) &&
                    Loader.GetDisplayWaitingAnimationDuringLoading(image))
                {
                    // Don't apply loading animation if image already has transform...
                    // Manage Waiting Image Parameter
                    image.Source = LoadingImage;
                    image.RenderTransformOrigin = new Point(0.5, 0.5);
                    image.RenderTransform = LoadingAnimationTransform;
                }
            }));
        }

        private void EndLoading(Image image, ImageSource imageSource, LoadImageRequest loadTask, bool markAsFinished)
        {
            lock (ImagesLastRunningTask)
            {
                if (ImagesLastRunningTask.ContainsKey(image))
                {
                    if (ImagesLastRunningTask[image].Source != loadTask.Source)
                        return; // if the last launched task for this image is not this one, abort it!

                    if (markAsFinished)
                        ImagesLastRunningTask.Remove(image);
                }
                else
                {
                    /* ERROR! */
                    System.Diagnostics.Debug.WriteLine(
                        "EndLoading() - unexpected condition: there is no running task for this image!");
                }

                image.Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    if (image.RenderTransform == LoadingAnimationTransform)
                    {
                        image.RenderTransform = Transform.Identity;
                    }

                    if (Loader.GetErrorDetected(image) && Loader.GetDisplayErrorThumbnailOnError(image))
                    {
                        imageSource = ErrorThumbnail;
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
                    // An issue occured while getting stream.
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
                        // An issue occured while creating bitmap image
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
                LoaderThreadThumbnailEvent.WaitOne();

                LoadImageRequest loadTask;

                do
                {

                    lock (LoadThumbnailQueue)
                    {
                        loadTask = LoadThumbnailQueue.Count > 0 ? LoadThumbnailQueue.Dequeue() : null;
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

                            lock (LoadNormalQueue)
                            {
                                LoadNormalQueue.Enqueue(loadTask);
                            }

                            LoaderThreadNormalSizeEvent.Set();
                        }
                    }

                } while (loadTask != null);

            } while (true);
        }

        private async Task LoaderThreadNormalSize()
        {
            do
            {
                LoaderThreadNormalSizeEvent.WaitOne();

                LoadImageRequest loadTask;

                do
                {

                    lock (LoadNormalQueue)
                    {
                        loadTask = LoadNormalQueue.Count > 0 ? LoadNormalQueue.Dequeue() : null;
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