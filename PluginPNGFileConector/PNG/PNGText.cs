using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Compression;

namespace Blastagon.FileText.PNG
{
    public class PNGText
    {

        //=========================================================================================

        byte[] byte_data;
        List<ChankData> chanks = new List<ChankData>();
        List<iTXtData> list_iTXt = new List<iTXtData>();

        public PNGText(string file_path)
        {
            //ファイルを開く
            var fs = new System.IO.FileStream(
                file_path,
                System.IO.FileMode.Open,
                System.IO.FileAccess.Read);

            //ファイルを読み込むバイト型配列を作成する
            byte_data = new byte[fs.Length];

            //ファイルの内容をすべて読み込む
            fs.Read(byte_data, 0, byte_data.Length);

            //閉じる
            fs.Close();

            uint read_pos = 0;

            var text = "";
            text = GetChankHeadKey(read_pos, 4);
            if (text != "?PNG")
            {
                // PNGファイルではない可能性が高い
                // err
                return;
            }

            read_pos += 8;

            while (true)
            {
                // チャンクの情報を読み込む
                var cd = new ChankData(ref byte_data, (int)read_pos);
                chanks.Add(cd);

                read_pos += cd.size; // チャンクの挿入を考えるとあまり依存した対応はしないほうが良いかも


                if (cd.key == "IEND") break;

                if (cd.key == "zTXt") // 圧縮情報を読むのが難しいようなので、いったん放棄
                {
                    text = GetChankStringToZero(cd.pos + 8, 80);

                    var def_data_start_pos = cd.pos + 8 + text.Count() + 1 + 1 + 2;
                    var def_data_size = cd.size - 12 - text.Count() - 2 - 2;
                    text = GetChankStringAsDeflate((uint)def_data_start_pos, (int)def_data_size);
                }
                if (cd.key == "iTXt")
                {
                    // 無圧縮前提で処理する...
                    text = GetChankStringToZero(cd.pos + 8, 80);

                    var t = new iTXtData();
                    t.owner_chank = cd;
                    t.key = text;

                    var def_data_start_pos = cd.pos + 8 + text.Count() + 1 + 1 + 3;
                    var def_data_size = cd.size - 12 - text.Count() - 2 - 3;

                    text = GetChankString(def_data_start_pos, (int)def_data_size);
                    t.value = text;

                    list_iTXt.Add(t);
                }

            }
        }

        private string GetChankHeadKey(uint read_pos, int key_length)
        {
            var bd = new byte[key_length];
            for (var i = 0; i < key_length; i++) bd[i] = byte_data[i + read_pos];

            var text = System.Text.Encoding.ASCII.GetString(bd);
            return text;
        }

        private int GetChankSize(int read_pos)
        {
            const int offset = 0;
            const int key_length = 4;
            var bd = new byte[key_length];
            var bd2 = new byte[key_length];
            for (var i = 0; i < key_length; i++) bd[i] = byte_data[i + offset + read_pos];
            for (var i = 0; i < key_length; i++) bd2[i] = bd[key_length - i - 1]; // エンディアンのため逆順に

            var size = BitConverter.ToInt32(bd2, 0);

            return size;
        }

        private string GetChankStringToZero(int read_pos, int key_length = 256)
        {
            for (var i = 0; i < key_length; i++)
            {
                if (byte_data[i + read_pos] == 0)
                {
                    key_length = i;
                    break;
                }
            }

            var bd = new byte[key_length];
            for (var i = 0; i < key_length; i++)
            {
                bd[i] = byte_data[i + read_pos];
            }

            var text = System.Text.Encoding.UTF8.GetString(bd);
            return text;
        }

        private string GetChankStringAsDeflate(uint read_pos, int key_length)
        {
            var bd = new byte[key_length];
            var bd2 = new byte[1024];
            for (var i = 0; i < key_length; i++) bd[i] = byte_data[i + read_pos];

            // 圧縮の記事、メモで残しておきます
            // https://code.msdn.microsoft.com/windowsdesktop/10-C-08886908

            var ms = new MemoryStream(bd);
            var ms2 = new MemoryStream();
            // 参考記事
            // http://d.hatena.ne.jp/s-kita/20090502/1241260697
            using (DeflateStream deflateStream = new DeflateStream(ms, CompressionMode.Decompress))
            {
                //buffer = new Byte[1024];
                while (true)
                {
                    var buffer = new Byte[1024];

                    //Deflateで圧縮されたファイルからデータを読み込む
                    Int32 readBytes = deflateStream.Read(buffer, 0, buffer.Length);
                    if (readBytes == 0)
                    {
                        break;
                    }

                    //解凍されたデータを書き込む
                    ms2.Write(buffer, 0, readBytes);
                }
            }

            // 圧縮されたデータを バイト配列で取得します 
            bd2 = ms2.ToArray();
            var text = System.Text.Encoding.ASCII.GetString(bd2);

            return text;
        }

        private string GetChankString(int read_pos, int key_length)
        {
            var bd = new byte[key_length];
            for (var i = 0; i < key_length; i++) bd[i] = byte_data[i + read_pos];

            //var text = System.Text.Encoding.ASCII.GetString(bd);
            var text = System.Text.Encoding.UTF8.GetString(bd);
            return text;
        }

        public iTXtData[] GetChankiTXt(string key)
        {
            var res = new List<iTXtData>();

            foreach (var t in list_iTXt)
            {
                if (t.key == key) res.Add(t);
            }

            return res.ToArray();
        }

        public void InsertChankiTXt(string key, string value)
        {
            var cd = new ChankData();
            cd.key = "iTXt";
            cd.pos = 0;
            cd.size = 0;

            var t = new iTXtData();
            t.key = key;
            t.owner_chank = cd;
            t.value = value;

            var i = 0;
            foreach (var cd2 in chanks)
            {
                if (cd2.key == "IDAT")
                {
                    chanks.Insert(i, cd);
                    break;
                }
                i++;
            }
            t.Update();

        }

        public void Save(string file_path)
        {
            using (FileStream fs = new FileStream(file_path,
                FileMode.Create, FileAccess.Write))
            {
                byte[] head = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

                fs.Write(head, 0, head.Length);

                foreach (var c in chanks)
                {
                    fs.Write(c.data_full, 0, c.data_full.Length);
                }
            }
        }


    }
}