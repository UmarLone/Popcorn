using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using NLog;
using Popcorn.Exceptions;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Windows;
using WPFLocalizeExtension.Engine;

namespace Popcorn
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Splash screen dispatcher
        /// </summary>
        private Dispatcher _splashScreenDispatcher;

        /// <summary>
        /// Watcher
        /// </summary>
        private static Stopwatch _watchStart;

        /// <summary>
        /// Initializes a new instance of the App class.
        /// </summary>
        static App()
        {
            Logger.Info(
                "Popcorn starting...");
            _watchStart = Stopwatch.StartNew();

            Directory.CreateDirectory(Utils.Constants.Logging);

            DispatcherHelper.Initialize();

            LocalizeDictionary.Instance.SetCurrentThreadCulture = true;
        }

        /// <summary>
        /// Initializes a new instance of the App class.
        /// </summary>
        public App()
        {
            DispatcherUnhandledException += AppDispatcherUnhandledException;
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        /// <summary>
        /// On startup, register synchronization context
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AsyncSynchronizationContext.Register();
        }

        /// <summary>
        /// Observe unhandled exceptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            CurrentDomainUnhandledException(sender, new UnhandledExceptionEventArgs(e.Exception, false));
        }

        /// <summary>
        /// Handle unhandled expceptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            CurrentDomainUnhandledException(sender, new UnhandledExceptionEventArgs(e.Exception, false));
        }

        /// <summary>
        /// When an unhandled exception has been thrown, handle it
        /// </summary>
        /// <param name="sender"><see cref="App"/> instance</param>
        /// <param name="e">DispatcherUnhandledExceptionEventArgs args</param>
        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            CurrentDomainUnhandledException(sender, new UnhandledExceptionEventArgs(e.Exception, false));
        }

        /// <summary>
        /// When an unhandled exception domain has been thrown, handle it
        /// </summary>
        /// <param name="sender"><see cref="App"/> instance</param>
        /// <param name="e">UnhandledExceptionEventArgs args</param>
        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                Logger.Fatal(ex);
                Messenger.Default.Send(
                    new UnhandledExceptionMessage(
                        new PopcornException(LocalizationProviderHelper.GetLocalizedValue<string>("FatalError"))));
            }
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var splashScreenThread = new Thread(() =>
            {
                var splashScreen = new Windows.SplashScreen();
                _splashScreenDispatcher = splashScreen.Dispatcher;
                splashScreen.Show();
                Dispatcher.Run();
                splashScreen.Close();
            });

            splashScreenThread.SetApartmentState(ApartmentState.STA);
            splashScreenThread.Start();

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Loaded += async (sender2, e2) =>
                await mainWindow.Dispatcher.InvokeAsync(() =>
                {
                    _splashScreenDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                    mainWindow.Activate();
                    _watchStart.Stop();
                    var elapsedStartMs = _watchStart.ElapsedMilliseconds;
                    Logger.Info(
                        $"Popcorn started in {elapsedStartMs} milliseconds.");
                });

            mainWindow.Show();
        }
    }
}