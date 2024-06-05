using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;

using Blastagon.Common;
using Blastagon.App;

namespace Blastagon.ResourceManage
{

    // 画像リソースを管理する
    // メモリ管理が煩雑になるので、一元管理する
    // 画像が必要なタイミングが、最遅は「描画のタイミング」なので、このあたりを踏まえて、
    // 代用で描画してもいいのか、今すぐ必要なのか…
    //
    public class ImageManager : IDisposable
    {
        static ImageManager image_manager;

        // 拡縮後のbmpを保持したいので、ファイルパスと1対1の関係にならない
        public class RImage : IDisposable
        {
            public const int DRAW_COUNTER_MAX = 10000;

            public enum ReserveSizeType
            {
                Fix,      // 決まっている
                FitWidth, // 横幅に合わせる（縦は決まっていない）
                Scale,    // スケールに合わせる
            }

            public ImageByFile owner;

            public double scale = 1;
            public Size size;
            public string ex_word = "";
            //public Rectangle clip;
            public volatile Bitmap bmp;
            public volatile bool is_reserve = false; // 予約されているかどうか
            public volatile ReserveSizeType reserve_size_type = ReserveSizeType.Fix;
            public volatile int draw_counter         = 0; // 早く読み込んだほうが良いかを検出するためのカウンター
            public volatile int draw_counter_release = 0; // bmpを開放してもよいか、放置ぐあいを検出するためのカウンター
            public volatile Action<RImage> CreatedImage;
            public volatile Lock lock_object = new Lock();

            public void Dispose()
            {
                try
                {
                    lock_object.Enter();
                    bmp.Dispose();
                    bmp = null;
                }
                finally
                {
                    lock_object.Exit();
                }

                owner.rimages.Remove(this);
            }

            // 予約完了していれば描画する
            public void Draw( Graphics g, int x, int y )
            {
                try
                {
                    lock_object.Enter();
                    if (bmp != null)
                    {
                        g.DrawImage(bmp, x, y);
                    }
                    else
                    {
                        if (size.Width > 0 && size.Height > 0)
                        {
                            foreach (var brother in owner.rimages)
                            {
                                if (brother.bmp != null)
                                {
                                    g.DrawImage(brother.bmp, x, y, size.Width, size.Height);
                                    brother.draw_counter_release = DRAW_COUNTER_MAX;
                                }
                            }
                        }

                        ImageManager._DrawReserveImage();
                    }
                    draw_counter++; if (DRAW_COUNTER_MAX < draw_counter) draw_counter = DRAW_COUNTER_MAX;
                    draw_counter_release = DRAW_COUNTER_MAX;
                }
                catch
                {
                    throw;
                }
                finally
                {
                    lock_object.Exit();
                }
            }
            
        }

        public class ImageByFile
        {
            public string file_path = "";
            public Size   size;
            //public Image image;
            public int image_release_count = 0;

            public List<RImage> rimages = new List<RImage>();
            //public volatile object rimage_bmp_lock;
            //public volatile Blastagon.Common.Lock rimage_bmp_lock;

            public bool is_size_fix = false; // サイズ確定済み

        }

        Dictionary<string, ImageByFile> images = new Dictionary<string, ImageByFile>();
        List<Bitmap> bitmaps = new List<Bitmap>();

        // 予約
        List<ImageByFile> reserve_images  = new List<ImageByFile>();
        List<RImage>      reserve_rimages = new List<RImage>(); // todo : 古いRImageが溜まりっぱなしの場合がありそうなので、考慮が必要かも…ない？

        // スレッド制御、予約や解放
        Thread        loop_thread;
        volatile bool loop_thread_end = false;
        volatile object loop_thread_lock = new object();
        //volatile object rimage_bmp_lock = new object();
        public volatile Lock rimage_bmp_lock = new Lock();

        // 解放の判断材料
        // 保持しているbitmapのピクセル数（横×高）の合計値が超えていると、解放する
        const int STOCK_PIXEL = 1024 * 1024 * 100;

        // 新規に読み込みが不要な放置状態の場合、予約読み込みを停止させる
        const int RESERVE_STOP_TIME = 1000;
        volatile object reserve_stop_lock = new object();
        volatile bool is_reserve_stop = false;
        System.Diagnostics.Stopwatch reserve_stop_watch = new System.Diagnostics.Stopwatch();

        public ImageManager()
        {
            image_manager = this;

            lock (reserve_stop_lock)
            {
                reserve_stop_watch.Start();
            }

            loop_thread = new Thread(new ThreadStart(LoopThread));
            loop_thread.Start();
        }

        private void LoopThread()
        {
            while(!loop_thread_end)
            {
                // 予約読み込み処理の停止判定
                lock (reserve_stop_lock)
                {
                    reserve_stop_watch.Stop();
                    if (reserve_stop_watch.ElapsedMilliseconds > RESERVE_STOP_TIME )
                    {
                        is_reserve_stop = true;
                    }
                    else
                    {
                        is_reserve_stop = false;
                    }
                    reserve_stop_watch.Start();
                }

                // 予約の読み込み処理
                if ((is_reserve_stop == false) && (reserve_rimages.Count > 0))
                {
                    // 描画頻度の高いものを読み込む
                    RImage load_rimage = null;
                    lock (loop_thread_lock)
                    {
                        var draw_counter = -1;
                        foreach (var rimage in reserve_rimages)
                        {
                            if (draw_counter < rimage.draw_counter)
                            {
                                draw_counter = rimage.draw_counter;
                                load_rimage = rimage;
                            }
                            rimage.draw_counter--;
                            if (rimage.draw_counter < 0) rimage.draw_counter = 0;
                        }
                    }

                    if (load_rimage != null) LoopThread_LoadImage(load_rimage);
                }
                WaitSleep.Do(0);

                // 最近描画されていないRImageのBitmapを破棄する
                LoopThread_ReleaseImage();



                WaitSleep.Do(0);
                

            }
        }

        private void ForeachRImage( Action<RImage> action )
        {
            lock (loop_thread_lock) // 二重でロックかけてるきがする...
            {
                foreach (var image_by_file in images)
                {
                    foreach (var rimage in image_by_file.Value.rimages)
                    {
                        action(rimage);
                    }
                }
            }
        }

        private void LoopThread_LoadImage(RImage rimage)
        {
            //Console.WriteLine( "{0} {1} {2}", rimage.owner.file_path, rimage.size.Width,  (new Random()).Next(9));

            ImageByFile image_by_file;
            lock (loop_thread_lock)
            {
                reserve_rimages.Remove(rimage);
            }
            image_by_file = rimage.owner;
            //var is_reload = image_by_file.is_size_fix; // 再読込かどうか
            if (!(System.IO.File.Exists(image_by_file.file_path)) ){
                AppCore.core.popup_log.AddMessage("ファイルがありません : "+ image_by_file.file_path+" : LoopThread_LoadImage");
                return;
            }

            try
            {
                var plugin = PluginFileConector.FileConectorManager.GetFileConector(image_by_file.file_path, false); // 現時点ではJPEG,PNGに読み込み方法にちがいがないことと、処理速度面でかなり影響があるためファイル内容チェックをオミットする

                image_by_file.is_size_fix = true; // サイズ確定済み宣言

                //var w = rimage.size.Width;
                //var h = rimage.size.Height;
                try
                {
                    //rimage_bmp_lock.Enter();
                    rimage.lock_object.Enter();
                    switch (rimage.reserve_size_type)
                    {
                        case RImage.ReserveSizeType.Fix:
                            {
                                int src_image_w = 0;
                                int src_image_h = 0;
                                //var ex_word = "";
                                //if (rimage.clip.Width!=0)
                                //{
                                //    ex_word = ImageLibrary.ImageTag.GetExWordByClip(rimage.clip);
                                //}
                                rimage.bmp = plugin.image_conector.FromFile(null, image_by_file.file_path, rimage.ex_word, rimage.size.Width, rimage.size.Height, out src_image_w, out src_image_h);
                                image_by_file.size = new Size(src_image_w, src_image_h);
                                //rimage.size.Width = rimage.bmp.Width;
                                //rimage.size.Height = rimage.bmp.Height;
                            }
                            break;
                        case RImage.ReserveSizeType.FitWidth:
                            {
                                int src_image_w = 0;
                                int src_image_h = 0;
                                rimage.bmp = plugin.image_conector.FromFile(null, image_by_file.file_path, rimage.ex_word, rimage.size.Width, out src_image_w, out src_image_h);
                                image_by_file.size = new Size(src_image_w, src_image_h);
                                rimage.size.Width = rimage.bmp.Width;
                                rimage.size.Height = rimage.bmp.Height;
                            }
                            break;
                        default:
                            throw (new System.Exception(image_by_file.file_path + " RImage.ReserveSizeType.FitWidth以外の予約は実装されていません : LoopThread_LoadImage"));
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    rimage.lock_object.Exit();
                    //rimage_bmp_lock.Exit();
                }

                if (rimage.CreatedImage != null) rimage.CreatedImage(rimage);


            }
            catch
            {
                AppCore.core.popup_log.AddMessage("対象のファイルが読み込めません : " + image_by_file.file_path + " : LoopThread_LoadImage");
            }

        }

        private void LoopThread_ReleaseImage()
        {
            const int DEC_LINE = 10;
            RImage delete_bmp_rimage = null;
            lock (loop_thread_lock)
            {
                // stock_pixelは、作成と解放時に加減して算出できるので毎回しらべるのは効率が悪い
                // ただ、別スレッドでうごいているので、作成と解放に気を使いたくない＆そこまで負荷になってないようなので
                var stock_pixel = 0;
                var delete_bmp_counter_min = -1;
                var delete_bmp_counter_max = -1;
                var stock_counter = 0; // 保持枚数のデバック確認用
                ForeachRImage((rimage) => {
                    if (rimage.bmp != null)
                    {
                        stock_pixel += rimage.size.Width * rimage.size.Height;
                        stock_counter++;
                        if ((delete_bmp_counter_min == -1) || (delete_bmp_counter_min > rimage.draw_counter_release))
                        {
                            delete_bmp_counter_min = rimage.draw_counter_release;
                            delete_bmp_rimage = rimage;
                        }
                        else if ((delete_bmp_counter_max == -1) || (delete_bmp_counter_max < rimage.draw_counter_release))
                        {
                            delete_bmp_counter_max = rimage.draw_counter_release;
                        }
                    }
                });

                if (delete_bmp_counter_max > DEC_LINE)
                {
                    ForeachRImage((rimage) => {
                        if ((rimage.bmp != null) && (rimage.draw_counter_release > 0))
                        {
                            rimage.draw_counter_release--;
                        }
                    });
                }

                if (stock_pixel >= STOCK_PIXEL)
                {
                    try
                    {
                        rimage_bmp_lock.Enter();
                        delete_bmp_rimage.lock_object.Enter();
                        delete_bmp_rimage.bmp.Dispose();
                        delete_bmp_rimage.bmp = null;
                        delete_bmp_rimage.draw_counter = 0;
                        delete_bmp_rimage.draw_counter_release = 0;
                        reserve_rimages.Add(delete_bmp_rimage);
                    }
                    finally
                    {
                        delete_bmp_rimage.lock_object.Exit();
                        rimage_bmp_lock.Exit();
                    }
                }
            }

        }

        static public RImage ReserveRImage(string file_path, int w, int h, double scale, RImage.ReserveSizeType reserve_size_type, string ex_word)
        {
            if (image_manager.images.ContainsKey(file_path))
            {
                var image_by_file = image_manager.images[file_path];
                lock (image_manager.loop_thread_lock)
                {
                    foreach (var rimage in image_by_file.rimages)
                    {
                        if (rimage.reserve_size_type == reserve_size_type)
                        {
                            if( rimage.ex_word==ex_word ){
                                switch (reserve_size_type)
                                {
                                    case RImage.ReserveSizeType.FitWidth:
                                        if (w == rimage.size.Width) return rimage;
                                        break;
                                    case RImage.ReserveSizeType.Fix:
                                        if (w == rimage.size.Width && h == rimage.size.Height) return rimage;
                                        break;
                                    case RImage.ReserveSizeType.Scale:
                                        if (scale == rimage.scale) return rimage;
                                        break;
                                }
                            }
                        }
                    }
                    return image_manager.ReserveRImage_NewRImage(file_path, w, h, scale, reserve_size_type, ex_word);
                }
            }
            else
            {
                return image_manager.ReserveRImage_NewRImage(file_path, w, h, scale, reserve_size_type, ex_word);
            }

        }

        // Bitmapを予約しておく
        static public RImage ReserveRImage( string file_path, int w, int h, double scale, RImage.ReserveSizeType reserve_size_type)
        {

            if ( image_manager.images.ContainsKey(file_path) )
            {
                var image_by_file = image_manager.images[file_path];
                lock (image_manager.loop_thread_lock)
                {
                    foreach (var rimage in image_by_file.rimages)
                    {
                        if (rimage.reserve_size_type == reserve_size_type)
                        {
                            switch (reserve_size_type)
                            {
                                default:
                                    throw (new System.Exception(file_path + " : ReserveRImage"));
                                //case RImage.ReserveSizeType.FitWidth:
                                //    if (w == rimage.size.Width) return rimage;
                                //    break;
                                case RImage.ReserveSizeType.Fix:
                                    if (w == rimage.size.Width && h == rimage.size.Height) return rimage;
                                    break;
                                case RImage.ReserveSizeType.Scale:
                                    if (scale == rimage.scale) return rimage;
                                    break;
                            }
                        }
                    }
                    return image_manager.ReserveRImage_NewRImage(file_path, w, h, scale, reserve_size_type, "");
                }
            }
            else
            {
                return image_manager.ReserveRImage_NewRImage(file_path,w,h,scale,reserve_size_type, "");
            }
        }

        private RImage ReserveRImage_NewRImage( string file_path, int w, int h, double scale, RImage.ReserveSizeType reserve_size_type, string ex_word)
        {

            ImageByFile image_by_file;
            if (images.ContainsKey(file_path))
            {
                image_by_file = images[file_path];
            }
            else
            {
                image_by_file                 = new ImageByFile();
                image_by_file.file_path       = file_path;
            }


            var rimage = new RImage();
            rimage.owner = image_by_file;
            rimage.size = new Size(w, h);
            rimage.scale = scale;
            rimage.reserve_size_type = reserve_size_type;
            rimage.ex_word = ex_word;

            //if ( ( clip.Width!=0) && (clip.Height != 0) )
            //{
            //    rimage.clip = clip;
            //}
            //else
            //{
            //    rimage.clip = null;
            //}

            // 予約
            lock (loop_thread_lock)
            {
                if (!images.ContainsKey(file_path) ) images.Add(file_path, image_by_file);
                image_by_file.rimages.Add(rimage);
                reserve_images.Add(image_by_file);
                reserve_rimages.Add(rimage);
            }
            return rimage;
        }

        // Bitmapを作るだけだが、リソースを食いすぎてエラーになりやすいので、一元管理する
        private Bitmap CreateBitmap( int w, int h )
        {
            try
            {
                var bmp = new Bitmap(w, h);
                return bmp;
            }
            catch
            {
                // リソースが足りない？
                return null;
            }
        }

        public void Dispose()
        {
            loop_thread_end = true;

        }

        // 予約中の画像へ描画要請があったときに呼ばれる
        // 新規画像の読み込みが必要か、放置されており、休めたほうがいいかを判断する材料として利用する
        static public void _DrawReserveImage()
        {
            lock (image_manager.reserve_stop_lock)
            {
                image_manager.reserve_stop_watch.Restart();
            }

        }
    }
}
