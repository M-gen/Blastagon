using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using Blastagon.Common;
using Blastagon.ResourceManage;
using Blastagon.App;

using Microsoft.VisualBasic.FileIO;
using Blastagon.UI.Common;

namespace Blastagon.UI
{

    public class ThumbnailImage
    {
        //public Image image;
        public ImageManager.RImage rimage;

        public Size size;
        public Point pos;

        //
        public bool is_tag_in = false;       // タグに所属している
        public bool is_tag_group_in = false; // 
        public List<ImageLibrary.ImageTag> in_clip_tags = new List<ImageLibrary.ImageTag>(); // クリップ系タグ
        public List<ImageLibrary.Tag> in_tag_group = new List<ImageLibrary.Tag>(); // 
        public ImageLibrary.ImageDataExWordSet data;

        public void Setup( string file_path, int width, int height)
        {
            // 指定されたサイズで対応する
            this.rimage = ImageManager.ReserveRImage(file_path, width, height, 1, ImageManager.RImage.ReserveSizeType.Fix, data.ex_word);

            size.Width = width;
            size.Height = height;
            AppCore.core.thumbnail_view.is_reflesh_body = true;
            rimage.CreatedImage = (rimage) =>
            {
                if (rimage == this.rimage)
                {
                    AppCore.core.thumbnail_view.is_reflesh_body = true;
                }
            };
        }

    }

    public class ThumbnailView : PictureBox
    {

        public LockList<ThumbnailImage> thumbnails = new LockList<ThumbnailImage>();
        List<KeyValuePair<Action<object>,object>> main_loop_timer_action = new List<KeyValuePair<Action<object>, object>>();

        MouseLeftButtonEventController mouse_left_button_event_controller;

        int image_place_width_num = 5;
        int image_one_width;
        Timer main_loop_timer;
        Int64 scroll_y = 0;
        int scroll_y_powwer = 0;
        int scroll_y_powwer_scale = 100;
        int scroll_y_powwer_max   = 20000;
        //object scroll_y_powwer_lock = new object();
        //bool is_scroll_y_powwer_off = false;
        Int64 scroll_y_max = 0;

        bool is_show_tag_list = false;
        bool is_ctrl_mouse_drag = false;            // CTRLマウスドラッグ
        Point ctrl_mouse_drag_start = new Point();
        Point ctrl_mouse_drag_end = new Point();
        bool is_free_mouse_drag = false;            // 制御キー無しマウスドラッグ
        Point free_mouse_drag_start = new Point();
        Point free_mouse_drag_end = new Point();

        public ImageLibrary.Tag select_tag;

        Bitmap bmp_main; // サムネイルなど主要部分

        public Action<Int64> Scroll;
        public Action        ResizeThumbnailSpace;
        public Action<ThumbnailImage> SelectImage;
        public Action<ThumbnailImage,Rectangle> SelectClipImage;

        volatile public bool is_reflesh_body = false;
        volatile object main_lock = new object();

        public ThumbnailView(System.Windows.Forms.Control form)
        {
            Location = new Point(0, 0);
            Size = new Size(1010, 800);

            image_one_width = Size.Width / image_place_width_num;

            form.Controls.Add(this);
            this.Paint      += _Paint;
            //form.MouseWheel += _MouseWheel;
            this.MouseDown += _MouseDown;
            this.MouseMove += _MouseMove;
            this.MouseUp   += _MouseUp;
            main_loop_timer = new Timer();
            main_loop_timer.Interval = 10;
            main_loop_timer.Tick += Main_loop_timer_Tick;
            main_loop_timer.Start();

            scroll_y_powwer = 0;
            bmp_main = new Bitmap(Size.Width, Size.Height);

            mouse_left_button_event_controller = new MouseLeftButtonEventController();
            mouse_left_button_event_controller.event_single_crick = mouse_left_button_event_controller_SingleClick;
            mouse_left_button_event_controller.event_double_crick = mouse_left_button_event_controller_DoubleClick;
            mouse_left_button_event_controller.event_drag_start   = mouse_left_button_event_controller_DragStart;
            mouse_left_button_event_controller.event_drag         = mouse_left_button_event_controller_Drag;
            mouse_left_button_event_controller.event_drag_end     = mouse_left_button_event_controller_DragEnd;
        }

        private void _MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouse_left_button_event_controller.Update_MouseUp(e.X, e.Y);
            }

            // スクロール停止が_MouseDownで取れないことが有るので、保険でいれておく
            if (scroll_y_powwer != 0) scroll_y_powwer = 0;
        }

        private void _MouseMove(object sender, MouseEventArgs e)
        {
            mouse_left_button_event_controller.Update_MouseMove(e.X, e.Y);
        }

        private void _MouseDown(object sender, MouseEventArgs e)
        {
            AppCore.core.tree_list.ReleaseEdit();

            if (e.Button == MouseButtons.Left)
            {
                mouse_left_button_event_controller.Update_MouseDown(e.X, e.Y);


            }
            else if (e.Button == MouseButtons.Right)
            {
                if (is_ctrl_mouse_drag)
                {
                    is_ctrl_mouse_drag = false;
                }
                else
                {
                    var mx = e.X;
                    var my = e.Y + scroll_y;
                    var t = GetHitThumbnailImage(mx, (int)my);
                    if (t != null)
                    {

                        ContextMenuStrip menu = new ContextMenuStrip();
                        menu.Items.Add("表示する(&V)", null, (s, e2) =>
                        {
                            AppCore.ViewImage(t.data);
                        });
                        menu.Items.Add("関連付けられたソフトで開く(&F)", null, (s, e2) =>
                        {
                            var p = new System.Diagnostics.Process();
                            p.StartInfo.FileName = t.data.image_data.file_path;
                            var result = p.Start();
                        });
                        menu.Items.Add("フォルダを開く(&W)", null, (s, e2) =>
                        {
                            var pos = t.data.image_data.file_path.LastIndexOf(@"\");
                            var dir = t.data.image_data.file_path.Substring(0, pos);
                            var name = t.data.image_data.file_path.Substring(pos + 1);

                        //var p = new System.Diagnostics.Process();
                        //p.StartInfo.FileName = dir;
                        //p.StartInfo.Arguments = name;
                        var p = new System.Diagnostics.Process();
                            p.StartInfo.FileName = "explorer";
                            p.StartInfo.Arguments = @"/select," + t.data.image_data.file_path;
                            var result = p.Start();

                        });
                        menu.Items.Add("ファイルパスをクリップボードにコピー(&E)", null, (s, e2) =>
                        {
                            Clipboard.SetText(t.data.image_data.file_path);
                        });
                        menu.Items.Add("画像をクリップボードにコピー(&C)", null, (s, e2) =>
                        {
                            using (var bmp = new Bitmap(t.data.image_data.file_path))
                            {
                                var is_clip = false;
                                var clip = new Rectangle(0,0,0,0);
                                t.data.AnalyzeExWord(ref clip, ref is_clip);
                                if (is_clip)
                                {
                                    using (var bmp2 = new Bitmap(clip.Width, clip.Height))
                                    {
                                        var g = Graphics.FromImage(bmp2);
                                        g.DrawImage(bmp, new Rectangle(0, 0, clip.Width, clip.Height), clip, GraphicsUnit.Pixel);
                                        Clipboard.SetImage(bmp2);
                                    }
                                }
                                else
                                {
                                    Clipboard.SetImage(bmp);
                                }
                            }
                        });
                        menu.Items.Add("タグをクリップボードにコピー(&T)", null, (s, e2) =>
                        {
                            var str = "";
                            foreach (var tag in t.data.image_data.tags)
                            {
                                str += tag.tag.GetFullName() + "\n";
                            }
                            Clipboard.SetText(str);
                        });
                        menu.Items.Add(new ToolStripSeparator());
                        menu.Items.Add("ピンをつける(&P)", null, (s, e2) =>
                        {
                            AppCore.core.SetPinImageData(t.data, true);
                            this.Refresh();
                        });
                        menu.Items.Add("ピンをはずす(&O)", null, (s, e2) =>
                        {
                            AppCore.core.SetPinImageData(t.data, false);
                            this.Refresh();
                        });

                        menu.Items.Add("ピンをつけた画像を表示(&@)", null, (s, e2) =>
                        {
                            AppCore.core.ShowThubnailPin();
                        });
                        menu.Items.Add(new ToolStripSeparator());
                        menu.Items.Add("ピックアップに追加(&Q)", null, (s, e2) =>
                        {
                            AppCore.core.picup_view.Add(t.data.image_data);
                        });
                        menu.Items.Add("ピックアップの削除", null, (s, e2) =>
                        {
                            AppCore.core.picup_view.Remove(t.data.image_data);
                        });
                        menu.Items.Add(new ToolStripSeparator());
                        menu.Items.Add("サムネイルの全てを完全に削除する(&L)", null, (s, e2) =>
                        {
                            var res = MessageBox.Show("サムネイルに表示されているファイルを全てを完全に削除しますか？", "確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                            if (res == DialogResult.OK)
                            {
                                GC.Collect();
                                foreach (var t2 in thumbnails)
                                {
                                    var file_path = t2.data.image_data.file_path;
                                    AppCore.core.image_library.RemoveImage(file_path);
                                    //AppCore.core.image_library.image_datas.Remove(file_path);
                                    //thumbnails.Remove(t2);

                                    var counter = 0;
                                    var counter_max = 3;
                                    var is_ok = true;
                                    do
                                    {
                                        if (counter > counter_max) break;
                                        else if (counter != 0) WaitSleep.Do(0);
                                        is_ok = true;
                                        try
                                        {
                                            FileSystem.DeleteFile(file_path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                                        }
                                        catch
                                        {
                                            is_ok = false;
                                            Console.WriteLine("b " + file_path);
                                        }
                                        counter++;
                                    } while (!is_ok);
                                }
                                thumbnails.Clear();

                                is_reflesh_body = true;

                            }

                        });
                        menu.Items.Add(new ToolStripSeparator());
                        menu.Items.Add("管理から外す(&U)", null, (s, e2) =>
                        {
                            AppCore.core.image_library.RemoveImage(t.data.image_data.file_path);
                            //AppCore.core.image_library.image_datas.Remove(t.data.image_data.file_path);
                            thumbnails.Remove(t);
                            is_reflesh_body = true;
                        });
                        menu.Items.Add("管理から外し、元ファイルも完全に削除する(&K)", null, (s, e2) =>
                        {
                            var res = MessageBox.Show(t.data.image_data.file_path + "\n元ファイルを完全に削除しますか？", "確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                            if (res == DialogResult.OK)
                            {
                                //AppCore.core.image_library.image_datas.Remove(t.data.image_data.file_path);
                                AppCore.core.image_library.RemoveImage(t.data.image_data.file_path);
                                thumbnails.Remove(t);

                                try
                                {
                                    FileSystem.DeleteFile(t.data.image_data.file_path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                                }
                                catch
                                {
                                }
                                is_reflesh_body = true;

                            }

                        });
                        menu.Show(Cursor.Position, ToolStripDropDownDirection.AboveLeft);
                    }
                }
            }

            if (scroll_y_powwer != 0)
            {
                scroll_y_powwer = 0;
                mouse_left_button_event_controller.Reset();
            }
        }

        private ThumbnailImage GetHitThumbnailImage(int x, int y)
        {
            lock (main_lock)
            {
                foreach (var t in thumbnails)
                {
                    if ((t.pos.X < x && x < t.pos.X + t.size.Width) &&
                        (t.pos.Y < y && y < t.pos.Y + t.size.Height))
                    {
                        return t;
                    }
                }
            }
            return null;
        }

        private ImageLibrary.ImageTag GetHitThumbnailClipImage( ThumbnailImage t, int x, int y)
        {
            // todo : 計算が合わない,
            var clip = new Rectangle(0, 0, 0, 0);
            var is_clip = false;
            t.data.AnalyzeExWord( ref clip, ref is_clip); // クリップされているので、調整が必要
            var x0 = x - t.pos.X;
            var y0 = y - t.pos.Y;
            var scale = ((double)t.size.Width / (double)t.data.image_data.size.Width);
            if (is_clip)
            {
                scale = ((double)t.size.Width / (double)clip.Width);
                //var scale2 = (double)clip.Width / (double)t.data.image_data.size.Width;
                x0 = (int)(x0 / scale + clip.X);
                y0 = (int)(y0 / scale + clip.Y);
            }
            else
            {
                x0 = (int)(x0 / scale);
                y0 = (int)(y0 / scale);
            }

            foreach (var tag in t.in_clip_tags)
            {
                if (((tag.clip.X ) <= x0 && x0 <= ((tag.clip.X + tag.clip.Width) ))
                    && ((tag.clip.Y ) <= y0 && y0 <= ((tag.clip.Y + tag.clip.Height) )))
                {
                    return tag;
                }
            }
            return null;

        }

        private void Main_loop_timer_Tick(object sender, EventArgs e)
        {
            if (main_loop_timer_action.Count() != 0)
            {

                var action = main_loop_timer_action[0];
                action.Key(action.Value);

                main_loop_timer_action.RemoveAt(0);
            }

            if (scroll_y_powwer != 0)
            {
                if (((Control.MouseButtons & MouseButtons.Middle) == MouseButtons.Middle) ||
                        ((Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left))
                {
                    scroll_y_powwer = 0;
                    mouse_left_button_event_controller.Reset();
                }

                SetScrollValue(scroll_y + scroll_y_powwer / scroll_y_powwer_scale, true);

                if (scroll_y_powwer > 0) scroll_y_powwer--;
                else scroll_y_powwer++;
                //if (Math.Abs(scroll_y_powwer) < 10) scroll_y_powwer = scroll_y_powwer / 2;
                this.is_reflesh_body = true;
            }

            if (is_reflesh_body)
            {
                is_reflesh_body = false;
                _UpdatePosThumbnail();
                this.RefreshBody();
            }
        }

        public void SetScrollValue( Int64 pos, bool is_action )
        {
            scroll_y = pos;
            if (scroll_y < 0)
            {
                scroll_y = 0;
                scroll_y_powwer = 0;
            }
            if (scroll_y_max < scroll_y)
            {
                scroll_y = scroll_y_max;
                scroll_y_powwer = 0;
            }
            if (is_action && Scroll != null) Scroll(scroll_y);
            this.is_reflesh_body = true;
        }

        private void AddMainLoopAction(Action<object> action, object obj)
        {
            main_loop_timer_action.Add(new KeyValuePair<Action<object>, object>(action, obj));
        }

        public ThumbnailImage AddImages( string file_path, int w, int h, string ex_word )
        {
            {
                var f = file_path.ToUpper();
                var is_ok = false;
                if (f.IndexOf(".PNG") > 0) is_ok = true;
                if (f.IndexOf(".JPEG") > 0) is_ok = true;
                if (f.IndexOf(".JPG") > 0) is_ok = true;
                if (!is_ok) return null;
            }

            var t = new ThumbnailImage();
            thumbnails.Add(t);
            t.data = new ImageLibrary.ImageDataExWordSet( new ImageLibrary.ImageData(), ex_word);
            t.data.image_data.file_path = file_path;
            t.data.image_data.size.Width = w;
            t.data.image_data.size.Height = h;
            t.Setup(file_path, image_one_width, image_one_width * h / w);

            //is_reflesh_body = true; // @@@

            return t;
        }

        public ThumbnailImage AddImages(ImageLibrary.ImageDataExWordSet image_data_ex_word)
        {
            var t = new ThumbnailImage();
            thumbnails.Add(t);

            t.data = image_data_ex_word;

            var clip = new Rectangle(0, 0, 0, 0);
            var is_clip = false;
            t.data.AnalyzeExWord( ref clip, ref is_clip);

            var w = image_one_width;
            var h = image_one_width * image_data_ex_word.image_data.size.Height / image_data_ex_word.image_data.size.Width;
            if ( is_clip )
            {
                w = image_one_width;
                h = image_one_width * clip.Height / clip.Width;
            }

            t.Setup(image_data_ex_word.image_data.file_path, w, h);
            //t.Setup(image_data_ex_word.image_data.file_path, image_one_width,
            //    image_one_width * image_data_ex_word.image_data.size.Height / image_data_ex_word.image_data.size.Width);

            //if (image_data_ex_word.image_data.size.Width != 0)
            //{
            //    t.Setup(image_data_ex_word.image_data.file_path, image_one_width,
            //        image_one_width * image_data_ex_word.image_data.size.Height / image_data_ex_word.image_data.size.Width, false);
            //}
            //else
            //{
            //t.Setup(image_data_ex_word.image_data.file_path, image_one_width, 0, image_data_ex_word.ex_word);
            //}

            is_reflesh_body = true;

            return t;
        }

        // 各サムネイルの位置を算出する
        // 画像描画時にサイズが確定する場合があるため、頻繁に呼び出している
        public void _UpdatePosThumbnail()
        {
            var old_scroll_y_max = scroll_y_max;

            var max_w = Size.Width;
            var w_num = max_w / image_one_width;
            var x_lines_y = new int[w_num];
            for (var i = 0; i < w_num; i++) x_lines_y[i] = 0;

            // 各サムネイルの位置を算出
            foreach (var t in thumbnails)
            {
                var y_min = -1;
                var target_i = 0;
                for (var i = 0; i < w_num; i++)
                {
                    if ((y_min > x_lines_y[i]) || (y_min == -1))
                    {
                        y_min = x_lines_y[i];
                        target_i = i;
                    }
                }
                var target_y = x_lines_y[target_i];
                x_lines_y[target_i] += t.size.Height;

                t.pos = new Point(target_i * image_one_width, target_y);
            }

            // 高さを算出
            var h = 0;
            for (var i = 0; i < w_num; i++)
            {
                if (h < x_lines_y[i])
                {
                    h = x_lines_y[i];
                }
            }
            scroll_y_max = h - Size.Height;
            if (scroll_y_max < 0) scroll_y_max = 0;

             
            if (old_scroll_y_max != scroll_y_max)
            {
                if (ResizeThumbnailSpace != null) ResizeThumbnailSpace();
            }
            else
            {
                if (ResizeThumbnailSpace != null) ResizeThumbnailSpace();

            }
            //if (ResizeThumbnailSpace != null) ResizeThumbnailSpace();

        }

        private void _Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            g.DrawImage(bmp_main, new Point(0, 0));

            //lock (main_lock)
            //{
            if(thumbnails.Count()==0)
            {

                Pen blackPen = new Pen(Color.FromArgb(60, 255, 255, 255), 5);
                blackPen.DashStyle = DashStyle.Dot;
                //g.DrawLine(blackPen, 10, 70, 200, 70);
                var margin = 20;
                g.DrawRectangle(blackPen, margin, margin, Size.Width - margin * 2, Size.Height - margin * 2);
                g.DrawString("こちらへ画像をドロップしてください", new Font("メイリオ", 20), new SolidBrush(Color.FromArgb(128, 255, 255, 255)), 40, 40);
                g.DrawString("対応形式 : PNG JPEG", new Font("メイリオ", 20), new SolidBrush(Color.FromArgb(100, 255, 255, 255)), 40, 40+50);
            }

            foreach (var t in thumbnails)
            {
                //
                var is_view = false;
                {
                    var y1 = (int)(t.pos.Y - scroll_y);
                    var h1 = t.size.Height;
                    var y2 = 0;
                    var h2 = Size.Height;
                    if (y1 + h1 >= y2 && y1 <= y2 + h2)
                    {
                        is_view = true;
                    }
                }
                if (!is_view) continue;

                if (t.is_tag_group_in)
                {
                    var region = new Region(new Rectangle(t.pos.X, (int)(t.pos.Y - scroll_y), t.size.Width, t.size.Height));
                    g.SetClip(region, CombineMode.Replace);
                    UserInterfaceCommon.DrawPattarnRect(g, t.pos.X, (int)(t.pos.Y - scroll_y), t.size.Width, t.size.Height, 10, 160);


                    {
                        var brush = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
                        var brush_text = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

                        var group_tag_num = t.in_tag_group.Count();
                        var one_h = 34;
                        var h = one_h * group_tag_num;
                        g.FillRectangle(brush, new Rectangle(t.pos.X, (int)(t.pos.Y - scroll_y) + t.size.Height - h, t.size.Width, h));

                        var select_tag_name_length = select_tag.GetFullName().Length;

                        for (var i = 0; i < group_tag_num; i++)
                        {
                            var name = t.in_tag_group[i].GetFullName().Substring(select_tag_name_length + 2).Replace(@"\:", " ");
                            g.DrawString(name, new Font("メイリオ", 20), brush_text, t.pos.X, (int)(t.pos.Y - scroll_y) + t.size.Height - h + one_h * i);
                        }
                    }

                    UserInterfaceCommon.DrawStringFrame(g, "メイリオ", 20, select_tag.name, t.pos.X, (int)(t.pos.Y - scroll_y),
                            Color.FromArgb(200, 255, 255, 255), Color.FromArgb(128, 0, 0, 0), 3);
                }
                else if (t.is_tag_in)
                {
                    var region = new Region(new Rectangle(t.pos.X, (int)(t.pos.Y - scroll_y), t.size.Width, t.size.Height));
                    g.SetClip(region, CombineMode.Replace);
                    UserInterfaceCommon.DrawPattarnRect(g, t.pos.X, (int)(t.pos.Y - scroll_y), t.size.Width, t.size.Height, 10, 160);

                    UserInterfaceCommon.DrawStringFrame(g, "メイリオ", 20, select_tag.name, t.pos.X, (int)(t.pos.Y - scroll_y),
                            Color.FromArgb(200, 255, 255, 255), Color.FromArgb(128, 0, 0, 0), 3);
                }

                if (t.in_clip_tags.Count()>0)
                {
                    var clip = new Rectangle(0, 0, 0, 0);
                    var is_clip = false;
                    t.data.AnalyzeExWord( ref clip, ref is_clip); // クリップされているので、調整が必要
                        
                    var scale = (double)t.size.Width / (double)t.data.image_data.size.Width;
                    if (is_clip) scale *= (double)t.data.image_data.size.Width / (double)clip.Width;

                    var pen = new Pen(Color.FromArgb(255, 255, 0, 0));
                    foreach (var tt in t.in_clip_tags)
                    {
                        var x0 = (int)(tt.clip.X * scale + t.pos.X);
                        var y0 = (int)(tt.clip.Y * scale + t.pos.Y - scroll_y);
                        var w0 = (int)(tt.clip.Width * scale);
                        var h0 = (int)(tt.clip.Height * scale);
                        if (is_clip)
                        {
                            x0 -= (int)(clip.X * scale);
                            y0 -= (int)(clip.Y * scale);
                        }

                        //if (t.size.Width  < w0) w0 = t.size.Width-1;
                        //if (t.size.Height < h0) h0 = t.size.Height - 1;

                        g.DrawRectangle(pen, x0, y0, w0, h0);
                    }
                }

                if (is_show_tag_list)
                {
                    var region = new Region(new Rectangle(t.pos.X, (int)(t.pos.Y - scroll_y), t.size.Width, t.size.Height));
                    g.SetClip(region, CombineMode.Replace);

                    var brush = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
                    var brush_text = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

                    var group_tag_num = t.in_tag_group.Count();
                    var one_h = 34;

                    for (var i = 0; i < t.data.image_data.tags.Count; i++)
                    {
                        var name = t.data.image_data.tags[i].tag.name;
                        UserInterfaceCommon.DrawStringFrame(g, "メイリオ", 20, name, t.pos.X, (int)(t.pos.Y - scroll_y) + one_h * i,
                                Color.FromArgb(200,255, 255, 255), Color.FromArgb(128,0, 0, 0), 3);
                    }

                }

                if (AppCore.core.image_library.IsPinOn(t.data))
                {
                    g.FillRectangle( new SolidBrush(Color.FromArgb(255,255,0,0)),  t.pos.X + 4, t.pos.Y +  4 - scroll_y, 10,10);
                }
                if(AppCore.core.picup_view.IsPickUp(t.data.image_data))
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 0, 0, 255)), t.pos.X + 4, t.pos.Y + 4 - scroll_y, 10, 10);
                }

            }
            //}
            {
                var region = new Region(new Rectangle(0, 0, Size.Width, Size.Height));
                g.SetClip(region, CombineMode.Replace);
            }

            if ( is_ctrl_mouse_drag )
            {
                var pen1 = new Pen(Color.FromArgb(255, 255, 125, 100));
                var pen2 = new Pen(Color.FromArgb(80, 0, 0, 0));

                var t = GetHitThumbnailImage(ctrl_mouse_drag_start.X, (int)(ctrl_mouse_drag_start.Y + scroll_y));
                long x, y, w, h;
                GetClipRectangle(t, out x, out y, out w, out h);
                y -= scroll_y;

                g.DrawRectangle(pen2, x - 1, y - 1, w + 2, h + 2);
                g.DrawRectangle(pen2, x+1, y + 1, w - 2, h - 2);
                g.DrawRectangle(pen1, x, y, w, h);
            }

            AppCore.core.popup_log.Paint(g,Size);
        }

        private void GetClipRectangle( ThumbnailImage t, out long x, out long y, out long w, out long h  )
        {
            x = ctrl_mouse_drag_start.X;       if (x > ctrl_mouse_drag_end.X) x = ctrl_mouse_drag_end.X;
            y = (long)ctrl_mouse_drag_start.Y; if (y > ctrl_mouse_drag_end.Y) y = ctrl_mouse_drag_end.Y; y += scroll_y;
            w = Math.Abs(ctrl_mouse_drag_end.X - ctrl_mouse_drag_start.X);
            h = Math.Abs(ctrl_mouse_drag_end.Y - ctrl_mouse_drag_start.Y);

            if (x < t.pos.X)
            {
                var d = t.pos.X - x;
                x = t.pos.X;
                w -= d;
            }
            else if (t.size.Width < (x - t.pos.X) + w)
            {
                w = t.size.Width - (x - t.pos.X);
            }


            if (y < t.pos.Y)
            {
                var d = t.pos.Y - y;
                y = t.pos.Y;
                h -= (int)d;
            }
            else if (t.size.Height < (y - t.pos.Y) + h)
            {
                h = t.size.Height - (int)(y - t.pos.Y);
            }

        }

        public void RefreshBody()
        {
            // 
            var i = 0;
            using (var g = Graphics.FromImage(bmp_main))
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), new Rectangle(0, 0, Size.Width, Size.Height));

                foreach (var t in thumbnails)
                {
                    i++;
                    // 描画範囲にあるかどうか
                    var RESERVE_LOAD_Y_MARGIN = 100; // 表示領域の前後もよけいに描画命令を出しておくことで、事前読み込みがスムーズに見える
                    var is_draw = false;
                    var y1 = (int)(t.pos.Y - scroll_y - RESERVE_LOAD_Y_MARGIN);
                    var h1 = t.size.Height + RESERVE_LOAD_Y_MARGIN * 2;
                    var y2 = 0;
                    var h2 = Size.Height;
                    if (y1 + h1 >= y2 && y1 <= y2 + h2)
                    {
                        is_draw = true;
                    }

                    try
                    {
                        if (is_draw)
                        {
                            t.rimage.Draw(g, t.pos.X, (int)(t.pos.Y - scroll_y));
                        }
                    }
                    catch
                    {

                    }
                }
            }
            this.Refresh();
        }

        public  void _MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (AppCore.core.image_view.is_show) return;

            int numberOfTextLinesToMove = e.Delta * SystemInformation.MouseWheelScrollLines / 120;

            scroll_y_powwer -= numberOfTextLinesToMove * 400;
            if (scroll_y_powwer < -scroll_y_powwer_max) scroll_y_powwer = -scroll_y_powwer_max;
            if (scroll_y_powwer > scroll_y_powwer_max) scroll_y_powwer = scroll_y_powwer_max;
        }

        public Int64 GetScrollValueMax()
        {
            return scroll_y_max;
        }

        public void SetSelectTag( ImageLibrary.Tag tag )
        {
            select_tag = tag;

            if (select_tag == null)
            {
                foreach (var t in thumbnails)
                {
                    t.is_tag_in = false;
                    t.is_tag_group_in = false;
                    t.in_tag_group.Clear();
                    t.in_clip_tags.Clear();
                }
            }
            else
            {
                foreach (var t in thumbnails)
                {
                    t.is_tag_in = false;
                    t.is_tag_group_in = false;
                    t.in_tag_group.Clear();
                    t.in_clip_tags.Clear();
                    foreach (var tt in t.data.image_data.tags)
                    {
                        var tag_group = ImageLibrary.Tag.CheckGroupTag(tag, tt.tag);
                        if (tag_group != null)
                        {
                            t.is_tag_group_in = true;
                            t.in_tag_group.Add(tt.tag);

                        }
                        if (tag == tt.tag)
                        {
                            t.is_tag_in = true;
                            if (tt.clip!=null)
                            {
                                t.in_clip_tags.Add(tt);
                            }
                        }
                    }
                }
            }
            this.Refresh();
        }

        public void ClearAndAddImages(List<ImageLibrary.ImageDataExWordSet> image_datas)
        {
            // todo 毎回、作り直しているので、このへんは効率化できるはず
            ClearImages();

            lock (main_lock)
            {
                //AddImages(ImageLibrary.ImageData image_data)
                foreach( var i in image_datas )
                {
                    AddImages(i);
                }
            }

        }

        public void ClearImages()
        {
            SetScrollValue(0, false);

            main_loop_timer_action.Clear();

            thumbnails.Clear();
        }

        // そのイメージはサムネイルに表示されているか
        public bool ExistImage(ImageLibrary.ImageData image_data)
        {
            foreach (var t in thumbnails)
            {
                if (t.data.image_data == image_data) return true;
            }
            return false;
        }

        public ImageLibrary.ImageDataExWordSet GetNextImage(ImageLibrary.ImageDataExWordSet image_data_ex_word )
        {
            var i = 0;
            foreach (var t in thumbnails)
            {
                if (t.data == image_data_ex_word) break;
                i++;
            }

            if (i >= thumbnails.Count()) return null;

            i++;
            if (i >= thumbnails.Count()) i -= thumbnails.Count();
            return thumbnails[i].data;

        }

        public ImageLibrary.ImageDataExWordSet GetBackImage(ImageLibrary.ImageDataExWordSet image_data_ex_word )
        {
            var i = 0;
            foreach (var t in thumbnails)
            {
                if (t.data == image_data_ex_word) break;
                i++;
            }

            if (i >= thumbnails.Count()) return null;

            i--;
            if (i < 0) i += thumbnails.Count();
            return thumbnails[i].data;

        }

        public void ResizeOrLineNumChane()
        {
            image_one_width = Size.Width / image_place_width_num;
            
            // 今のサムネイルデータを全て破棄

            bmp_main.Dispose();
            bmp_main = new Bitmap(Size.Width, Size.Height);

            foreach (var t in thumbnails)
            {
                var clip = new Rectangle(0, 0, 0, 0);
                var is_clip = false;
                t.data.AnalyzeExWord( ref clip, ref is_clip);

                var w = image_one_width;
                var h = image_one_width * t.data.image_data.size.Height / t.data.image_data.size.Width;
                if (is_clip)
                {
                    w = image_one_width;
                    h = image_one_width * clip.Height / clip.Width;
                }
                t.Setup(t.data.image_data.file_path, w, h);

            }

            is_reflesh_body = true;
        }

        public void SetLineNum( int line_num )
        {
            image_place_width_num = line_num;
            image_one_width = Size.Width / image_place_width_num;
            ResizeOrLineNumChane();
        }

        public void ReleaseTagByThubnail(ImageLibrary.Tag tag)
        {
            var delte_thumbnail = new List<ThumbnailImage>();
            foreach (var t in thumbnails)
            {
                foreach (var t2 in t.data.image_data.tags)
                {
                    if(t2.tag == tag)
                    {
                        delte_thumbnail.Add(t);
                        break;
                    }
                    else if (ImageLibrary.Tag.CheckGroupTag(tag, t2.tag) !=null)
                    {
                        delte_thumbnail.Add(t);
                        break;
                    }
                }
            }

            foreach (var t in delte_thumbnail)
            {
                thumbnails.Remove(t);
            }

            is_reflesh_body = true;

        }

        public void SetIsShowTagList(bool is_show_tag_list)
        {
            this.is_show_tag_list = is_show_tag_list;
        }

        public bool GetIsShowTagList()
        {
            return is_show_tag_list;
        }

        public Int64 GetScrollHeight()
        {
            return scroll_y_max;
        }

        private void mouse_left_button_event_controller_SingleClick(MouseLeftButtonEventController ec)
        {
            Console.WriteLine("SingleClick");

            AppCore.core.ReleaseFocusTagSearchBox();
            
            //if ( is_scroll_y_powwer_off )
            //{
            //    is_scroll_y_powwer_off = false;
            //}
            //else if (scroll_y_powwer != 0)
            //{
            //    // スクロール中
            //}
            //else if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            //{
            //    if (select_tag != null)
            //    {
            //        is_ctrl_mouse_drag = true;
            //        ctrl_mouse_drag_start = new Point(e.X, e.Y);
            //    }
            //}
            //else
            {
                if (select_tag == null)
                {
                }
                else
                {
                    var mx = ec.click_start_point.X;
                    var my = ec.click_start_point.Y + scroll_y;


                    var t = GetHitThumbnailImage(mx, (int)my);

                    if (t != null)
                    {
                        if ((t.in_clip_tags.Count > 0) && (t.in_clip_tags[0].clip.Width > 0))
                        {
                            // クリップタグがクリックされたのかどうか
                            var tag = GetHitThumbnailClipImage(t, mx, (int)my);
                            if (tag != null)
                            {
                                t.in_clip_tags.Remove(tag);
                                t.data.image_data.tags.Remove(tag);
                                this.Refresh();
                            }
                        }
                        else if (t.is_tag_group_in && !t.is_tag_in)
                        {
                            // 下のタグで所属済みなので、ややこしくなるから上のタグでのtrue化を禁止
                        }
                        else
                        {
                            if (t.is_tag_in) t.is_tag_in = false;
                            else t.is_tag_in = true;

                            if (SelectImage != null) SelectImage(t);
                            //this.Refresh();
                            is_reflesh_body = true;
                        }
                    }
                }
            }
        }
        private void mouse_left_button_event_controller_DoubleClick(MouseLeftButtonEventController ec)
        {
            Console.WriteLine("DoubleClick");

            var mx = ec.click_start_point.X;
            var my = ec.click_start_point.Y + scroll_y;
            var t = GetHitThumbnailImage(mx, (int)my);
            if (t != null)
            {
                AppCore.ViewImage(t.data);
            }
        }
        private void mouse_left_button_event_controller_DragStart(MouseLeftButtonEventController ec)
        {
            Console.WriteLine("DragStart");

            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (select_tag != null)
                {
                    is_ctrl_mouse_drag = true;
                    ctrl_mouse_drag_start = new Point(ec.drag_start_point.X, ec.drag_start_point.Y);
                }
            }
            else
            {
                is_free_mouse_drag = true;
                free_mouse_drag_current_y = ec.drag_start_point.Y;
                free_mouse_drag_start = new Point(ec.drag_start_point.X, ec.drag_start_point.Y);
            } 
            //drag_start_pos = this.Location;

            //drag_start_mouse_pos = new Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
        }

        int free_mouse_drag_current_y = 0;
        private void mouse_left_button_event_controller_Drag(MouseLeftButtonEventController ec)
        {
            Console.WriteLine("Drag");
            if (is_ctrl_mouse_drag)
            {
                ctrl_mouse_drag_end = new Point(ec.drag_now_point.X, ec.drag_now_point.Y);
                this.Refresh();
            }
            else if (is_free_mouse_drag)
            {
                free_mouse_drag_end = new Point(ec.drag_now_point.X, ec.drag_now_point.Y);

                Console.WriteLine($"v0 {scroll_y_powwer}");
                var v = 0;
                v = ( free_mouse_drag_current_y - free_mouse_drag_end.Y) * 5;
                //v = ( free_mouse_drag_current_y - free_mouse_drag_end.Y) * 100;
                free_mouse_drag_current_y = free_mouse_drag_end.Y;
                //if ((free_mouse_drag_current_y- free_mouse_drag_end.Y) > 0)
                ////    if (free_mouse_drag_start.Y > free_mouse_drag_end.Y)
                //{
                //    v = 10;
                //}
                //else
                //{
                //    v = -10;
                //}
                //int numberOfTextLinesToMove = e.Delta * SystemInformation.MouseWheelScrollLines / 120;
                //scroll_y_powwer -= numberOfTextLinesToMove * 400;

                if (false)
                {
                    //scroll_y_powwer = 100;
                    scroll_y_powwer += v;
                    if (scroll_y_powwer < -scroll_y_powwer_max) scroll_y_powwer = -scroll_y_powwer_max;
                    if (scroll_y_powwer > scroll_y_powwer_max) scroll_y_powwer = scroll_y_powwer_max;
                }
                else
                {
                    SetScrollValue(scroll_y + v, true);

                }
                //is_reflesh_body = true;
                Console.WriteLine($"v {scroll_y_powwer}");

            }

            //drag_now_mouse_pos = new Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);

            //var mx = drag_now_mouse_pos.X - drag_start_mouse_pos.X;
            //var my = drag_now_mouse_pos.Y - drag_start_mouse_pos.Y;
            ////this.Location = new Point(drag_start_pos.X - mx, drag_start_pos.Y - my);
            //var next_x = drag_start_pos.X + mx;

            //if (next_x > this.Parent.ClientSize.Width - this.Size.Width)
            //{
            //    next_x = this.Parent.ClientSize.Width - this.Size.Width;
            //}

            //this.Location = new Point(next_x, 0);

            //if (event_drag != null) event_drag(this);
        }

        private void mouse_left_button_event_controller_DragEnd(MouseLeftButtonEventController ec)
        {
            Console.WriteLine("DragEnd");

            if (is_ctrl_mouse_drag)
            {
                var mx = ctrl_mouse_drag_start.X;
                var my = ctrl_mouse_drag_start.Y + scroll_y;
                var t = GetHitThumbnailImage(mx, (int)my);

                long x0, y0, w0, h0;
                GetClipRectangle(t, out x0, out y0, out w0, out h0);

                var clip = new Rectangle(0, 0, 0, 0);
                var is_clip = false;
                //t.AnalyzeExWord(t.data.ex_word, ref clip, ref is_clip); // クリップされているので、調整が必要
                t.data.AnalyzeExWord(ref clip, ref is_clip); // クリップされているので、調整が必要

                // 画像としての　クリッピング矩形を算出
                var scale = (double)t.rimage.size.Width / (double)t.data.image_data.size.Width;
                if (is_clip) scale = (double)t.rimage.size.Width / (double)clip.Width;
                var x1 = (int)((x0 - t.pos.X) / scale);
                var y1 = (int)((y0 - t.pos.Y) / scale);
                var w1 = (int)(w0 / scale);
                var h1 = (int)(h0 / scale);
                if (is_clip)
                {
                    x1 += (int)(clip.X);
                    y1 += (int)(clip.Y);
                }

                //Console.WriteLine("{0} {1} {2} {3}", x1, y1, w1, h1);
                //Console.WriteLine("t {0} {1}", t.data.image_data.size.Width, t.data.image_data.size.Height);

                t.is_tag_in = true;
                if (SelectClipImage != null) SelectClipImage(t, new Rectangle((int)x1, (int)y1, (int)w1, (int)h1));
                is_ctrl_mouse_drag = false;

            }
            else if (is_free_mouse_drag)
            {
                Console.WriteLine($"v3 {scroll_y_powwer}");
                Console.WriteLine($"Drag ({free_mouse_drag_start.X},{free_mouse_drag_start.Y}) ({free_mouse_drag_end.X},{free_mouse_drag_end.Y})");
                is_free_mouse_drag = false;
            }
        }
    }
}
