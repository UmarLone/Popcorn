using System.IO;

namespace Popcorn.GifLoader.Decoding
{
    internal class GifHeader : GifBlock
    {
        private string Signature { get; set; }
        private string Version { get; set; }
        public GifLogicalScreenDescriptor LogicalScreenDescriptor { get; private set; }

        private GifHeader()
        {
        }

        internal override GifBlockKind Kind => GifBlockKind.Other;

        internal static GifHeader ReadHeader(Stream stream)
        {
            var header = new GifHeader();
            header.Read(stream);
            return header;
        }

        private void Read(Stream stream)
        {
            Signature = GifHelpers.ReadString(stream, 3);
            if (Signature != "GIF")
                throw GifHelpers.InvalidSignatureException(Signature);
            Version = GifHelpers.ReadString(stream, 3);
            if (Version != "87a" && Version != "89a")
                throw GifHelpers.UnsupportedVersionException(Version);
            LogicalScreenDescriptor = GifLogicalScreenDescriptor.ReadLogicalScreenDescriptor(stream);
        }
    }
}