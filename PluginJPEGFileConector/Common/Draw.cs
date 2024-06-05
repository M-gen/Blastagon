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
    static class Draw
    {
        public static bool CheckImageCompare(string file_path_1, string file_path_2)
        {
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
            }

            return pixels;
        }
    }
}
