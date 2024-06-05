using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlastagonPluginInterface.ImagePlugin
{
    public abstract class ITagConector : IDisposable
    {
        public virtual void Write(string file_path, string tags_word)
        {
            throw new System.ArgumentException("実装されていません", "ImagePlugin Err");
        }

        public virtual List<Tag> Read(string file_path)
        {
            throw new System.ArgumentException("実装されていません", "ImagePlugin Err");
        }
        public virtual void Dispose() {}
    }
}
