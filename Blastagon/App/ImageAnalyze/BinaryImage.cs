using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;

namespace Blastagon.App.ImageAnalyze
{
    // Imageから画像データ(32bitRGBA)の取扱を簡易化する
    public class ImageToBinary_32bitRGBA : IDisposable
    {
        Bitmap bitmap;
        System.Drawing.Imaging.BitmapData bitmap_data;
        IntPtr ptr;
        bool is_write;
        bool is_get_bitmap = false; // Bitmapを外にわたしている場合、Disposeしない

        public byte[] pixels;
        public int width;
        public int height;
        public int stride;
        

        public ImageToBinary_32bitRGBA(Image image, bool is_write)
        {
            this.is_write = is_write;

            bitmap = new Bitmap(image);
            width = bitmap.Width;
            height = bitmap.Height;

            var rect = new Rectangle(0, 0, width, height);

            if (this.is_write)
            {
                bitmap_data = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }
            else
            {
                bitmap_data = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }
            stride = bitmap_data.Stride;

            ptr = bitmap_data.Scan0;
            pixels = new byte[bitmap_data.Stride * height];
            System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, pixels.Length);
        }

        public void Dispose()
        {
            if (this.is_write)
            {
                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, ptr, pixels.Length);
            }
            bitmap.UnlockBits(bitmap_data);

            if (!is_get_bitmap)
            {
                bitmap.Dispose();
            }

        }

        public Bitmap GetBitmap()
        {
            is_get_bitmap = true;
            return bitmap;
        }

    }

    // 32bit(各種8bit)RGBA画像データを扱うためのクラス
    public class BinaryImage
    {
        public byte[] pixels; // 中核となるデータ
        readonly int width;
        readonly int height;

        public BinaryImage(Image image)
        {
            using ( var i2b = new ImageToBinary_32bitRGBA( image, false))
            {
                pixels = i2b.pixels;
                width  = i2b.width;
                height = i2b.height;
            }
        }
    }
}
