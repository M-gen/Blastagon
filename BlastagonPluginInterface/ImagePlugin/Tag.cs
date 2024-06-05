using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlastagonPluginInterface.ImagePlugin
{
    // 画像ごとのタグ情報
    public class Tag
    {
        public string name;
        public int good_count; // 良さ・星
        public string ex_word; // 拡張文字列
        public Tag(string name, int good_count, string ex_word)
        {
            this.name = name;
            this.good_count = good_count;
            this.ex_word = ex_word;
        }
    }
}
