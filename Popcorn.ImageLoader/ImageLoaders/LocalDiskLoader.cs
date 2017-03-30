using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace Popcorn.ImageLoader.ImageLoaders
{
    internal class LocalDiskLoader: ILoader
    {

        public Stream Load(string source)
        {
            //Thread.Sleep(1000);
            return File.OpenRead(source);
        }
    }
}
