using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Popcorn.AttachedProperties
{
    /// <summary>
    /// Image async
    /// </summary>
    public class ImageAsyncHelper : DependencyObject
    {
        /// <summary>
        /// Get source uri
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Uri GetSourceUri(DependencyObject obj)
        {
            return (Uri)obj.GetValue(SourceUriProperty);
        }

        /// <summary>
        /// Set source uri
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetSourceUri(DependencyObject obj, Uri value)
        {
            obj.SetValue(SourceUriProperty, value);
        }

        /// <summary>
        /// Source Uri property
        /// </summary>
        public static readonly DependencyProperty SourceUriProperty =
            DependencyProperty.RegisterAttached("SourceUri",
                typeof(Uri),
                typeof(ImageAsyncHelper),
                new PropertyMetadata
                {
                    PropertyChangedCallback = (obj, e) =>
                        ((Image)obj).SetBinding(
                            Image.SourceProperty,
                            new Binding("VerifiedUri")
                            {
                                Source = new ImageAsyncHelper
                                {
                                    _givenUri = (Uri)e.NewValue
                                },
                                IsAsync = true
                            }
                        )
                }
            );

        private Uri _givenUri;

        public Uri VerifiedUri
        {
            get
            {
                try
                {
                    if (_givenUri == null) return null;
                    System.Net.Dns.GetHostEntry(_givenUri.DnsSafeHost);
                    return _givenUri;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}
