using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using GalaSoft.MvvmLight.Messaging;
using Meta.Vlc.Wpf;
using Popcorn.Messaging;

namespace Popcorn.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Messenger.Default.Register<DropFileMessage>(this, e =>
            {
                if (e.Event == DropFileMessage.DropFileEvent.Enter)
                {
                    BorderThickness = new Thickness(1);
                    BorderBrush = (SolidColorBrush) new BrushConverter().ConvertFrom("#CCE51400");
                    GlowBrush = (SolidColorBrush) new BrushConverter().ConvertFrom("#CCE51400");
                    DoubleAnimation da = new DoubleAnimation
                    {
                        To = 0.5d,
                        Duration = new Duration(TimeSpan.FromMilliseconds(750)),
                        EasingFunction = new PowerEase
                        {
                            EasingMode = EasingMode.EaseInOut,
                            Power = 2d
                        }
                    };
                    BeginAnimation(OpacityProperty, da);
                }
                else
                {
                    BorderThickness = new Thickness(0);
                    BorderBrush = Brushes.Transparent;
                    GlowBrush = Brushes.Transparent;
                    DoubleAnimation da = new DoubleAnimation
                    {
                        To = 1.0d,
                        Duration = new Duration(TimeSpan.FromMilliseconds(750)),
                        EasingFunction = new PowerEase
                        {
                            EasingMode = EasingMode.EaseInOut,
                            Power = 2d
                        }
                    };
                    BeginAnimation(OpacityProperty, da);
                }
            });
        }

        /// <summary>
        /// On window closing, release VLC instance
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            ApiManager.ReleaseAll();
            base.OnClosing(e);
        }
    }
}