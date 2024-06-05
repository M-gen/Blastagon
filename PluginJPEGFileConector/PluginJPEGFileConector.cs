using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing; // 参照で追加
using System.IO;

using Blastagon.JPEG;
using BlastagonPluginInterface.ImagePlugin;

namespace Plugin
{
    public class Info : IInfo
    {
        Dictionary<string, bool> check_read_log = new Dictionary<string, bool>();
         
        public Info()
        {
            IsTagConect = true;
            IsImageLoad = true;
            file_type = "JPEG,JPG";
        }
        public override void Dispose()
        {
        }
    }

    public class TagConector : ITagConector
    {
        const int JPEG_EXIF_ID = 601;

        // 
        private static class TmpFilePath
        {
            public static string Generate(int index = 0)
            {
                var tmp = string.Format(@"data/tmp/tmp_{0:004x}.jpeg", index);

                if (System.IO.File.Exists(tmp))
                {
                    return Generate(index + 1);
                }
                return tmp;
            }
        }

        public TagConector()
        {
        }

        public override void Write(string file_path, string tags_word)
        {
            var word = tags_word;

            // 一時ファイルを作って保存する
            // 単数しか扱わない
            var tmp_file_path = TmpFilePath.Generate();
            var jpeg_text = new JPEGText(file_path);
            jpeg_text.SaveText(tmp_file_path, JPEG_EXIF_ID, word );

            // 一時ファイルと、前の画像が、ピクセルとして一致しているか確認
            if (Blastagon.Common.Draw.CheckImageCompare(file_path, tmp_file_path))
            {
                File.Delete(file_path);
                File.Move(tmp_file_path, file_path);
                return;
            }
            else
            {
                //File.Delete(tmp_file_path);
                throw (new System.Exception(file_path + " タグ書き込み後のファイルが一致しません : PluginJPEGFileConector"));
            }

        }

        public override List<Tag> Read(string file_path)
        {
            var tags = new List<Tag>();

            var jpeg_text = new JPEGText(file_path);
            var strings = jpeg_text.GetText(JPEG_EXIF_ID).Split('\n');
            foreach (var str in strings)
            {
                if (str == "") continue;
                var tag_split = str.Replace(@"\,", "\t").Split('\t');
                //var ex_
                //var tag = new Tag(tag_split[0], int.Parse(tag_split[1]));
                var ex_word = "";
                if (tag_split.Count() > 2) ex_word = tag_split[2];

                var tag = new Tag(tag_split[0], int.Parse(tag_split[1]), ex_word);
                tags.Add(tag);
            }

            return tags;
        }
    }

    public class ImageConector : IImageConector
    {

        public override Bitmap FromFile( Image image, string file_path, string ex_word)
        {
            if (image != null)
            {
                var bitmap = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                using (var g = Graphics.FromImage(bitmap))
                {
                    g.DrawImage(image, 0, 0, bitmap.Width, bitmap.Height);
                }

                return bitmap;
            }
            else
            {
                try
                {
                    using (image = Image.FromFile(file_path))
                    {
                        var bitmap = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.DrawImage(image, 0, 0, bitmap.Width, bitmap.Height);
                        }
                        return bitmap;
                    }
                }
                catch
                {
                    return null;
                }
            }
        }
        public override Bitmap FromFile(Image image, string file_path, string ex_word, int w, int h, out int src_image_w, out int src_image_h)
        {
            var clip = new Rectangle(0,0,0,0);
            Helper.Tag.AnalyzeExWord(ex_word, ref clip);

            if (image != null)
            {
                var bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                using (var g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.DrawImage(image, 0, 0, w, h);
                }

                src_image_w = image.Width;
                src_image_h = image.Height;
                return bitmap;
            }
            else
            {
                using (image = Image.FromFile(file_path))
                {
                    var bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    using (var g = Graphics.FromImage(bitmap))
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        if ( clip.Width>0 )
                        {
                            var dst_rect = new Rectangle(0, 0, w, h);
                            g.DrawImage(image, dst_rect, clip, GraphicsUnit.Pixel);
                        }
                        else {
                            g.DrawImage(image, 0, 0, w, h);
                        }
                    }

                    src_image_w = image.Width;
                    src_image_h = image.Height;
                    return bitmap;
                }

            }
        }

        public override Bitmap FromFile(Image image, string file_path, string ex_word, int w, out int src_image_w, out int src_image_h)
        {
            if (image != null)
            {
                var h = w * image.Height / image.Width;
                var bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                using (var g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.DrawImage(image, 0, 0, w, h);
                }

                src_image_w = image.Width;
                src_image_h = image.Height;
                return bitmap;
            }
            else
            {
                using (FileStream fs = File.OpenRead(file_path))
                using (Image img = Image.FromStream(fs, false, false))
                //using (image = Image.FromFile(file_path))
                {
                    var h = w * image.Height / image.Width;
                    var bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    using (var g = Graphics.FromImage(bitmap))
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.DrawImage(image, 0, 0, w, h);
                    }

                    src_image_w = image.Width;
                    src_image_h = image.Height;
                    return bitmap;
                }

            }
        }
    }

}
