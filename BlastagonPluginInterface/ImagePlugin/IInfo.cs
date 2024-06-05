using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlastagonPluginInterface.ImagePlugin
{
    public abstract class IInfo : IDisposable
    {
        public bool IsTagConect;    // タグの操作が可能かどうか
        public bool IsImageLoad;    // 画像の読み込みが可能かどうか
        public string file_type;

        //public bool is_read = false; // 読み込み可能かどうか
        //public virtual bool CheckRead(string file_path){ return false; } // 読み込めるかどうかを確認

        public virtual void Dispose(){}
    }
}
