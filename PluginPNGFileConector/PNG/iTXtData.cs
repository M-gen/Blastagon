using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blastagon.FileText.PNG
{

    public class iTXtData
    {
        public ChankData owner_chank;
        public string key;
        public string value;

        /// <summary>
        /// 更新し、紐づくPngChankerのデータを新しくします（save用）
        /// </summary>
        public void Update()
        {
            switch (owner_chank.key)
            {
                case "iTXt":
                    {
                        byte[] key_d = System.Text.Encoding.ASCII.GetBytes(key);
                        //byte[] value_d = System.Text.Encoding.ASCII.GetBytes(value);
                        byte[] value_d = System.Text.Encoding.UTF8.GetBytes(value);

                        var size = key_d.Length + 1   // キーワード+"/0"
                            + 1 + 1 +                 // 圧縮フラグ(1) + 圧縮形式(1)
                            + 1                       // 言語タグ "/0" (1)
                            + 1                       // 翻訳キーワード+"/0" (1)
                            + value_d.Length;         // 値 ※ここは最後"/0"が無いので注意
                        var bd = new byte[size];
                        var pos = 0;

                        // キーワード
                        for (var i = 0; i < key.Length; i++)
                        {
                            bd[i + pos] = key_d[i];
                        }
                        pos += key.Length;
                        bd[pos] = 0;
                        pos += 1;

                        // 圧縮フラグ(1) + 圧縮形式(1) + 言語タグ+"/0" (1) + 翻訳キーワード+"/0" (1)
                        for (var i = 0; i < 4; i++)
                        {
                            bd[i + pos] = 0;
                        }
                        pos += +4;

                        // 値+"/0"
                        for (var i = 0; i < value_d.Length; i++)
                        {
                            bd[i + pos] = value_d[i];
                        }
                        pos += value_d.Length;

                        owner_chank.SetData(ref bd);
                    }
                    break;

            }
        }
    }
}
