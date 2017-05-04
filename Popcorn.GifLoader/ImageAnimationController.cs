using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Popcorn.GifLoader
{
    /// <summary>
    /// Provides a way to pause, resume or seek a GIF animation.
    /// </summary>
    public class ImageAnimationController : IDisposable
    {
        private static DependencyPropertyDescriptor SourceDescriptor { get; }

        static ImageAnimationController()
        {
            SourceDescriptor = DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof(Image));
        }

        private Image Image { get; }
        private ObjectAnimationUsingKeyFrames Animation { get; }
        private AnimationClock Clock { get; }
        private ClockController ClockController { get; }

        internal ImageAnimationController(Image image, ObjectAnimationUsingKeyFrames animation, bool autoStart)
        {
            Image = image;
            Animation = animation;
            Animation.Completed += AnimationCompleted;
            Clock = Animation.CreateClock();
            ClockController = Clock.Controller;
            SourceDescriptor.AddValueChanged(image, ImageSourceChanged);

            // ReSharper disable once PossibleNullReferenceException
            ClockController.Pause();

            Image.ApplyAnimationClock(Image.SourceProperty, Clock);

            if (autoStart)
                ClockController.Resume();
        }

        void AnimationCompleted(object sender, EventArgs e)
        {
            Image.RaiseEvent(new System.Windows.RoutedEventArgs(ImageBehavior.AnimationCompletedEvent, Image));
        }

        private void ImageSourceChanged(object sender, EventArgs e)
        {
            OnCurrentFrameChanged();
        }

        /// <summary>
        /// Returns the number of frames in the image.
        /// </summary>
        public int FrameCount
        {
            get { return Animation.KeyFrames.Count; }
        }

        /// <summary>
        /// Returns a value that indicates whether the animation is paused.
        /// </summary>
        public bool IsPaused
        {
            get { return Clock.IsPaused; }
        }

        /// <summary>
        /// Returns a value that indicates whether the animation is complete.
        /// </summary>
        public bool IsComplete
        {
            get { return Clock.CurrentState == ClockState.Filling; }
        }

        /// <summary>
        /// Seeks the animation to the specified frame index.
        /// </summary>
        /// <param name="index">The index of the frame to seek to</param>
        public void GotoFrame(int index)
        {
            var frame = Animation.KeyFrames[index];
            ClockController.Seek(frame.KeyTime.TimeSpan, TimeSeekOrigin.BeginTime);
        }

        /// <summary>
        /// Returns the current frame index.
        /// </summary>
        public int CurrentFrame
        {
            get
            {
                var time = Clock.CurrentTime;
                var frameAndIndex =
                    Animation.KeyFrames
                        .Cast<ObjectKeyFrame>()
                        .Select((f, i) => new {Time = f.KeyTime.TimeSpan, Index = i})
                        .FirstOrDefault(fi => fi.Time >= time);
                if (frameAndIndex != null)
                    return frameAndIndex.Index;
                return -1;
            }
        }

        /// <summary>
        /// Pauses the animation.
        /// </summary>
        public void Pause()
        {
            ClockController.Pause();
        }

        /// <summary>
        /// Starts or resumes the animation. If the animation is complete, it restarts from the beginning.
        /// </summary>
        public void Play()
        {
            ClockController.Resume();
        }

        /// <summary>
        /// Raised when the current frame changes.
        /// </summary>
        public event EventHandler CurrentFrameChanged;

        private void OnCurrentFrameChanged()
        {
            EventHandler handler = CurrentFrameChanged;
            handler?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Finalizes the current object.
        /// </summary>
        ~ImageAnimationController()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the current object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the current object
        /// </summary>
        /// <param name="disposing">true to dispose both managed an unmanaged resources, false to dispose only managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Image.BeginAnimation(Image.SourceProperty, null);
                Animation.Completed -= AnimationCompleted;
                SourceDescriptor.RemoveValueChanged(Image, ImageSourceChanged);
                Image.Source = null;
            }
        }
    }
}