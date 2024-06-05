using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;

namespace BlastagonPluginInterface.ImagePlugin
{
    public abstract class IImageConector : IDisposable
    {
        public virtual Bitmap FromFile(Image image, string file_path, string ex_word)
        {
            throw new System.ArgumentException("実装されていません", "ImagePlugin Err");
        }
        public virtual Bitmap FromFile(Image image, string file_path, string ex_word, int w, int h, out int src_image_w, out int src_image_h)
        {
            throw new System.ArgumentException("実装されていません", "ImagePlugin Err");
        }
        public virtual Bitmap FromFile(Image image, string file_path, string ex_word, int w, out int src_image_w, out int src_image_h)
        {
            throw new System.ArgumentException("実装されていません", "ImagePlugin Err");
        }
        public virtual void Dispose() { }
    }
}
