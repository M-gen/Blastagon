using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;

using Blastagon.Common;
using Blastagon.UI;
using Blastagon.UI.Common;
using Blastagon.UI.Menu;
using Blastagon.ResourceManage;
using Blastagon.ThreadManager;
using Blastagon.PluginFileConector;

// OpenCVカスケードの学習データの参考
// https://blog.aidemy.net/entry/2018/05/21/210100

namespace Blastagon.App.ExportData
{
    public class ExportCVData
    {
        int size = 32;


        public ExportCVData()
        {

        }

        public void ExportPositive(Blastagon.App.ImageLibrary.Tag tag )
        {
            var out_file_path = "export/pos.txt";

            var i = 0;
            //ファイルを上書きし、UTF-8で書き込む
            using (var sw = new System.IO.StreamWriter(
                out_file_path,
                false,
                //System.Text.Encoding.GetEncoding("UTF-8")))
                System.Text.Encoding.GetEncoding("Shift_JIS")))
            {
                //foreach (var tag in image_tag.tag)
                foreach( var image in Blastagon.App.AppCore.core.image_library.image_datas)
                {
                    ImageLibrary.ImageTag image_tag = null;
                    var tag2s = new List<ImageLibrary.ImageTag>();
                    {
                        var is_ok = false;
                        foreach( var tag2 in image.tags)
                        {
                            if( tag2.tag==tag )
                            {
                                is_ok = true;
                                image_tag = tag2;
                                tag2s.Add(tag2);
                                //break;
                            }
                        }
                        if (!is_ok) continue;
                    }

                    var file_type = image.file_path.Substring(image.file_path.LastIndexOf('.')+1);
                    var image_file_path  = $@"export/pos/{i}.{file_type}";
                    var image_file_path2 = $@"pos/{i}.{file_type}";
                    if (image_tag.ex_word != "")
                    {
                        if (tag2s.Count() == 1)
                        {
                            var clip = new Rectangle(0, 0, 0, 0);
                            var is_clip = false;
                            var image_data_ex_word = new ImageLibrary.ImageDataExWordSet(image, image_tag.ex_word);
                            image_data_ex_word.AnalyzeExWord(ref clip, ref is_clip);

                            sw.WriteLine($"{image_file_path2} 1 {clip.X} {clip.Y} {clip.Width} {clip.Height}");
                        }
                        else
                        {
                            var num = tag2s.Count();
                            var str = "";
                            foreach (var tag2 in tag2s)
                            {
                                var clip = new Rectangle(0, 0, 0, 0);
                                var is_clip = false;
                                var image_data_ex_word = new ImageLibrary.ImageDataExWordSet(image, image_tag.ex_word);
                                image_data_ex_word.AnalyzeExWord(ref clip, ref is_clip);
                                str += $"{clip.X} {clip.Y} {clip.Width} {clip.Height} ";
                            }
                            sw.WriteLine($"{image_file_path2} {num} {str}");

                        }
                    }

                    File.Copy(image.file_path, image_file_path, true);
                    i++;
                }
            }

            //using (var sw = new System.IO.StreamWriter(
            //    @"export/lean_step1.bat",
            //    false,
            //    //System.Text.Encoding.GetEncoding("UTF-8")))
            //    System.Text.Encoding.GetEncoding("Shift_JIS")))
            //{
            //    sw.WriteLine($"opencv_createsamples.exe -info pos.txt -vec pos.vec -num {i} -w {size} -h {size}");
            //    sw.WriteLine($"pause");
            //}
        }

        public void ExportNegative(Blastagon.App.ImageLibrary.Tag tag)
        {
            var out_file_path = "export/ng.txt";
            var i = 0;

            //ファイルを上書きし、UTF-8で書き込む
            using (var sw = new System.IO.StreamWriter(
                out_file_path,
                false,
                //System.Text.Encoding.GetEncoding("UTF-8")))
                System.Text.Encoding.GetEncoding("Shift_JIS")))
            {
                //foreach (var tag in image_tag.tag)
                foreach (var image in Blastagon.App.AppCore.core.image_library.image_datas)
                {
                    ImageLibrary.ImageTag image_tag = null;
                    {
                        var is_ok = false;
                        foreach (var tag2 in image.tags)
                        {
                            if (tag2.tag == tag)
                            {
                                is_ok = true;
                                image_tag = tag2;
                                break;
                            }
                        }
                        if (!is_ok) continue;
                    }

                    var file_type = image.file_path.Substring(image.file_path.LastIndexOf('.') + 1);
                    var image_file_path = $@"export/ng/{i}.{file_type}";
                    var image_file_path2 = $@"ng/{i}.{file_type}";
                    sw.WriteLine($"{image_file_path2}");

                    if (image_tag.ex_word != "")
                    {
                        var clip = new Rectangle(0, 0, 0, 0);
                        var is_clip = false;
                        var image_data_ex_word = new ImageLibrary.ImageDataExWordSet(image, image_tag.ex_word);
                        image_data_ex_word.AnalyzeExWord(ref clip, ref is_clip); // クリップされているので、調整が必要

                        //sw.WriteLine($"{image_file_path2} 1 {clip.X} {clip.Y} {clip.Width} {clip.Height}");
                        var plugin = FileConectorManager.GetFileConector( image.file_path, false);

                        //using (var  image2 = (Bitmap)plugin.image_conector.FromFile(null, image.file_path, ""))
                        using (var  image2 = new Bitmap(image.file_path)) // 画像取得
                        {
                            var image3 = new Bitmap(clip.Width, clip.Height);
                            var g = Graphics.FromImage(image3);
                            //g.DrawImage(image2, -clip.X, -clip.Y, clip.Width, clip.Height);
                            g.DrawImage(image2, 0, 0, new Rectangle(clip.X, clip.Y, clip.Width, clip.Height), GraphicsUnit.Pixel );
                            image3.Save(image_file_path);
                        }
                    }
                    else
                    {
                        File.Copy(image.file_path, image_file_path, true);

                    }
                    i++;
                }
            }

        }

        public void SetupClearDirectory( string path )
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                Directory.Delete(path, true);
                Directory.CreateDirectory(path);
            }
        }


        public void ExportCNNData(Blastagon.App.ImageLibrary.Tag tag)
        {
            var out_dir_root = @"export/cnn";
            var size = 64;

            SetupClearDirectory(out_dir_root);

            var i = 0;
            foreach (var tag_chiled in tag.chiled)
            {
                var dir_path = out_dir_root + $"/{i}_{tag_chiled.name}";
                Directory.CreateDirectory(dir_path);
                var i2 = 0;
                foreach (var image in Blastagon.App.AppCore.core.image_library.image_datas)
                {
                    // タグの検出
                    ImageLibrary.ImageTag image_tag = null;
                    {
                        var is_ok = false;
                        foreach (var tag2 in image.tags)
                        {
                            if (tag2.tag == tag_chiled)
                            {
                                is_ok = true;
                                image_tag = tag2;
                                break;
                            }
                        }
                        if (!is_ok) continue;
                    }

                    using (var image2 = new Bitmap(image.file_path)) // 画像取得
                    {
                        var image3 = new Bitmap(size, size);
                        var g = Graphics.FromImage(image3);
                        //g.DrawImage(image2, -clip.X, -clip.Y, clip.Width, clip.Height);
                        //g.DrawImage(image2, 0, 0, new Rectangle(clip.X, clip.Y, clip.Width, clip.Height), GraphicsUnit.Pixel);
                        //g.DrawImage(image2, 0, 0, new Rectangle(0, 0, size, size), GraphicsUnit.Pixel);
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        //g.DrawImage(image2, new Rectangle(0, 0, size, size), new Rectangle(0, 0, image3.Width, image3.Height), GraphicsUnit.Pixel);
                        g.DrawImage(image2, new Rectangle(0, 0, size, size), new Rectangle(0, 0, image2.Width, image2.Height), GraphicsUnit.Pixel);

                        var file_type = image.file_path.Substring(image.file_path.LastIndexOf('.') + 1);
                        var image_file_path = $@"{dir_path}/{i2}.{file_type}";
                        image3.Save(image_file_path);
                    }
                    i2++;
                }

                i++;
            }

            //if (Directory.Exists(path))
            //{
            //    return null;
            //}
            //return Directory.CreateDirectory(path);
        }

        public void ExportSSD(Blastagon.App.ImageLibrary.Tag tag)
        {
            var out_dir_root = @"export/ssd";
            var size = 64;

            SetupClearDirectory(out_dir_root);
            //var out_file_path = out_dir_root + @".xml";
            var out_annotation_dir_path = out_dir_root + @"/annotation";

            var i = 0;
            Directory.CreateDirectory(out_annotation_dir_path);

            // 順序が逆
            // Todo : 画像を一通り調べながらタグの一致を見て、必要なら出力のながれだな…
            foreach (var image in Blastagon.App.AppCore.core.image_library.image_datas)
            {
                // タグの検出
                //ImageLibrary.ImageTag image_tag = null;
                var tags = new List<ImageLibrary.ImageTag>();
                {
                    var is_ok = false;
                    foreach (var tag_chiled in tag.chiled)
                    {
                        foreach (var tag2 in image.tags)
                        {
                            if (tag2.tag == tag_chiled)
                            {
                                is_ok = true;
                                //image_tag = tag2;
                                tags.Add(tag2);
                                //break;
                            }
                        }
                        //if (is_ok) break;
                    }
                    if (!is_ok) continue;
                }
                var out_file_path = out_annotation_dir_path + $@"/{i:0000}.xml";

                //ファイルを上書きし、UTF-8で書き込む
                using (var sw = new System.IO.StreamWriter(
                    out_file_path,
                    false,
                    System.Text.Encoding.GetEncoding("UTF-8")))
                {
                    var index = image.file_path.LastIndexOf('\\');
                    var image_file_path = image.file_path.Substring(index+1);
                    var image_dir_path = image.file_path.Substring(0, index);
                    sw.WriteLine( $"<annotation>");
                    sw.WriteLine( $"    <folder>{image_dir_path}</folder>");
                    sw.WriteLine( $"    <filename>{image_file_path}</filename>");
                    sw.WriteLine( $"    <source>");
                    sw.WriteLine( $"        <database>Unknown</database>");
                    sw.WriteLine( $"    </source>");
                    sw.WriteLine( $"    <size>");
                    sw.WriteLine( $"        <width>{image.size.Width}</width>");
                    sw.WriteLine( $"        <height>{image.size.Height}</height>");
                    sw.WriteLine( $"    </size>");
                    sw.WriteLine( $"    <segmentd>0</segmentd>");
                    sw.WriteLine( $"    </source>");

                    foreach ( var tag2 in tags)
                    {

                        sw.WriteLine($"    {tag2.ex_word}");
                    }

                    sw.WriteLine($"</annotation>");
                }

                //using (var image2 = new Bitmap(image.file_path)) // 画像取得
                //    {
                //        var file_type = image.file_path.Substring(image.file_path.LastIndexOf('.') + 1);
                //        var image_file_path = $@"{dir_path}/{i2}.png";
                //        image2.Save(image_file_path, System.Drawing.Imaging.ImageFormat.Png);
                //    }
                //    i2++;
                //}

                i++;
            }

        }

        List<ImageLibrary.ImageTag> GetImageTags(Blastagon.App.ImageLibrary.Tag tag, Blastagon.App.ImageLibrary.ImageData imageData )
        {
            var tags = new List<ImageLibrary.ImageTag>();

            //ImageLibrary.ImageTag image_tag = null;
            foreach (var tag_chiled in tag.chiled)
            {
                foreach (var tag2 in imageData.tags)
                {
                    if (tag2.tag == tag_chiled)
                    {
                        tags.Add(tag2);
                    }
                }
            }

            return tags;

        }

        /// <summary>
        /// 配布目的ようにファイルを出力する
        /// </summary>
        /// <param name="tag"></param>
        public void ExportTagImages(Blastagon.App.ImageLibrary.Tag tag)
        {
            var out_dir_root = @"export/release";
            //var size = 64;

            SetupClearDirectory(out_dir_root);

            foreach (var image in Blastagon.App.AppCore.core.image_library.image_datas)
            {
                // タグの検出
                var tags = GetImageTags(tag, image);
                if (tags.Count() == 0) continue;

                var tagText = "";
                foreach (var tag2 in tags)
                {
                    if (tagText != "") tagText += " ";
                    tagText += tag2.tag.name;
                }

                var path = image.file_path;
                var tmp = path.Substring(path.LastIndexOf(@"\") + 1);
                File.Copy(image.file_path, out_dir_root + @"\" + tmp);

                //image.file_path

                //sw.WriteLine($"{i} {tagText}");
                //var OutFilePath = $@"{out_annotation_dir_path}/{i}_.png";
                //SaveScaleFixWidth(OutFilePath, image.file_path, 256);

                //i++;
            }

        }

        public void ExportCNNImageTagSet(Blastagon.App.ImageLibrary.Tag tag)
        {
            var out_dir_root = @"export/cnnTag";
            var size = 64;

            SetupClearDirectory(out_dir_root);
            //var out_file_path = out_dir_root + @".xml";
            var out_annotation_dir_path = out_dir_root + @"/annotation";
            var outTextFilePath = out_dir_root + @"/info.txt";

            var i = 0;
            Directory.CreateDirectory(out_annotation_dir_path);

            using (var sw = new System.IO.StreamWriter(
                    outTextFilePath,
                    false
                    //System.Text.Encoding.GetEncoding("UTF-8")
                )) 
            {
                foreach (var image in Blastagon.App.AppCore.core.image_library.image_datas)
                {
                    // タグの検出
                    var tags = GetImageTags(tag, image);
                    if (tags.Count() == 0) continue;

                    var tagText = "";
                    foreach (var tag2 in tags)
                    {
                        if (tagText != "") tagText += " ";
                        tagText += tag2.tag.name;
                    }

                    sw.WriteLine($"{i} {tagText}");
                    var OutFilePath = $@"{out_annotation_dir_path}/{i}_.png";
                    SaveScaleFixWidth(OutFilePath, image.file_path, 256);

                    i++;
                }
            }

        }

        private void SaveScaleFixWidth(string outFilePath, string srcFilePath, int size)
        {
            using (var image2 = new Bitmap(srcFilePath))
            {
                var toWidth = (float)size;
                var toHeight = (float)(size * image2.Height / image2.Width);

                using (var image3 = new Bitmap((int)toWidth, (int)toHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    var g = Graphics.FromImage(image3);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image2, new RectangleF(0, 0, toWidth, toHeight), new RectangleF(0, 0, image2.Width, image2.Height), GraphicsUnit.Pixel);
                    image3.Save(outFilePath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }

        }
        public void ExportGANImage(Blastagon.App.ImageLibrary.Tag tag)
        {
            var out_dir_root = @"export/GAN_Images";
            var size = 1024;

            SetupClearDirectory(out_dir_root);

            var i = 0;

            foreach (var image in Blastagon.App.AppCore.core.image_library.image_datas)
            {
                var is_ok = false;
                foreach( var t in image.tags)
                {
                    if(t.tag==tag)
                    {
                        is_ok = true;
                        break;
                    }
                }
                if (!is_ok) continue;
                var path = image.file_path;
                var tmp = path.Substring(path.LastIndexOf(@"\") + 1);
                var OutFilePath = $@"{out_dir_root}/{i}_.png";
                SaveScaleFixWidth(OutFilePath, image.file_path, size);

                i++;
            }

        }

        // 正方形にトリミングして出力
        private void SaveBitmapCutScaleSq(string outFilePath, string srcFilePath, int size )
        {
            using (var image2 = new Bitmap(srcFilePath))
            {
                //var file_type =;
                //var image_file_path = $@"{out_annotation_dir_path}/{i}.png";

                var targetSize = 256;
                var scale = 1.0;
                //var targetSrcWidth = 0;
                //var targetSrcHeight = 0;
                //var offsetX = 0.0;
                //var offsetY = 0.0;
                var toWidth = (float)0;
                var toHeight = (float)0;
                if (image2.Width > image2.Height)
                { // 横幅のほうが長い
                    scale = (double)targetSize / (double)image2.Height;

                    //var space = targetSize - scale * image2.Height;
                    //offsetY = space / 2;
                    //var space = targetSize - scale * image2.Width;
                    toHeight = (int)(targetSize / scale);
                    toWidth = toHeight;
                }
                else
                {
                    scale = (double)targetSize / (double)image2.Width;

                    toWidth = (int)(targetSize / scale);
                    toHeight = toWidth;
                    //offsetX = space / 2;
                }


                using (var image3 = new Bitmap(targetSize, targetSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    var g = Graphics.FromImage(image3);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    //g.Clear(Color.FromArgb(255, 0, 0, 0));
                    g.DrawImage(image2, new RectangleF(0, 0, targetSize, targetSize), new RectangleF(0, 0, toWidth, toHeight), GraphicsUnit.Pixel);
                    //image2.Save(image_file_path, System.Drawing.Imaging.ImageFormat.Png);
                    image3.Save(outFilePath, System.Drawing.Imaging.ImageFormat.Png);
                }
                //image2.Save($@"{out_annotation_dir_path}/{i}_.png", System.Drawing.Imaging.ImageFormat.Png);
            }

        }

    }
}
