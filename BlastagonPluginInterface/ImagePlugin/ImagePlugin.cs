using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlastagonPluginInterface.ImagePlugin
{
    public class ImagePlugin : IDisposable
    {
        public IInfo info;
        public ITagConector tag_conector;
        public IImageConector image_conector;

        public virtual void Dispose()
        {
            info.Dispose();
            tag_conector.Dispose();
            image_conector.Dispose();
        }
    }
}
