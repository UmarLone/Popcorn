using Popcorn.Utils.Exceptions;

namespace Popcorn.ImageLoader.ImageLoaders
{
    internal static class LoaderFactory
    {
        public static ILoader CreateLoader(SourceType sourceType)
        {
            switch (sourceType)
            {
                case SourceType.LocalDisk:
                    return new LocalDiskLoader();
                case SourceType.ExternalResource:
                    return new ExternalLoader();
                default:
                    throw new PopcornException("Unexpected exception");
            }
        }
    }
}