using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Blastagon.Common
{
    public class PointD
    {
        public double X;
        public double Y;
        public PointD(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }

    public class RectangleD
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;
        public RectangleD(double X, double Y, double Width, double Height)
        {
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
        }
        public RectangleD(Rectangle rect)
        {
            this.X = rect.X;
            this.Y = rect.Y;
            this.Width = rect.Width;
            this.Height = rect.Height;
        }
        public RectangleD(RectangleD rect)
        {
            this.X = rect.X;
            this.Y = rect.Y;
            this.Width = rect.Width;
            this.Height = rect.Height;
        }

        public Rectangle ToRectangle()
        {
            return new Rectangle((int)X,(int)Y,(int)Width,(int)Height);
        }

        public RectangleD Scale( PointD ancer_point, double scale )
        {
            var rect = new RectangleD(0,0,0,0);
            rect.Width  = Width * scale;
            rect.Height = Height * scale;

            var x1 = ancer_point.X - X;
            var y1 = ancer_point.Y - Y;
            var x2 = x1 * scale;
            var y2 = y1 * scale;
            rect.X = ancer_point.X - x2;
            rect.Y = ancer_point.Y - y2;

            return rect;
        }
    }

    public static class Draw
    {
        public static bool CheckImageCompare(string file_path_1, string file_path_2)
        {
            GC.Collect();
            var pixel_1 = GetLockPixel(file_path_1);
            var pixel_2 = GetLockPixel(file_path_2);
            if (pixel_1.Count() == pixel_2.Count())
            {
                var num = pixel_1.Count();
                for (var i = 0; i < num; i++)
                {
                    if (pixel_1[i] != pixel_2[i]) return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private static byte[] GetLockPixel(string file_path)
        {
            byte[] pixels;
            using (var image = Image.FromFile(file_path))
            {
                //var bmp = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
                var bmp = (Bitmap)image;
                var rect = new Rectangle(0, 0, image.Width, image.Height);

                var bitmap_data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var stride = bitmap_data.Stride;
                var ptr = bitmap_data.Scan0;
                pixels = new byte[bitmap_data.Stride * bmp.Height];
                System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, pixels.Length);

                bmp.UnlockBits(bitmap_data);
                bmp.Dispose();
            }

            return pixels;
        }
    }
}
