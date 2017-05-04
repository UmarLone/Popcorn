using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Popcorn.GifLoader.Decoding
{
    // label 0x01
    internal class GifPlainTextExtension : GifExtension
    {
        internal const int ExtensionLabel = 0x01;

        private int BlockSize { get; set; }
        private int Left { get; set; }
        private int Top { get; set; }
        private int Width { get; set; }
        private int Height { get; set; }
        private int CellWidth { get; set; }
        private int CellHeight { get; set; }
        private int ForegroundColorIndex { get; set; }
        private int BackgroundColorIndex { get; set; }
        private string Text { get; set; }

        private IList<GifExtension> Extensions { get; set; }

        private GifPlainTextExtension()
        {
        }

        internal override GifBlockKind Kind => GifBlockKind.GraphicRendering;

        internal static GifPlainTextExtension ReadPlainText(Stream stream, IEnumerable<GifExtension> controlExtensions,
            bool metadataOnly)
        {
            var plainText = new GifPlainTextExtension();
            plainText.Read(stream, controlExtensions, metadataOnly);
            return plainText;
        }

        private void Read(Stream stream, IEnumerable<GifExtension> controlExtensions, bool metadataOnly)
        {
            // Note: at this point, the label (0x01) has already been read

            byte[] bytes = new byte[13];
            stream.ReadAll(bytes, 0, bytes.Length);

            BlockSize = bytes[0];
            if (BlockSize != 12)
                throw GifHelpers.InvalidBlockSizeException("Plain Text Extension", 12, BlockSize);

            Left = BitConverter.ToUInt16(bytes, 1);
            Top = BitConverter.ToUInt16(bytes, 3);
            Width = BitConverter.ToUInt16(bytes, 5);
            Height = BitConverter.ToUInt16(bytes, 7);
            CellWidth = bytes[9];
            CellHeight = bytes[10];
            ForegroundColorIndex = bytes[11];
            BackgroundColorIndex = bytes[12];

            var dataBytes = GifHelpers.ReadDataBlocks(stream, metadataOnly);
            Text = Encoding.ASCII.GetString(dataBytes);
            Extensions = controlExtensions.ToList().AsReadOnly();
        }
    }
}