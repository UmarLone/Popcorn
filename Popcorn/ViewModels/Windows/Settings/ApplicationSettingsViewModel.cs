using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Popcorn.Models.Localization;
using Popcorn.Services.Language;

namespace Popcorn.ViewModels.Windows.Settings
{
    /// <summary>
    /// Application's settings
    /// </summary>
    public sealed class ApplicationSettingsViewModel : ViewModelBase
    {
        /// <summary>
        /// Services used to interacts with languages
        /// </summary>
        private readonly ILanguageService _languageService;

        /// <summary>
        /// The download limit
        /// </summary>
        private int _downloadLimit;

        /// <summary>
        /// The language used through the application
        /// </summary>
        private Language _language;

        /// <summary>
        /// The upload limit
        /// </summary>
        private int _uploadLimit;

        /// <summary>
        /// The version of the app
        /// </summary>
        private string _version;

        /// <summary>
        /// Initializes a new instance of the ApplicationSettingsViewModel class.
        /// </summary>
        public ApplicationSettingsViewModel(ILanguageService languageService)
        {
            _languageService = languageService;
            Version = Utils.Constants.AppVersion;
            RegisterCommands();
        }

        /// <summary>
        /// The download limit
        /// </summary>
        public int DownloadLimit
        {
            get { return _downloadLimit; }
            set { Set(() => DownloadLimit, ref _downloadLimit, value); }
        }

        /// <summary>
        /// The version of the app
        /// </summary>
        public string Version
        {
            get { return _version; }
            set { Set(() => Version, ref _version, value); }
        }

        /// <summary>
        /// The upload limit
        /// </summary>
        public int UploadLimit
        {
            get { return _uploadLimit; }
            set { Set(() => UploadLimit, ref _uploadLimit, value); }
        }

        /// <summary>
        /// The language used through the application
        /// </summary>
        public Language Language
        {
            get { return _language; }
            set { Set(() => Language, ref _language, value); }
        }

        /// <summary>
        /// Command used to initialize the settings asynchronously
        /// </summary>
        public RelayCommand InitializeAsyncCommand { get; private set; }

        /// <summary>
        /// Load asynchronously the languages of the application
        /// </summary>
        private async Task InitializeAsync()
        {
            Language = new Language(_languageService);
            await Language.LoadLanguages();
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
            => InitializeAsyncCommand = new RelayCommand(async () => await InitializeAsync());
    }
}