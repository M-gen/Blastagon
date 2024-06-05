using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Drawing.Drawing2D;
using Blastagon.PluginFileConector;

namespace Blastagon.App.ImageAnalyze
{
    // 画像の彩度に対して段階的な評価(タグ)をつける
    public class SaturationStep
    {
        public List<string> tag_names = new List<string>();

        public SaturationStep(ImageLibrary.ImageData image_data)
        {
            var plugin = FileConectorManager.GetFileConector(image_data.file_path, false);
            if (plugin == null)
            {
                // todo: popup_log.AddMessage("拡張子とファイル内容が一致しないため、書き込みできません : WriteTagDataInImageFiles " + image_data.file_path);
                return;
            }

            var hsls = new List<HslColor>();
            using (var image = plugin.image_conector.FromFile(null, image_data.file_path, ""))
            {
                // 画像を縮小して、計算速度を上げる
                var scale_size_wh = 100;
                var scale_bmp = new Bitmap(scale_size_wh, scale_size_wh);
                using (var g = Graphics.FromImage(scale_bmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    g.DrawImage(image, 0, 0, scale_size_wh, scale_size_wh);
                }
                var binary_image = new BinaryImage(scale_bmp);

                var size = binary_image.pixels.Count() / 4;
                for(var i=0;i<size;i++)
                {
                    var hsl = HslColor.FromRgb(binary_image.pixels[i*4+2], binary_image.pixels[i*4+1], binary_image.pixels[i*4]);
                    hsls.Add(hsl);
                }
            }

            // 全体的に彩度が低く、モノクロとしてあつかえるかどうか
            {
                var is_monokuro = true;
                foreach (var hsl in hsls)
                {
                    if (GetS(hsl) > 0.1) is_monokuro = false;
                }
                if (is_monokuro)
                {
                    tag_names.Add("0.1");
                    return;
                }
            }

            {
                var is_ok = true;
                foreach (var hsl in hsls)
                {
                    if (GetS(hsl) > 0.2) is_ok = false;
                }
                if (is_ok)
                {
                    tag_names.Add("0.2");
                    return;
                }
            }

            {
                var is_ok = true;
                foreach (var hsl in hsls)
                {
                    if (GetS(hsl) > 0.3) is_ok = false;
                }
                if (is_ok)
                {
                    tag_names.Add("0.3");
                    return;
                }
            }

            {
                var is_ok = true;
                foreach (var hsl in hsls)
                {
                    if (GetS(hsl) > 0.4) is_ok = false;
                }
                if (is_ok)
                {
                    tag_names.Add("0.4");
                    return;
                }
            }

            {
                var is_ok = true;
                foreach (var hsl in hsls)
                {
                    if (GetS(hsl) > 0.5) is_ok = false;
                }
                if (is_ok)
                {
                    tag_names.Add("0.5");
                    return;
                }
            }

            {
                var is_ok = true;
                foreach (var hsl in hsls)
                {
                    if (GetS(hsl) > 0.6) is_ok = false;
                }
                if (is_ok)
                {
                    tag_names.Add("0.6");
                    return;
                }
            }

            {
                var is_ok = true;
                foreach (var hsl in hsls)
                {
                    if (GetS(hsl) > 0.6) is_ok = false;
                }
                if (is_ok)
                {
                    tag_names.Add("0.6");
                    return;
                }
            }

            {
                var is_ok = true;
                foreach (var hsl in hsls)
                {
                    if (GetS(hsl) > 0.7) is_ok = false;
                }
                if (is_ok)
                {
                    tag_names.Add("0.7");
                    return;
                }
            }

            {
                var is_ok = true;
                foreach (var hsl in hsls)
                {
                    if (GetS(hsl) > 0.8) is_ok = false;
                }
                if (is_ok)
                {
                    tag_names.Add("0.8");
                    return;
                }
            }

            {
                var is_ok = true;
                foreach (var hsl in hsls)
                {
                    if (GetS(hsl) > 0.9) is_ok = false;
                }
                if (is_ok)
                {
                    tag_names.Add("0.9");
                    return;
                }
            }

            tag_names.Add("1.0");
            return;

            //// 
            //var is_threshold = 0.5; // 境界、閾値
            //var h_up_count   = 0;
            //var h_down_count = 0;
            //var h_count      = 0;
            //foreach (var hsl in hsls)
            //{
            //    h_count++;
            //    if (GetS(hsl) > is_threshold) h_up_count++;
            //    else h_down_count++;
            //}

            //if ( h_up_count==0 )
            //{
            //    // 閾値をこえたものがない = 全体的に彩度が低い
            //    tag_names.Add("彩度が低い");
            //    return;
            //}
            //else if (((double)h_up_count/(double)h_count) <= 0.5)
            //{
            //    tag_names.Add("普通");
            //    return;
            //}
            //else
            //{
            //    tag_names.Add("彩度が高い");
            //    return;
            //}

        }

        // 明度分で彩度を補正した値を取得…HSLでやってるからややこしいのかもしれない
        public double GetS( HslColor hsl )
        {
            double v = hsl.S;
            //v = v * ( 0.5 - Math.Abs(hsl.L - 0.5) ) * 2.0;
            //v = v * (Math.Abs(hsl.L - 0.5) ) * 2.0;
            //v = (Math.Abs(hsl.L - 0.5) * 2.0) * v;
            v = (1.0 - Math.Abs(hsl.L - 0.5) * 2.0) * v;
            //Console.WriteLine($"{v} {hsl.S} {hsl.L}");
            return v;
        }

    }
}
