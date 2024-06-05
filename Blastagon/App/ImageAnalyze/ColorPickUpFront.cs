using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using Blastagon.PluginFileConector;

namespace Blastagon.App.ImageAnalyze
{
    // ColorPickUpCoreにより解析した結果に基づいて、具体的に色分けをするクラス
    public class ColorPickUpFront
    {
        ColorPickUpCore color_pick_up_core;
        public List<string> tag_names = new List<string>();

        public ColorPickUpFront(ImageLibrary.ImageData image_data, int color_choise_num)
        {

            var plugin = FileConectorManager.GetFileConector(image_data.file_path, false);
            if (plugin == null)
            {
                // todo: popup_log.AddMessage("拡張子とファイル内容が一致しないため、書き込みできません : WriteTagDataInImageFiles " + image_data.file_path);
                return;
            }
            using (var image = plugin.image_conector.FromFile(null, image_data.file_path, ""))
            {
                color_pick_up_core = new ColorPickUpCore(image, 100);
            }

            // 解析した指定個の色を取得する
            for (var i = 0; i < color_choise_num; i++)
            {
                //var color = Color.FromArgb(color_pick_up_core.cpexs[0].r, color_pick_up_core.cpexs[0].g, color_pick_up_core.cpexs[0].b);
                if (color_pick_up_core.cpexs.Count() <= i) break;
                var cpex = color_pick_up_core.cpexs[i];
                var color = Color.FromArgb(cpex.r, cpex.g, cpex.b);
                var color_hsv = HslColor.FromRgb(color);

                if (color_hsv.S < 0.1)
                {
                    // 色相が極めて低い
                    if (color_hsv.L < 0.1)
                    {
                        tag_names.Add("黒");
                    }
                    else if (color_hsv.L > 0.9)
                    {
                        tag_names.Add("白");
                    }
                    else
                    {
                        tag_names.Add("灰");
                    }
                }
                else
                {
                    // 色相で判定
                    var t = "赤"; // 赤は色相を一周するので、暫定で入れておく

                    var ranges = new List<ColorHRange>();
                    ranges.Add(new ColorHRange(25, 50, "橙"));
                    ranges.Add(new ColorHRange(50, 65, "黄"));
                    ranges.Add(new ColorHRange(65, 160, "緑"));
                    ranges.Add(new ColorHRange(160, 265, "青"));
                    ranges.Add(new ColorHRange(265, 340, "紫"));
                    foreach (var range in ranges)
                    {
                        if (range.min <= color_hsv.H && color_hsv.H <= range.max)
                        {
                            t = range.name;
                            break;
                        }
                    }
                    tag_names.Add(t);
                }
            }


        }

        // 色相の範囲
        public class ColorHRange
        {
            public double min;
            public double max;
            public string name;
            
            // 彩度の範囲を0～360で指定する
            public ColorHRange(int min, int max, string name)
            {
                this.min = (double)min;
                this.max = (double)max;
                this.name = name;
            }
        }

    }
}
