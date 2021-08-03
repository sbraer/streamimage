using System;
using System.IO;

namespace StreamImage
{
    public class ImageCreatorEventArgs : EventArgs
    {
        public MemoryStream Bitmap { private set; get; }

        public ImageCreatorEventArgs(in MemoryStream bitmap)
        {
            Bitmap = bitmap;
        }
    }
}
