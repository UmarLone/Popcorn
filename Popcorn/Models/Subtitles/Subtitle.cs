using GalaSoft.MvvmLight;

namespace Popcorn.Models.Subtitles
{
    public class Subtitle : ObservableObject
    {
        private string _filePath;
        private OSDB.Subtitle _subtitle;

        /// <summary>
        /// Subtitle
        /// </summary>
        public OSDB.Subtitle Sub
        {
            get { return _subtitle; }
            set { Set(() => Sub, ref _subtitle, value); }
        }

        /// <summary>
        /// Subtitle file path
        /// </summary>
        public string FilePath
        {
            get { return _filePath; }
            set { Set(() => FilePath, ref _filePath, value); }
        }
    }
}