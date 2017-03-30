using System.Diagnostics;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Popcorn.Messaging;
using Popcorn.Models.Shows;

namespace Popcorn.ViewModels.Pages.Home.Show.Details
{
    public class ShowDetailsViewModel : ViewModelBase
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The show
        /// </summary>
        private ShowJson _show;

        public ShowDetailsViewModel()
        {
            RegisterCommands();
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            LoadShowCommand = new RelayCommand<ShowJson>(async movie => await LoadShow(movie));
        }

        /// <summary>
        /// Command used to load the movie
        /// </summary>
        public RelayCommand<ShowJson> LoadShowCommand { get; private set; }

        /// <summary>
        /// The show
        /// </summary>
        public ShowJson Show
        {
            get { return _show; }
            set { Set(ref _show, value); }
        }

        /// <summary>
        /// Load the requested show
        /// </summary>
        /// <param name="show">The show to load</param>
        private async Task LoadShow(ShowJson show)
        {
            var watch = Stopwatch.StartNew();

            Messenger.Default.Send(new LoadShowMessage());
            Show = show;

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Logger.Debug($"LoadShow ({show.ImdbId}) in {elapsedMs} milliseconds.");
        }
    }
}