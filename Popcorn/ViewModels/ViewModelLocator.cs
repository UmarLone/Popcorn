using System.Diagnostics.CodeAnalysis;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using Popcorn.Models.ApplicationState;
using Popcorn.Services.Language;
using Popcorn.Services.Movies.History;
using Popcorn.Services.Movies.Movie;
using Popcorn.Services.Movies.Trailer;
using Popcorn.Services.Settings;
using Popcorn.Services.Shows.Show;
using Popcorn.Services.Subtitles;
using Popcorn.ViewModels.Pages.Home;
using Popcorn.ViewModels.Pages.Home.Anime;
using Popcorn.ViewModels.Pages.Home.Movie;
using Popcorn.ViewModels.Pages.Home.Movie.Details;
using Popcorn.ViewModels.Pages.Home.Show;
using Popcorn.ViewModels.Windows;
using Popcorn.ViewModels.Windows.Settings;

namespace Popcorn.ViewModels
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            #region Services

            SimpleIoc.Default.Register<ISettingsService, SettingsService>();
            SimpleIoc.Default.Register<IMovieService, MovieService>();
            SimpleIoc.Default.Register<ILanguageService, LanguageService>();
            SimpleIoc.Default.Register<IMovieHistoryService, MovieHistoryService>();
            SimpleIoc.Default.Register<IMovieTrailerService, MovieTrailerService>();
            SimpleIoc.Default.Register<IApplicationService, ApplicationService>();
            SimpleIoc.Default.Register<ISubtitlesService, SubtitlesService>();
            SimpleIoc.Default.Register<IShowService, ShowService>();

            #endregion

            #region ViewModels

            SimpleIoc.Default.Register<WindowViewModel>();
            SimpleIoc.Default.Register<PagesViewModel>();

            SimpleIoc.Default.Register<MoviePageViewModel>();
            SimpleIoc.Default.Register<MovieDetailsViewModel>();

            SimpleIoc.Default.Register<AnimePageViewModel>();

            SimpleIoc.Default.Register<ShowPageViewModel>();

            SimpleIoc.Default.Register<ApplicationSettingsViewModel>();

            #endregion
        }

        /// <summary>
        /// Gets the Window property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public WindowViewModel Window => ServiceLocator.Current.GetInstance<WindowViewModel>();

        /// <summary>
        /// Gets the Pages property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public PagesViewModel Pages => ServiceLocator.Current.GetInstance<PagesViewModel>();

        /// <summary>
        /// Gets the MovieDetails property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public MovieDetailsViewModel MovieDetails => ServiceLocator.Current.GetInstance<MovieDetailsViewModel>();

        /// <summary>
        /// Gets the ApplicationSettings property.
        /// </summary>
        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public ApplicationSettingsViewModel ApplicationSettings
            => ServiceLocator.Current.GetInstance<ApplicationSettingsViewModel>();
    }
}