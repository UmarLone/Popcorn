using System.IO;
using System.Text;

namespace Popcorn.GifLoader.Decoding
{
    internal class GifCommentExtension : GifExtension
    {
        internal const int ExtensionLabel = 0xFE;

        private string Text { get; set; }

        private GifCommentExtension()
        {
        }

        internal override GifBlockKind Kind => GifBlockKind.SpecialPurpose;

        internal static GifCommentExtension ReadComment(Stream stream)
        {
            var comment = new GifCommentExtension();
            comment.Read(stream);
            return comment;
        }

        private void Read(Stream stream)
        {
            // Note: at this point, the label (0xFE) has already been read

            var bytes = GifHelpers.ReadDataBlocks(stream, false);
            if (bytes != null)
                Text = Encoding.ASCII.GetString(bytes);
        }
    }
}