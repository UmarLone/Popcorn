using System.IO;

namespace Popcorn.GifLoader.Decoding
{
    internal class GifImageData
    {
        private byte LzwMinimumCodeSize { get; set; }
        private byte[] CompressedData { get; set; }

        private GifImageData()
        {
        }

        internal static GifImageData ReadImageData(Stream stream, bool metadataOnly)
        {
            var imgData = new GifImageData();
            imgData.Read(stream, metadataOnly);
            return imgData;
        }

        private void Read(Stream stream, bool metadataOnly)
        {
            LzwMinimumCodeSize = (byte) stream.ReadByte();
            CompressedData = GifHelpers.ReadDataBlocks(stream, metadataOnly);
        }
    }
}