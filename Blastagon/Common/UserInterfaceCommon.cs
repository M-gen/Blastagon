using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Blastagon.Common
{
    public class UserInterfaceCommon
    {
        public static void DrawStringFrame( Graphics g, string font_name, int size, string text, int x, int y, Color in_color, Color flame_color, double frame_size )
        {
            //var gm = g.SmoothingMode;
            //g.SmoothingMode = SmoothingMode.None;
            //g.SmoothingMode = gm;
            g.SmoothingMode = SmoothingMode.HighQuality;

            //GraphicsPathオブジェクトの作成
            var gp = new System.Drawing.Drawing2D.GraphicsPath();

            FontFamily ff = new FontFamily(font_name);
            gp.AddString(text, ff, 0, size,  new Point(x, y), StringFormat.GenericDefault);

            g.DrawPath(new Pen(flame_color, (float)frame_size), gp);
            g.FillPath(new SolidBrush(in_color), gp);

            //リソースを解放する
            ff.Dispose();
        }

        public static Bitmap BitmapFromFile( string file_path, int w, int h, double alpha, bool is_color_change, Color color )
        {
            var image = Image.FromFile(file_path);
            var cm = new System.Drawing.Imaging.ColorMatrix();
            cm.Matrix00 = 1;
            cm.Matrix11 = 1;
            cm.Matrix22 = 1;
            cm.Matrix33 = (float)alpha;
            cm.Matrix44 = 1;

            //ImageAttributesオブジェクトの作成
            var ia = new System.Drawing.Imaging.ImageAttributes();
            //ColorMatrixを設定する
            ia.SetColorMatrix(cm);

            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ia);
            }
            image.Dispose();

            // アルファのみそのままで、色(RGB)をかえる
            if (is_color_change)
            {
                var bitmap_data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var stride = bitmap_data.Stride;
                var ptr = bitmap_data.Scan0;
                var pixels = new byte[bitmap_data.Stride * bmp.Height];
                System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, pixels.Length);
                
                for ( var y = 0; y < bmp.Height; y++ )
                {
                    for (var x = 0; x < bmp.Width; x++)
                    {
                        var pos = x * 4 + y * stride;
                        pixels[pos + 0] = color.B; 
                        pixels[pos + 1] = color.G;
                        pixels[pos + 2] = color.R;
                    }
                }

                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, ptr, pixels.Length);
                bmp.UnlockBits(bitmap_data);
            }

            return bmp;
        }

        // テスト用ボタンなどを簡単に作るため
        public static Control CreateButton(System.Windows.Forms.Control form, int x, int y, int w, int h, string text )
        {
            var btn = new Control();
            form.Controls.Add(btn);
            btn.Location = new Point(x, y);
            btn.Size     = new Size(w, h);
            btn.Text = text;

            return btn;
        }

        // ボタンなどを簡単に作るため
        public static Control CreateButton(Control btn, System.Windows.Forms.Control form, int x, int y, int w, int h, string text)
        {
            form.Controls.Add(btn);
            btn.Location = new Point(x, y);
            btn.Size = new Size(w, h);
            btn.Text = text;

            return btn;
        }

        public static void DrawButtonOff(Graphics g, int x, int y, int w, int h, bool is_line, Action<bool> func = null)
        {
            var color_2 = new SolidBrush(Color.FromArgb(255, 80, 80, 80));
            var color_3 = new Pen(Color.FromArgb(255, 32, 32, 32));
            var color_4 = new Pen(Color.FromArgb(255, 110, 110, 110));
            var color_5 = new Pen(Color.FromArgb(255, 0, 0, 0));

            g.FillRectangle(color_2, new Rectangle(x + 0, y + 0, w, h));
            ///g.FillRectangle(color_2, new Rectangle(x - 1, y - 1, w-2, h-2));
            g.DrawRectangle(color_5, new Rectangle(x + 0, y + 0, w - 1, h - 1));
            g.DrawRectangle(color_3, new Rectangle(x + 1, y + 1, w - 3, h - 3));

            if (is_line)
            {
                g.DrawRectangle(color_4, new Rectangle(x + 2, y + 2, w - 5, h - 5));
                g.DrawRectangle(color_4, new Rectangle(x + 3, y + 3, w - 7, h - 7));
                g.DrawRectangle(color_4, new Rectangle(x + 4, y + 4, w - 9, h - 9));
            }

            if (func != null) func(true);

            g.FillRectangle(new SolidBrush(Color.FromArgb(112, 0, 0, 0)), new Rectangle(x, y + h / 2, w - 1, h / 2));

            DrawLigntV(g, x + 1, y + 1, w - 2, (int)(h * 0.40), 64, 0.1);
            DrawLigntV(g, x + 1, y + h - 2, w - 2, (int)(-h * 0.23), 48, 0.3);
        }

        public static void DrawButtonOffEx(Graphics g, int x, int y, int w, int h, bool is_line, double grad_alpha, Action<bool> func = null)
        {
            var color_2 = new SolidBrush(Color.FromArgb(255, 80, 80, 80));
            var color_3 = new Pen(Color.FromArgb(255, 32, 32, 32));
            var color_4 = new Pen(Color.FromArgb(255, 110, 110, 110));
            var color_5 = new Pen(Color.FromArgb(255, 0, 0, 0));

            g.FillRectangle(color_2, new Rectangle(x + 0, y + 0, w, h));
            ///g.FillRectangle(color_2, new Rectangle(x - 1, y - 1, w-2, h-2));
            g.DrawRectangle(color_5, new Rectangle(x + 0, y + 0, w - 1, h - 1));
            g.DrawRectangle(color_3, new Rectangle(x + 1, y + 1, w - 3, h - 3));

            if (is_line)
            {
                g.DrawRectangle(color_4, new Rectangle(x + 2, y + 2, w - 5, h - 5));
                g.DrawRectangle(color_4, new Rectangle(x + 3, y + 3, w - 7, h - 7));
                g.DrawRectangle(color_4, new Rectangle(x + 4, y + 4, w - 9, h - 9));
            }

            if (func != null) func(true);

            g.FillRectangle(new SolidBrush(Color.FromArgb((int)(112* grad_alpha), 0, 0, 0)), new Rectangle(x, y + h / 2, w - 1, h / 2));

            DrawLigntV(g, x + 1, y + 1, w - 2, (int)(h * 0.40), 64, 0.1);
            DrawLigntV(g, x + 1, y + h - 2, w - 2, (int)(-h * 0.23), 48, 0.3);
        }

        public static Point[] GetDeltaPoint(int x, int y, int h, int w, bool turn)
        {
            var ox = x + w / 2;
            var ow = w / 2;
            var oy = y;
            var oh = h;
            if (!turn)
            {
                Point[] ps = {new Point(ox, oy),
                    new Point(ox-ow, oy+oh),
                    new Point(ox+ow, oy+oh),
                    new Point(ox+1, oy)};
                return ps;
            }
            else
            {
                Point[] ps = {new Point(ox, oy+oh),
                    new Point(ox-ow, oy),
                    new Point(ox+ow, oy),
                    new Point(ox+1, oy+oh)};
                return ps;
            }
        }

        public static void DrawLigntV(Graphics g, int x, int y, int w, int light_long, int light_force, double light_slim)
        {
            var dir = 1;
            if (light_long < 0)
            {
                dir = -1;
                light_long = -light_long;
            }
            for (var i = 0; i < light_long; i++)
            {
                var oy = y + i * dir;
                var ca = light_force - (i * light_force / light_long);
                g.DrawLine(new Pen(Color.FromArgb(ca, 255, 255, 255)), (int)(x + light_slim * i), oy, x + w - (int)(light_slim * i), oy);
            }

        }

        public static void DrawDelta(Graphics g, int x, int y, int w, int h, bool turn)
        {

            var ps = GetDeltaPoint(x, y, w, h, turn);
            var ps2 = GetDeltaPoint(x - 1, y - 1, w + 2, h + 2, turn);
            var ps3 = GetDeltaPoint(x - 2, y - 2, w + 4, h + 4, turn);
            var ps4 = GetDeltaPoint(x - 3, y - 3, w + 6, h + 6, turn);

            var cv = 140;
            g.FillPolygon(new SolidBrush(Color.FromArgb(255, cv, cv, cv)), ps); // 多角形を描画する
            g.DrawPolygon(new Pen(Color.FromArgb(32, 255, 255, 255)), ps);           //
            g.DrawPolygon(new Pen(Color.FromArgb(108, 0, 0, 0)), ps2);           //
            g.DrawPolygon(new Pen(Color.FromArgb(64, 0, 0, 0)), ps3);           //
            //g.DrawPolygon(new Pen(Color.FromArgb( 32,   0,   0,   0)), ps4);           //

        }

        public static void DrawPattarnRect(Graphics g, int x, int y, int w, int h, int dot_size)
        {
            var gm = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;

            var x_num = w / dot_size;
            var y_num = h / dot_size;

            var color_1 = new SolidBrush(Color.FromArgb(255, 80, 80, 80));
            var color_2 = new SolidBrush(Color.FromArgb(255, 100, 100, 100));
            for (var x2 = 0; x2 < x_num; x2++)
            {
                for (var y2 = 0; y2 < y_num; y2++)
                {
                    var x3 = x2 * dot_size + x;
                    var y3 = y2 * dot_size + y;
                    var color = color_1;
                    var cc = 0;
                    if (y2 % 2 == 1) cc++;
                    if ((x2 + cc) % 2 == 1)
                    {
                        color = color_2;
                    }
                    g.FillRectangle(color, new Rectangle(x3, y3, dot_size, dot_size));
                }
            }
            g.SmoothingMode = gm;
        }

        // 透過、色指定など
        public static void DrawPattarnRect(Graphics g, int x, int y, int w, int h, int dot_size, int color_a)
        {
            var gm = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;


            var x_num = w / dot_size + 1;
            var y_num = h / dot_size + 1;

            var color_1 = new SolidBrush(Color.FromArgb(color_a, 80, 80, 80));
            var color_2 = new SolidBrush(Color.FromArgb(color_a, 100, 100, 100));
            for (var x2 = 0; x2 < x_num; x2++)
            {
                for (var y2 = 0; y2 < y_num; y2++)
                {
                    var x3 = x2 * dot_size + x;
                    var y3 = y2 * dot_size + y;
                    var color = color_1;
                    var cc = 0;
                    if (y2 % 2 == 1) cc++;
                    if ((x2 + cc) % 2 == 1)
                    {
                        color = color_2;
                    }
                    g.FillRectangle(color, new Rectangle(x3, y3, dot_size, dot_size));
                }
            }
            g.SmoothingMode = gm;
            
        }
    }
}
