using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Popcorn.GifLoader.Decoding
{
    internal class GifFile
    {
        public GifHeader Header { get; private set; }
        public IList<GifFrame> Frames { get; private set; }
        private IList<GifExtension> Extensions { get; set; }
        public ushort RepeatCount { get; private set; }

        private GifFile()
        {
        }

        internal static GifFile ReadGifFile(Stream stream, bool metadataOnly)
        {
            var file = new GifFile();
            file.Read(stream, metadataOnly);
            return file;
        }

        private void Read(Stream stream, bool metadataOnly)
        {
            Header = GifHeader.ReadHeader(stream);

            if (Header.LogicalScreenDescriptor.HasGlobalColorTable)
            {
                GifHelpers.ReadColorTable(stream, Header.LogicalScreenDescriptor.GlobalColorTableSize);
            }

            ReadFrames(stream, metadataOnly);

            var netscapeExtension =
                Extensions
                    .OfType<GifApplicationExtension>()
                    .FirstOrDefault(GifHelpers.IsNetscapeExtension);

            if (netscapeExtension != null)
                RepeatCount = GifHelpers.GetRepeatCount(netscapeExtension);
            else
                RepeatCount = 1;
        }

        private void ReadFrames(Stream stream, bool metadataOnly)
        {
            var frames = new List<GifFrame>();
            var controlExtensions = new List<GifExtension>();
            var specialExtensions = new List<GifExtension>();
            while (true)
            {
                var block = GifBlock.ReadBlock(stream, controlExtensions, metadataOnly);

                if (block.Kind == GifBlockKind.GraphicRendering)
                    controlExtensions = new List<GifExtension>();

                var frame = block as GifFrame;
                if (frame != null)
                {
                    frames.Add(frame);
                }
                else if (block is GifExtension)
                {
                    var extension = (GifExtension) block;
                    switch (extension.Kind)
                    {
                        case GifBlockKind.Control:
                            controlExtensions.Add(extension);
                            break;
                        case GifBlockKind.SpecialPurpose:
                            specialExtensions.Add(extension);
                            break;
                    }
                }
                else if (block is GifTrailer)
                {
                    break;
                }
            }

            this.Frames = frames.AsReadOnly();
            this.Extensions = specialExtensions.AsReadOnly();
        }
    }
}