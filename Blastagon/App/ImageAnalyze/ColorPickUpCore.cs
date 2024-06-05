using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Drawing.Drawing2D;

namespace Blastagon.App.ImageAnalyze
{
    // メディアンカットを利用した画像の色をピックアップするためのクラス

    public class ColorPickUpCore
    {
        public BinaryImage bin_image;
        public List<ColorPointEx> cpexs = new List<ColorPointEx>(); // 抽出色

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="scale_size_wh">解析する画像サイズ</param>
        /// <param name="median_cut_depth">メディアンカットで探索する階層の数</param>
        /// <param name="choice_color_num">抽出する色の数</param>
        public ColorPickUpCore( Bitmap bmp, int scale_size_wh, int median_cut_depth = 5, int choice_color_num = 16)
        {
            var scale_bmp = new Bitmap(scale_size_wh, scale_size_wh);
            using (var g = Graphics.FromImage(scale_bmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.DrawImage(bmp, 0, 0, scale_size_wh, scale_size_wh);
            }

            MedianCut(bmp, scale_size_wh, scale_size_wh, median_cut_depth, choice_color_num);
        }

        private void MedianCut(Bitmap bmp, int w, int h, int median_cut_depth, int choice_color_num)
        {
            // todo: bin には縮小後を使う、ここで宣言してるとややこしい
            bin_image = new BinaryImage(bmp);

            var bi = bin_image;
            var pixel_byte = 4;
            var box0 = new ColorPointBox();
            for (var x = 0; x < w; x++)
            {
                for (var y = 0; y < h; y++)
                {
                    var pos = (x + y * w) * pixel_byte;
                    var cp = new ColorPoint();
                    cp.r = bi.pixels[pos + 2];
                    cp.g = bi.pixels[pos + 1];
                    cp.b = bi.pixels[pos + 0];
                    cp.a = bi.pixels[pos + 3];
                    box0.colors.Add(cp);
                }
            }

            var boxs = new List<ColorPointBox>();
            boxs.Add(box0);

            var i = 0;
            var i_num = median_cut_depth;
            while (i < i_num)
            {
                var boxs_next = new List<ColorPointBox>(); // 次の箱
                foreach (var box in boxs)
                {
                    box.Update();
                    // 各辺の長さ(r,g,b)
                    var long_r = box.r_max - box.r_min;
                    var long_g = box.g_max - box.g_min;
                    var long_b = box.b_max - box.b_min;

                    ColorPointBox box1, box2;

                    if (long_r < long_g && long_r < long_b)
                    {
                        // r
                        box.SplitR(long_r / 2 + box.r_min, out box1, out box2);
                    }
                    else if (long_g <= long_r && long_g < long_b)
                    {
                        // g
                        box.SplitR(long_g / 2 + box.g_min, out box1, out box2);
                    }
                    else
                    {
                        // b
                        box.SplitR(long_b / 2 + box.b_min, out box1, out box2);
                    }

                    if (box1.colors.Count() > 0) boxs_next.Add(box1);
                    if (box2.colors.Count() > 0) boxs_next.Add(box2);


                }

                boxs = boxs_next;
                i++;
            }

            foreach (var box in boxs) box.Update();

            var cpexs = new List<ColorPointEx>();
            {
                // 候補となる色を箱の頂点から算出
                var cpexs_tmp = new List<ColorPointEx>();
                foreach (var box in boxs)
                {
                    box.AddColorPointEx(ref cpexs_tmp);
                }

                // 重複を削除
                foreach (var cpex in cpexs_tmp)
                {
                    var is_ok = true;
                    foreach (var cpex2 in cpexs)
                    {
                        if ((cpex.a == cpex2.a) && (cpex.r == cpex2.r) && (cpex.g == cpex2.g) && (cpex.b == cpex2.b))
                        {
                            is_ok = false; // 一致
                            break;
                        }
                    }
                    if (is_ok)
                    {
                        cpexs.Add(cpex);
                    }
                }
            }

            // 近い色をあつめておく
            foreach (var cp in box0.colors)
            {
                var range = cp.GetRangeRGB(cpexs[0]);
                var near_cpex = cpexs[0];
                for (var j = 1; j < cpexs.Count(); j++)
                {
                    var cpex = cpexs[j];
                    var tmp_range = cp.GetRangeRGB(cpex);
                    if (tmp_range < range)
                    {
                        range = tmp_range;
                        near_cpex = cpex;
                    }
                }
                near_cpex.near_colors.Add(cp);
            }

            var fix_cpexs = new List<ColorPointEx>();

            var fix_color_num = 8 * 2;
            for (var fix_i = 0; fix_i < fix_color_num; fix_i++)
            {
                // 確定色とするか、評価していく
                {
                    // 既に確定した色との最小距離を算出
                    foreach (var cpex in cpexs)
                    {
                        cpex.near_fix_colors = -1;

                        if (fix_cpexs.Count() == 0) cpex.near_fix_colors = 0;
                        foreach (var cpex2 in fix_cpexs)
                        {
                            var tmp_range = cpex.GetRangeRGB(cpex2);
                            if ((cpex.near_fix_colors == -1) || (tmp_range < cpex.near_fix_colors))
                            {
                                cpex.near_fix_colors = tmp_range;
                            }
                        }
                    }

                    var near_colors_count_max = 0.0;
                    var near_fix_colors_max = 0.0;
                    foreach (var cpex in cpexs)
                    {
                        var tmp = cpex.near_colors.Count();
                        if (near_colors_count_max < tmp) near_colors_count_max = tmp;

                        var tmp2 = cpex.near_fix_colors;
                        if (near_fix_colors_max < tmp2) near_fix_colors_max = tmp2;
                    }
                    if (near_fix_colors_max == 0.0) near_fix_colors_max = 1; // 初回は0なので

                    // 
                    var exp = 0.0; // 評価値
                    var near_fix_bonus = 1.0; //Todo 画像サイズを繁栄しないとうまく機能しないはず
                    var fix_cpex = (ColorPointEx)null;
                    foreach (var cpex in cpexs)
                    {
                        // maxでわって正規化して評価する
                        //var tmp_exp = (cpex.near_colors.Count() / near_colors_count_max ) + (cpex.near_fix_colors / near_fix_colors_max) * near_fix_bonus;
                        var tmp_exp = (cpex.near_colors.Count() / near_colors_count_max) * (cpex.near_fix_colors / near_fix_colors_max) * near_fix_bonus;
                        if (cpex.near_fix_colors == 0) tmp_exp = cpex.near_colors.Count() / near_colors_count_max;
                        if ((fix_cpex == null) || (tmp_exp > exp))
                        {
                            exp = tmp_exp;
                            fix_cpex = cpex;
                        }
                    }

                    if (fix_cpex != null)
                    {
                        fix_cpex.is_fix = true;
                        fix_cpexs.Add(fix_cpex);
                        //Console.WriteLine($"{(fix_cpex.near_colors.Count() / near_colors_count_max)} {(fix_cpex.near_fix_colors / near_fix_colors_max) * near_fix_bonus}");
                        cpexs.Remove(fix_cpex);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            this.cpexs = fix_cpexs;


        }

        public class ColorPoint
        {
            public int r;
            public int g;
            public int b;
            public int a;

            // RGBの3要素で距離を測る
            public double GetRangeRGB(ColorPoint cp2)
            {
                // 平方根を算出する意味が無いかもしれない…距離の比較ができればいいので
                var res = Math.Sqrt((r - cp2.r) * (r - cp2.r) + (g - cp2.g) * (g - cp2.g) + (b - cp2.b) * (b - cp2.b));
                return res;
            }
        }

        // 色空間と、そこにふくまれる色
        public class ColorPointBox
        {
            public int r_max;
            public int r_min;
            public int g_max;
            public int g_min;
            public int b_max;
            public int b_min;

            public List<ColorPoint> colors = new List<ColorPoint>();

            public void Update()
            {
                if (colors.Count() > 0)
                {
                    var tmp_cp = colors[0];
                    r_max = tmp_cp.r;
                    r_min = tmp_cp.r;
                    g_max = tmp_cp.g;
                    g_min = tmp_cp.g;
                    b_max = tmp_cp.b;
                    b_min = tmp_cp.b;
                }

                foreach (var cp in colors)
                {
                    if (cp.r > r_max) r_max = cp.r;
                    if (cp.r < r_min) r_min = cp.r;
                    if (cp.g > g_max) g_max = cp.g;
                    if (cp.g < g_min) g_min = cp.g;
                    if (cp.b > b_max) b_max = cp.b;
                    if (cp.b < b_min) b_min = cp.b;
                }

            }

            public void SplitR(int split_value, out ColorPointBox box1, out ColorPointBox box2)
            {
                box1 = new ColorPointBox();
                box2 = new ColorPointBox();


                foreach (var cp in colors)
                {
                    if (cp.r < split_value)
                    {
                        box1.colors.Add(cp);
                    }
                    else
                    {
                        box2.colors.Add(cp);
                    }
                }
            }

            public void SplitG(int split_value, out ColorPointBox box1, out ColorPointBox box2)
            {
                box1 = new ColorPointBox();
                box2 = new ColorPointBox();


                foreach (var cp in colors)
                {
                    if (cp.g < split_value)
                    {
                        box1.colors.Add(cp);
                    }
                    else
                    {
                        box2.colors.Add(cp);
                    }
                }
            }

            public void SplitB(int split_value, out ColorPointBox box1, out ColorPointBox box2)
            {
                box1 = new ColorPointBox();
                box2 = new ColorPointBox();


                foreach (var cp in colors)
                {
                    if (cp.b < split_value)
                    {
                        box1.colors.Add(cp);
                    }
                    else
                    {
                        box2.colors.Add(cp);
                    }
                }
            }

            // 頂点と、中心の色をリストに追加する
            public void AddColorPointEx(ref List<ColorPointEx> list)
            {
                var cpexs = new ColorPointEx[9];
                for (var i = 0; i < 9; i++) cpexs[i] = new ColorPointEx();

                // 頂点
                cpexs[0].r = this.r_max; cpexs[0].g = this.g_max; cpexs[0].b = this.b_max; cpexs[0].a = 255;
                cpexs[1].r = this.r_max; cpexs[1].g = this.g_max; cpexs[1].b = this.b_min; cpexs[1].a = 255;
                cpexs[2].r = this.r_max; cpexs[2].g = this.g_min; cpexs[2].b = this.b_max; cpexs[2].a = 255;
                cpexs[3].r = this.r_max; cpexs[3].g = this.g_min; cpexs[3].b = this.b_min; cpexs[3].a = 255;
                cpexs[4].r = this.r_min; cpexs[4].g = this.g_max; cpexs[4].b = this.b_max; cpexs[4].a = 255;
                cpexs[5].r = this.r_min; cpexs[5].g = this.g_max; cpexs[5].b = this.b_min; cpexs[5].a = 255;
                cpexs[6].r = this.r_min; cpexs[6].g = this.g_min; cpexs[6].b = this.b_max; cpexs[6].a = 255;
                cpexs[7].r = this.r_min; cpexs[7].g = this.g_min; cpexs[7].b = this.b_min; cpexs[7].a = 255;

                // 中心
                cpexs[8].r = (this.r_min + this.r_max) / 2;
                cpexs[8].g = (this.g_min + this.g_max) / 2;
                cpexs[8].b = (this.b_min + this.b_max) / 2;

                foreach (var cpex in cpexs)
                {
                    list.Add(cpex);
                }
            }
        }


        public class ColorPointEx : ColorPoint
        {
            public bool is_fix;                                             // 確定した色かどうか（減色で採用する色と確定したかどうか）
            public List<ColorPoint> near_colors = new List<ColorPoint>();   // 近似している色
            public double near_fix_colors = 0;                              // 他の「確定した色」への近似率・評価（高いと、他の色（離れた色）を確定しやすいようにする
        }


    }
}
