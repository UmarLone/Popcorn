using System.Globalization;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Threading;
using Popcorn.Helpers;
using Popcorn.Models.Localization;
using Popcorn.Services.User;
using Popcorn.Utils;

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
        private readonly IUserService _userService;

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
        /// Cache size
        /// </summary>
        private string _cacheSize;

        /// <summary>
        /// Initializes a new instance of the ApplicationSettingsViewModel class.
        /// </summary>
        public ApplicationSettingsViewModel(IUserService userService)
        {
            _userService = userService;
            Version = Constants.AppVersion;
            RefreshCacheSize();
            RegisterCommands();

            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                DownloadLimit = await _userService.GetDownloadLimit();
                UploadLimit = await _userService.GetUploadLimit();
            });
        }

        /// <summary>
        /// The download limit
        /// </summary>
        public int DownloadLimit
        {
            get => _downloadLimit;
            set
            {
                Set(() => DownloadLimit, ref _downloadLimit, value);
                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    await _userService.SetDownloadLimit(value);
                });
            }
        }

        /// <summary>
        /// The version of the app
        /// </summary>
        public string Version
        {
            get => _version;
            set { Set(() => Version, ref _version, value); }
        }

        /// <summary>
        /// Cache size
        /// </summary>
        public string CacheSize
        {
            get => _cacheSize;
            set { Set(() => CacheSize, ref _cacheSize, value); }
        }

        /// <summary>
        /// The upload limit
        /// </summary>
        public int UploadLimit
        {
            get => _uploadLimit;
            set
            {
                Set(() => UploadLimit, ref _uploadLimit, value);
                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                {
                    await _userService.SetUploadLimit(value);
                });
            }
        }

        /// <summary>
        /// The language used through the application
        /// </summary>
        public Language Language
        {
            get => _language;
            set { Set(() => Language, ref _language, value); }
        }

        /// <summary>
        /// Command used to initialize the settings asynchronously
        /// </summary>
        public RelayCommand InitializeAsyncCommand { get; private set; }

        /// <summary>
        /// Clear the cache
        /// </summary>
        public RelayCommand ClearCacheCommand { get; private set; }

        /// <summary>
        /// Update size cache
        /// </summary>
        public RelayCommand UpdateCacheSizeCommand { get; private set; }

        /// <summary>
        /// Load asynchronously the languages of the application
        /// </summary>
        private async Task InitializeAsync()
        {
            Language = new Language(_userService);
            await Language.LoadLanguages();
        }

        /// <summary>
        /// Register commands
        /// </summary>
        private void RegisterCommands()
        {
            InitializeAsyncCommand = new RelayCommand(async () => await InitializeAsync());
            UpdateCacheSizeCommand = new RelayCommand(RefreshCacheSize);
            ClearCacheCommand = new RelayCommand(() =>
            {
                FileHelper.DeleteFolder(Constants.Assets);
                RefreshCacheSize();
            });
        }

        /// <summary>
        /// Refresh cache size
        /// </summary>
        private void RefreshCacheSize()
        {
            var cache = FileHelper.GetDirectorySize(Constants.Assets);
            CacheSize =
                (cache / 1024 / 1024)
                .ToString(CultureInfo.InvariantCulture);
        }
    }
}