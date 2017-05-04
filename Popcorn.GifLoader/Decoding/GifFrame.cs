using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Popcorn.GifLoader.Decoding
{
    internal class GifFrame : GifBlock
    {
        internal const int ImageSeparator = 0x2C;

        public GifImageDescriptor Descriptor { get; private set; }
        public IList<GifExtension> Extensions { get; private set; }

        private GifFrame()
        {
        }

        internal override GifBlockKind Kind => GifBlockKind.GraphicRendering;

        internal static GifFrame ReadFrame(Stream stream, IEnumerable<GifExtension> controlExtensions,
            bool metadataOnly)
        {
            var frame = new GifFrame();

            frame.Read(stream, controlExtensions, metadataOnly);

            return frame;
        }

        private void Read(Stream stream, IEnumerable<GifExtension> controlExtensions, bool metadataOnly)
        {
            // Note: at this point, the Image Separator (0x2C) has already been read

            Descriptor = GifImageDescriptor.ReadImageDescriptor(stream);
            if (Descriptor.HasLocalColorTable)
            {
                GifHelpers.ReadColorTable(stream, Descriptor.LocalColorTableSize);
            }
            GifImageData.ReadImageData(stream, metadataOnly);
            Extensions = controlExtensions.ToList().AsReadOnly();
        }
    }
}