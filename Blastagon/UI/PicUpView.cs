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

//using Microsoft.VisualBasic.FileIO;
using Blastagon.UI.Common;

namespace Blastagon.UI
{
    public class PicUpView : PictureBox
    {
        class ImageBox
        {
            public ImageLibrary.ImageData image_data;
            //public ImageManager.RImage rimage;
            public Image image;
            public RectangleD view_rect; // 表示領域
            public RectangleD src_rect ; // 内部の表示領域
            public int offset_x = 0;
            public int offset_y = 0;
            public double scale = 1.0;

            public ImageBox(ImageLibrary.ImageData image_data)
            {
                this.image_data = image_data;
            }
        }

        enum DragMode
        {
            None,
            BoxMove,    // 矩形を移動
            BoxInMove,  // 中のオフセットを移動
            //
            BoxResizeW, // 左西  N
            BoxResizeE, // 右東 W E
            BoxResizeN, // 上北  S
            BoxResizeS, // 下南
            BoxResizeWN, // WN EN
            BoxResizeEN, // WS ES
            BoxResizeWS, // 
            BoxResizeES, // 
            //
            BoxFitResizeW, // 左西
            BoxFitResizeE, // 右東
            BoxFitResizeN, // 上北
            BoxFitResizeS, // 下南
            BoxFitResizeWN, //
            BoxFitResizeEN, //
            BoxFitResizeWS, //
            BoxFitResizeES, //
        }

        enum MouseMode
        {
            Normal,
            Cut
        }

        class DragStatus
        {
            public DragMode  drag_mode  = DragMode.None;
            public MouseMode mouse_mode = MouseMode.Normal;
            public bool is_mouse_left_drag = false;
            //public PointD mouse_left_drag_move = new PointD(0, 0);

            public RectangleD view_start_rect = new RectangleD(0, 0, 0, 0);
            public RectangleD view_now_rect   = new RectangleD(0, 0, 0, 0);
            public RectangleD src_start_rect  = new RectangleD(0, 0, 0, 0);
            public RectangleD src_now_rect    = new RectangleD(0, 0, 0, 0);

        }

        public bool is_show = false;
        public bool is_reflesh = false;
        List<ImageBox> items = new List<ImageBox>();
        MouseLeftButtonEventController mouse_left_button_event_controller;

        ImageBox select_item;
        Timer main_loop_timer;
        DragStatus drag_status = new DragStatus();

        public PicUpView()
        {
            this.Paint += _Paint;
            this.Hide();
            this.MouseMove += _MouseMove;
            this.MouseDown += _MouseDown;
            this.MouseUp   += _MouseUp;

            mouse_left_button_event_controller = new MouseLeftButtonEventController();
            mouse_left_button_event_controller.event_single_crick = mouse_left_button_event_controller_SingleClick;
            mouse_left_button_event_controller.event_double_crick = mouse_left_button_event_controller_DoubleClick;
            mouse_left_button_event_controller.event_drag_start = mouse_left_button_event_controller_DragStart;
            mouse_left_button_event_controller.event_drag = mouse_left_button_event_controller_Drag;
            mouse_left_button_event_controller.event_drag_end = mouse_left_button_event_controller_DragEnd;

            main_loop_timer = new Timer();
            main_loop_timer.Interval = 30;
            main_loop_timer.Tick += Main_loop_timer_Tick;
            main_loop_timer.Start();
        }

        private void Main_loop_timer_Tick(object sender, EventArgs e)
        {
            if (is_reflesh)
            {
                is_reflesh = false;
                this.Refresh();
            }
        }

        private void _MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouse_left_button_event_controller.Update_MouseUp(e.X, e.Y);
            }

        }

        private void _MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouse_left_button_event_controller.Update_MouseDown(e.X, e.Y);
            }
        }

        private void _MouseMove(object sender, MouseEventArgs e)
        {
            mouse_left_button_event_controller.Update_MouseMove(e.X, e.Y);


            if(!drag_status.is_mouse_left_drag)
            {
                var hit_item = HitItem(e.X, e.Y);

                if (hit_item != select_item)
                {
                    select_item = hit_item;
                    this.Refresh();
                }
                if (select_item != null)
                {
                    var x = e.X;
                    var y = e.Y;
                    var margin = 16;
                    var is_w = x - select_item.view_rect.X < margin;
                    var is_e = select_item.view_rect.Width - margin < x - select_item.view_rect.X;
                    var is_n = y - select_item.view_rect.Y < margin;
                    var is_s = select_item.view_rect.Height - margin < y - select_item.view_rect.Y;

                    if (drag_status.mouse_mode == MouseMode.Normal)
                    {
                        if (is_w && is_n)
                        {
                            drag_status.drag_mode = DragMode.BoxFitResizeWN;
                            this.Cursor = Cursors.SizeNWSE;
                        }
                        else if (is_e && is_n)
                        {
                            drag_status.drag_mode = DragMode.BoxFitResizeEN;
                            this.Cursor = Cursors.SizeNESW;
                        }
                        else if (is_w && is_s)
                        {
                            drag_status.drag_mode = DragMode.BoxFitResizeWS;
                            this.Cursor = Cursors.SizeNESW;
                        }
                        else if (is_e && is_s)
                        {
                            drag_status.drag_mode = DragMode.BoxFitResizeES;
                            this.Cursor = Cursors.SizeNWSE;
                        }
                        else if (is_w)
                        {
                            drag_status.drag_mode = DragMode.BoxFitResizeW;
                            this.Cursor = Cursors.SizeWE;
                        }
                        else if (is_e)
                        {
                            drag_status.drag_mode = DragMode.BoxFitResizeE;
                            this.Cursor = Cursors.SizeWE;
                        }
                        else if (is_n)
                        {
                            drag_status.drag_mode = DragMode.BoxFitResizeN;
                            this.Cursor = Cursors.SizeNS;
                        }
                        else if (is_s)
                        {
                            drag_status.drag_mode = DragMode.BoxFitResizeS;
                            this.Cursor = Cursors.SizeNS;
                        }
                        else
                        {
                            drag_status.drag_mode = DragMode.BoxMove;
                            this.Cursor = Cursors.Default;
                        }
                    }
                    else
                    {
                        if (is_w && is_n)
                        {
                            drag_status.drag_mode = DragMode.BoxResizeWN;
                            this.Cursor = Cursors.SizeNWSE;
                        }
                        else if (is_e && is_n)
                        {
                            drag_status.drag_mode = DragMode.BoxResizeEN;
                            this.Cursor = Cursors.SizeNESW;
                        }
                        else if (is_w && is_s)
                        {
                            drag_status.drag_mode = DragMode.BoxResizeWS;
                            this.Cursor = Cursors.SizeNESW;
                        }
                        else if (is_e && is_s)
                        {
                            drag_status.drag_mode = DragMode.BoxResizeES;
                            this.Cursor = Cursors.SizeNWSE;
                        }
                        else if (is_w)
                        {
                            drag_status.drag_mode = DragMode.BoxResizeW;
                            this.Cursor = Cursors.SizeWE;
                        }
                        else if (is_e)
                        {
                            drag_status.drag_mode = DragMode.BoxResizeE;
                            this.Cursor = Cursors.SizeWE;
                        }
                        else if (is_n)
                        {
                            drag_status.drag_mode = DragMode.BoxResizeN;
                            this.Cursor = Cursors.SizeNS;
                        }
                        else if (is_s)
                        {
                            drag_status.drag_mode = DragMode.BoxResizeS;
                            this.Cursor = Cursors.SizeNS;
                        }
                        else
                        {
                            drag_status.drag_mode = DragMode.BoxInMove;
                            this.Cursor = Cursors.Default;
                        }

                    }
                }
                else
                {
                    drag_status.drag_mode = DragMode.None;
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private ImageBox HitItem( int x, int y )
        {
            var res = default(ImageBox);
            foreach (var item in items)
            {
                if (Hit.IsHit(x, y, item.view_rect))
                {
                    res = item;
                }
            }
            return res;
        }

        private void _Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 80, 80, 80)), 0, 0, this.Size.Width, this.Size.Height);

            foreach( var item in items)
            {
                if (item == select_item)
                {
                    var pen = new Pen(Color.FromArgb(255, 0, 0, 255), 1);
                    var pen2 = new Pen(Color.FromArgb(255, 0, 255, 0), 1);
                    if (drag_status.mouse_mode == MouseMode.Cut)
                    {
                        pen = new Pen(Color.FromArgb(255, 255, 0, 0), 1);
                    }

                    if (drag_status.is_mouse_left_drag)
                    {
                        g.SetClip(drag_status.view_now_rect.ToRectangle());
                        g.DrawImage(item.image, drag_status.src_now_rect.ToRectangle());
                        g.DrawRectangle(pen, new Rectangle((int)drag_status.view_now_rect.X, (int)drag_status.view_now_rect.Y, (int)drag_status.view_now_rect.Width - 1, (int)drag_status.view_now_rect.Height - 1));
                        g.SetClip(drag_status.src_now_rect.ToRectangle());
                        g.DrawRectangle(pen2, new Rectangle((int)drag_status.src_now_rect.X, (int)drag_status.src_now_rect.Y, (int)drag_status.src_now_rect.Width - 1, (int)drag_status.src_now_rect.Height - 1));

                    }
                    else
                    {
                        g.SetClip(item.view_rect.ToRectangle());
                        g.DrawImage(item.image, item.src_rect.ToRectangle());
                        g.DrawRectangle(pen, new Rectangle((int)item.view_rect.X, (int)item.view_rect.Y, (int)item.view_rect.Width - 1, (int)item.view_rect.Height - 1));

                        //var rect = new Rectangle(item.view_rect.X, item.view_rect.Y, item.view_rect.Width, item.view_rect.Height);
                        //g.SetClip(rect);
                        ////item.rimage.Draw(g, item.view_rect.X, item.view_rect.Y);
                        //g.DrawImage(item.image, item.view_rect.X , item.view_rect.Y , item.src_rect.Width, item.src_rect.Height);

                        //g.DrawRectangle(pen, new Rectangle(rect.X,rect.Y,rect.Width-1,rect.Height-1));
                    }

                }
                else
                {
                    g.SetClip(item.view_rect.ToRectangle());
                    g.DrawImage(item.image, item.src_rect.ToRectangle());
                    //var rect = new Rectangle(item.view_rect.X, item.view_rect.Y, item.view_rect.Width, item.view_rect.Height);
                    //g.SetClip(rect);
                    ////item.rimage.Draw(g, item.view_rect.X, item.view_rect.Y);
                    //g.DrawImage(item.image, item.view_rect.X, item.view_rect.Y, item.src_rect.Width, item.src_rect.Height);
                }
            }
        }

        public void _MouseWheel( int wheel_move_value )
        {
            if (select_item != null && !drag_status.is_mouse_left_drag)
            {
                if (drag_status.mouse_mode == MouseMode.Cut)
                {
                    var scale = 1.0;
                    if(wheel_move_value > 0 )
                    {
                        scale *= 1.1;
                    }
                    else
                    {
                        scale /= 1.1;
                    }

                    //var view_w = drag_status.view_start_rect.Width + mx;
                    //var scale = (double)(view_w) / (double)drag_status.view_start_rect.Width;
                    var ancer_point = new PointD(
                            select_item.view_rect.X + select_item.view_rect.Width / 2,
                            select_item.view_rect.Y + select_item.view_rect.Height / 2
                        );
                    //select_item.view_rect = select_item.view_rect.Scale(ancer_point, scale);
                    select_item.src_rect = select_item.src_rect.Scale(ancer_point, scale);
                    is_reflesh = true;
                }
                else if (drag_status.mouse_mode == MouseMode.Normal)
                {
                    var scale = 1.0;
                    if (wheel_move_value > 0)
                    {
                        scale *= 1.1;
                    }
                    else
                    {
                        scale /= 1.1;
                    }

                    //var view_w = drag_status.view_start_rect.Width + mx;
                    //var scale = (double)(view_w) / (double)drag_status.view_start_rect.Width;
                    var ancer_point = new PointD(
                            select_item.view_rect.X + select_item.view_rect.Width / 2,
                            select_item.view_rect.Y + select_item.view_rect.Height / 2
                        );
                    select_item.view_rect = select_item.view_rect.Scale(ancer_point, scale);
                    select_item.src_rect = select_item.src_rect.Scale(ancer_point, scale);
                    is_reflesh = true;
                }
            }
        }

        public void Add( ImageLibrary.ImageData image_data )
        {
            var image_box = new ImageBox(image_data);
            var wh = image_data.size.Width * image_data.size.Height;
            var wh_default = 400 * 400;
            var wh_scale = (double)wh_default / (double)wh;
            var x = 0;
            var y = 0;
            image_box.view_rect = new RectangleD(x, y, (int)(image_data.size.Width * wh_scale), (int)(image_data.size.Height * wh_scale));
            image_box.src_rect  = new RectangleD(x, y, (int)(image_data.size.Width * wh_scale), (int)(image_data.size.Height * wh_scale));


            image_box.image = Image.FromFile(image_data.file_path);
            //image_box.rimage = ImageManager.ReserveRImage(image_data.file_path,
            //    image_box.view_rect.Width, image_box.view_rect.Height, wh_scale, ImageManager.RImage.ReserveSizeType.Fix, "");

            //image_box.rimage.CreatedImage = (rimage) =>
            //{
            //    if (rimage == image_box.rimage)
            //    {
            //        AppCore.core.picup_view.is_reflesh = true;
            //    }
            //};

            items.Add(image_box);

            is_reflesh = true;
        }

        private void mouse_left_button_event_controller_SingleClick(MouseLeftButtonEventController ec)
        {
            if ( drag_status.mouse_mode == MouseMode.Normal)
            {
                drag_status.mouse_mode = MouseMode.Cut;
                is_reflesh = true;
            }
            else if (drag_status.mouse_mode == MouseMode.Cut)
            {
                drag_status.mouse_mode = MouseMode.Normal;
                is_reflesh = true;
            }
        }
        private void mouse_left_button_event_controller_DoubleClick(MouseLeftButtonEventController ec)
        {
        }
        private void mouse_left_button_event_controller_DragStart(MouseLeftButtonEventController ec)
        {
            if (select_item == null) return;
            drag_status.is_mouse_left_drag = true;
            drag_status.view_start_rect = new RectangleD( select_item.view_rect);
            drag_status.view_now_rect   = new RectangleD( select_item.view_rect );
            drag_status.src_start_rect = new RectangleD( select_item.src_rect );
            drag_status.src_now_rect   = new RectangleD( select_item.src_rect );

            switch (drag_status.drag_mode)
            {
                case DragMode.BoxMove:
                    break;
                //
                case DragMode.BoxResizeE:
                    break;
                case DragMode.BoxResizeW:
                    break;
                case DragMode.BoxResizeS:
                    break;
                case DragMode.BoxResizeN:
                    break;
                //
                case DragMode.BoxFitResizeE:
                    break;
            }
            is_reflesh = true;
        }
        private void mouse_left_button_event_controller_Drag(MouseLeftButtonEventController ec)
        {
            var mx = ec.drag_now_point.X - ec.drag_start_point.X;
            var my = ec.drag_now_point.Y - ec.drag_start_point.Y;
            switch (drag_status.drag_mode)
            {
                case DragMode.BoxMove:
                    drag_status.view_now_rect.X = drag_status.view_start_rect.X + mx;
                    drag_status.view_now_rect.Y = drag_status.view_start_rect.Y + my;
                    drag_status.src_now_rect.X = drag_status.src_start_rect.X + mx;
                    drag_status.src_now_rect.Y = drag_status.src_start_rect.Y + my;
                    break;
                case DragMode.BoxInMove:
                    drag_status.view_now_rect.X = drag_status.view_start_rect.X;
                    drag_status.view_now_rect.Y = drag_status.view_start_rect.Y;
                    {
                        var src_x = drag_status.src_start_rect.X + mx;
                        var src_y = drag_status.src_start_rect.Y + my;
                        if (src_x < drag_status.view_start_rect.X - drag_status.src_start_rect.Width + drag_status.view_now_rect.Width)
                        {
                            src_x = drag_status.view_start_rect.X - drag_status.src_start_rect.Width + drag_status.view_now_rect.Width;
                        }
                        else if ( src_x > drag_status.view_start_rect.X )
                        {
                            src_x = drag_status.view_start_rect.X;
                        }
                        if (src_y < drag_status.view_start_rect.Y - drag_status.src_start_rect.Height + drag_status.view_now_rect.Height)
                        {
                            src_y = drag_status.view_start_rect.Y - drag_status.src_start_rect.Height + drag_status.view_now_rect.Height;
                        }
                        else if (src_y > drag_status.view_start_rect.Y)
                        {
                            src_y = drag_status.view_start_rect.Y;
                        }
                        drag_status.src_now_rect.X = src_x;
                        drag_status.src_now_rect.Y = src_y;
                    }
                    break;
                //
                case DragMode.BoxResizeE:
                    drag_status.view_now_rect.Width = drag_status.view_start_rect.Width + mx;
                    if (drag_status.view_now_rect.Width > select_item.src_rect.Width) drag_status.view_now_rect.Width = select_item.src_rect.Width;
                    //select_item.view_rect.Width = drag_status.view_now_rect.Width;
                    break;
                case DragMode.BoxResizeW:
                    drag_status.view_now_rect.X = drag_status.view_start_rect.X + mx;
                    drag_status.view_now_rect.Width = drag_status.view_start_rect.Width - mx;
                    if (drag_status.view_now_rect.Width > drag_status.src_now_rect.Width)
                    {
                        var over_x = drag_status.view_now_rect.Width - drag_status.src_now_rect.Width;
                        drag_status.view_now_rect.Width = drag_status.src_now_rect.Width;
                        drag_status.view_now_rect.X = drag_status.view_start_rect.X + mx + over_x;
                    }
                    break;
                case DragMode.BoxResizeS:
                    drag_status.view_now_rect.Height = drag_status.view_start_rect.Height + my;
                    if (drag_status.view_now_rect.Height > select_item.src_rect.Height) drag_status.view_now_rect.Height = select_item.src_rect.Height;
                    break;
                case DragMode.BoxResizeN:
                    drag_status.view_now_rect.Y = drag_status.view_start_rect.Y + my;
                    if (drag_status.view_now_rect.Y < drag_status.src_now_rect.Y)
                    {
                        drag_status.view_now_rect.Y = drag_status.src_now_rect.Y;
                    }
                    drag_status.view_now_rect.Height = drag_status.view_start_rect.Height - (drag_status.view_now_rect.Y - drag_status.view_start_rect.Y);

                    break;
                case DragMode.BoxResizeES:
                    drag_status.view_now_rect.Width = drag_status.view_start_rect.Width + mx;
                    if (drag_status.view_now_rect.Width > select_item.src_rect.Width) drag_status.view_now_rect.Width = select_item.src_rect.Width;

                    drag_status.view_now_rect.Height = drag_status.view_start_rect.Height + my;
                    if (drag_status.view_now_rect.Height > select_item.src_rect.Height) drag_status.view_now_rect.Height = select_item.src_rect.Height;
                    break;
                case DragMode.BoxResizeWS:
                    drag_status.view_now_rect.X = drag_status.view_start_rect.X + mx;
                    drag_status.view_now_rect.Width = drag_status.view_start_rect.Width - mx;
                    if (drag_status.view_now_rect.Width > drag_status.src_now_rect.Width)
                    {
                        var over_x = drag_status.view_now_rect.Width - drag_status.src_now_rect.Width;
                        drag_status.view_now_rect.Width = drag_status.src_now_rect.Width;
                        drag_status.view_now_rect.X = drag_status.view_start_rect.X + mx + over_x;
                    }

                    drag_status.view_now_rect.Height = drag_status.view_start_rect.Height + my;
                    if (drag_status.view_now_rect.Height > select_item.src_rect.Height) drag_status.view_now_rect.Height = select_item.src_rect.Height;
                    break;
                case DragMode.BoxResizeEN:
                    drag_status.view_now_rect.Width = drag_status.view_start_rect.Width + mx;
                    if (drag_status.view_now_rect.Width > select_item.src_rect.Width) drag_status.view_now_rect.Width = select_item.src_rect.Width;

                    drag_status.view_now_rect.Y = drag_status.view_start_rect.Y + my;
                    if (drag_status.view_now_rect.Y < drag_status.src_now_rect.Y)
                    {
                        drag_status.view_now_rect.Y = drag_status.src_now_rect.Y;
                    }
                    drag_status.view_now_rect.Height = drag_status.view_start_rect.Height - (drag_status.view_now_rect.Y - drag_status.view_start_rect.Y);
                    break;
                case DragMode.BoxResizeWN:
                    drag_status.view_now_rect.X = drag_status.view_start_rect.X + mx;
                    drag_status.view_now_rect.Width = drag_status.view_start_rect.Width - mx;
                    if (drag_status.view_now_rect.Width > drag_status.src_now_rect.Width)
                    {
                        var over_x = drag_status.view_now_rect.Width - drag_status.src_now_rect.Width;
                        drag_status.view_now_rect.Width = drag_status.src_now_rect.Width;
                        drag_status.view_now_rect.X = drag_status.view_start_rect.X + mx + over_x;
                    }

                    drag_status.view_now_rect.Y = drag_status.view_start_rect.Y + my;
                    if (drag_status.view_now_rect.Y < drag_status.src_now_rect.Y)
                    {
                        drag_status.view_now_rect.Y = drag_status.src_now_rect.Y;
                    }
                    drag_status.view_now_rect.Height = drag_status.view_start_rect.Height - (drag_status.view_now_rect.Y - drag_status.view_start_rect.Y);
                    break;
                //
                case DragMode.BoxFitResizeE:
                    {
                        var view_w = drag_status.view_start_rect.Width + mx;
                        var scale = (double)(view_w) / (double)drag_status.view_start_rect.Width;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X,
                                drag_status.view_start_rect.Height / 2 + drag_status.view_start_rect.Y
                            );
                        drag_status.view_now_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        drag_status.src_now_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
                case DragMode.BoxFitResizeW:
                    {
                        var view_w = drag_status.view_start_rect.Width - mx;
                        var scale = (double)(view_w) / (double)drag_status.view_start_rect.Width;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X + drag_status.view_start_rect.Width,
                                drag_status.view_start_rect.Height / 2 + drag_status.view_start_rect.Y
                            );
                        drag_status.view_now_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        drag_status.src_now_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
                case DragMode.BoxFitResizeN:
                    {
                        var view_h = drag_status.view_start_rect.Height - my;
                        var scale = (double)(view_h) / (double)drag_status.view_start_rect.Height;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X + drag_status.view_start_rect.Width / 2,
                                drag_status.view_start_rect.Y + drag_status.view_start_rect.Height
                            );
                        drag_status.view_now_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        drag_status.src_now_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
                case DragMode.BoxFitResizeS:
                    {
                        var view_h = drag_status.view_start_rect.Height + my;
                        var scale = (double)(view_h) / (double)drag_status.view_start_rect.Height;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X + drag_status.view_start_rect.Width / 2,
                                drag_status.view_start_rect.Y
                            );
                        drag_status.view_now_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        drag_status.src_now_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
                case DragMode.BoxFitResizeEN:
                    {
                        var view_w = drag_status.view_start_rect.Width + (mx - my) / 2;
                        var scale = (double)(view_w) / (double)drag_status.view_start_rect.Width;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X,
                                drag_status.view_start_rect.Y + drag_status.view_start_rect.Height
                            );
                        drag_status.view_now_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        drag_status.src_now_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
                case DragMode.BoxFitResizeWN:
                    {
                        var view_w = drag_status.view_start_rect.Width - (mx + my) / 2;
                        var scale = (double)(view_w) / (double)drag_status.view_start_rect.Width;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X + drag_status.view_start_rect.Width,
                                drag_status.view_start_rect.Y + drag_status.view_start_rect.Height
                            );
                        drag_status.view_now_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        drag_status.src_now_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
                case DragMode.BoxFitResizeES:
                    {
                        var view_w = drag_status.view_start_rect.Width + (mx + my) / 2;
                        var scale = (double)(view_w) / (double)drag_status.view_start_rect.Width;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X,
                                drag_status.view_start_rect.Y
                            );
                        drag_status.view_now_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        drag_status.src_now_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
                case DragMode.BoxFitResizeWS:
                    {
                        var view_w = drag_status.view_start_rect.Width - (mx - my) / 2;
                        var scale = (double)(view_w) / (double)drag_status.view_start_rect.Width;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X + drag_status.view_start_rect.Width,
                                drag_status.view_start_rect.Y
                            );
                        drag_status.view_now_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        drag_status.src_now_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
            }
            is_reflesh = true;
        }
        private void mouse_left_button_event_controller_DragEnd(MouseLeftButtonEventController ec)
        {
            var mx = ec.drag_now_point.X - ec.drag_start_point.X;
            var my = ec.drag_now_point.Y - ec.drag_start_point.Y;
            drag_status.is_mouse_left_drag = false;
            switch(drag_status.drag_mode)
            {
                case DragMode.BoxMove:
                    select_item.view_rect.X = drag_status.view_start_rect.X + mx;
                    select_item.view_rect.Y = drag_status.view_start_rect.Y + my;
                    select_item.src_rect.X = drag_status.src_start_rect.X + mx;
                    select_item.src_rect.Y = drag_status.src_start_rect.Y + my;
                    break;
                case DragMode.BoxInMove:
                    drag_status.view_now_rect.X = drag_status.view_start_rect.X;
                    drag_status.view_now_rect.Y = drag_status.view_start_rect.Y;
                    {
                        var src_x = drag_status.src_start_rect.X + mx;
                        var src_y = drag_status.src_start_rect.Y + my;
                        if (src_x < drag_status.view_start_rect.X - drag_status.src_start_rect.Width + drag_status.view_now_rect.Width)
                        {
                            src_x = drag_status.view_start_rect.X - drag_status.src_start_rect.Width + drag_status.view_now_rect.Width;
                        }
                        else if (src_x > drag_status.view_start_rect.X)
                        {
                            src_x = drag_status.view_start_rect.X;
                        }
                        if (src_y < drag_status.view_start_rect.Y - drag_status.src_start_rect.Height + drag_status.view_now_rect.Height)
                        {
                            src_y = drag_status.view_start_rect.Y - drag_status.src_start_rect.Height + drag_status.view_now_rect.Height;
                        }
                        else if (src_y > drag_status.view_start_rect.Y)
                        {
                            src_y = drag_status.view_start_rect.Y;
                        }
                        select_item.src_rect.X = src_x;
                        select_item.src_rect.Y = src_y;
                    }
                    break;
                case DragMode.BoxResizeE:
                    select_item.view_rect.Width = drag_status.view_start_rect.Width + mx;
                    if (select_item.view_rect.Width > select_item.src_rect.Width) select_item.view_rect.Width = select_item.src_rect.Width;
                    break;
                case DragMode.BoxResizeW:
                    select_item.view_rect.X = drag_status.view_start_rect.X + mx;
                    select_item.view_rect.Width = drag_status.view_start_rect.Width - mx;
                    if (select_item.view_rect.Width > drag_status.src_now_rect.Width)
                    {
                        var over_x = select_item.view_rect.Width - drag_status.src_now_rect.Width;
                        select_item.view_rect.Width = drag_status.src_now_rect.Width;
                        select_item.view_rect.X     = drag_status.view_start_rect.X + mx + over_x;
                    }
                    break;
                case DragMode.BoxResizeS:
                    select_item.view_rect.Height = drag_status.view_start_rect.Height + my;
                    if (select_item.view_rect.Height > select_item.src_rect.Height) select_item.view_rect.Height = select_item.src_rect.Height;

                    break;
                case DragMode.BoxResizeN:
                    select_item.view_rect.Y = drag_status.view_start_rect.Y + my;
                    if (select_item.view_rect.Y < drag_status.src_now_rect.Y)
                    {
                        select_item.view_rect.Y = drag_status.src_now_rect.Y;
                    }
                    select_item.view_rect.Height = drag_status.view_start_rect.Height - (select_item.view_rect.Y - drag_status.view_start_rect.Y);
                    break;
                case DragMode.BoxResizeES:
                    select_item.view_rect.Width = drag_status.view_start_rect.Width + mx;
                    if (select_item.view_rect.Width > select_item.src_rect.Width) select_item.view_rect.Width = select_item.src_rect.Width;

                    select_item.view_rect.Height = drag_status.view_start_rect.Height + my;
                    if (select_item.view_rect.Height > select_item.src_rect.Height) select_item.view_rect.Height = select_item.src_rect.Height;
                    break;
                case DragMode.BoxResizeWS:
                    select_item.view_rect.X = drag_status.view_start_rect.X + mx;
                    select_item.view_rect.Width = drag_status.view_start_rect.Width - mx;
                    if (select_item.view_rect.Width > drag_status.src_now_rect.Width)
                    {
                        var over_x = select_item.view_rect.Width - drag_status.src_now_rect.Width;
                        select_item.view_rect.Width = drag_status.src_now_rect.Width;
                        select_item.view_rect.X = drag_status.view_start_rect.X + mx + over_x;
                    }

                    select_item.view_rect.Height = drag_status.view_start_rect.Height + my;
                    if (select_item.view_rect.Height > select_item.src_rect.Height) select_item.view_rect.Height = select_item.src_rect.Height;
                    break;
                case DragMode.BoxResizeEN:
                    select_item.view_rect.Width = drag_status.view_start_rect.Width + mx;
                    if (select_item.view_rect.Width > select_item.src_rect.Width) select_item.view_rect.Width = select_item.src_rect.Width;

                    select_item.view_rect.Y = drag_status.view_start_rect.Y + my;
                    if (select_item.view_rect.Y < drag_status.src_now_rect.Y)
                    {
                        select_item.view_rect.Y = drag_status.src_now_rect.Y;
                    }
                    select_item.view_rect.Height = drag_status.view_start_rect.Height - (select_item.view_rect.Y - drag_status.view_start_rect.Y);
                    break;
                case DragMode.BoxResizeWN:
                    select_item.view_rect.X = drag_status.view_start_rect.X + mx;
                    select_item.view_rect.Width = drag_status.view_start_rect.Width - mx;
                    if (select_item.view_rect.Width > drag_status.src_now_rect.Width)
                    {
                        var over_x = select_item.view_rect.Width - drag_status.src_now_rect.Width;
                        select_item.view_rect.Width = drag_status.src_now_rect.Width;
                        select_item.view_rect.X = drag_status.view_start_rect.X + mx + over_x;
                    }

                    select_item.view_rect.Y = drag_status.view_start_rect.Y + my;
                    if (select_item.view_rect.Y < drag_status.src_now_rect.Y)
                    {
                        select_item.view_rect.Y = drag_status.src_now_rect.Y;
                    }
                    select_item.view_rect.Height = drag_status.view_start_rect.Height - (select_item.view_rect.Y - drag_status.view_start_rect.Y);
                    break;
                //
                case DragMode.BoxFitResizeE:

                    {
                        var view_w = drag_status.view_start_rect.Width + mx;
                        var scale = (double)(view_w) / (double)drag_status.view_start_rect.Width;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X,
                                drag_status.view_start_rect.Height / 2 + drag_status.view_start_rect.Y
                            );
                        select_item.view_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        select_item.src_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
                case DragMode.BoxFitResizeW:
                    {
                        var view_w = drag_status.view_start_rect.Width - mx;
                        var scale = (double)(view_w) / (double)drag_status.view_start_rect.Width;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X + drag_status.view_start_rect.Width,
                                drag_status.view_start_rect.Height / 2 + drag_status.view_start_rect.Y
                            );
                        select_item.view_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        select_item.src_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
                case DragMode.BoxFitResizeN:
                    {
                        var view_h = drag_status.view_start_rect.Height - my;
                        var scale = (double)(view_h) / (double)drag_status.view_start_rect.Height;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X + drag_status.view_start_rect.Width / 2,
                                drag_status.view_start_rect.Y + drag_status.view_start_rect.Height
                            );
                        select_item.view_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        select_item.src_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
                case DragMode.BoxFitResizeS:
                    {
                        var view_h = drag_status.view_start_rect.Height + my;
                        var scale = (double)(view_h) / (double)drag_status.view_start_rect.Height;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X + drag_status.view_start_rect.Width / 2,
                                drag_status.view_start_rect.Y
                            );
                        select_item.view_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        select_item.src_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
                case DragMode.BoxFitResizeEN:
                    {
                        var view_w = drag_status.view_start_rect.Width + (mx - my) / 2;
                        var scale = (double)(view_w) / (double)drag_status.view_start_rect.Width;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X,
                                drag_status.view_start_rect.Y + drag_status.view_start_rect.Height
                            );
                        select_item.view_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        select_item.src_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
                case DragMode.BoxFitResizeWN:
                    {
                        var view_w = drag_status.view_start_rect.Width - (mx + my) / 2;
                        var scale = (double)(view_w) / (double)drag_status.view_start_rect.Width;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X + drag_status.view_start_rect.Width,
                                drag_status.view_start_rect.Y + drag_status.view_start_rect.Height
                            );
                        select_item.view_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        select_item.src_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
                case DragMode.BoxFitResizeES:
                    {
                        var view_w = drag_status.view_start_rect.Width + (mx + my) / 2;
                        var scale = (double)(view_w) / (double)drag_status.view_start_rect.Width;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X,
                                drag_status.view_start_rect.Y
                            );
                        select_item.view_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        select_item.src_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
                case DragMode.BoxFitResizeWS:
                    {
                        var view_w = drag_status.view_start_rect.Width - (mx-my) / 2;
                        var scale = (double)(view_w) / (double)drag_status.view_start_rect.Width;
                        var ancer_point = new PointD(
                                drag_status.view_start_rect.X + drag_status.view_start_rect.Width,
                                drag_status.view_start_rect.Y
                            );
                        select_item.view_rect = drag_status.view_start_rect.Scale(ancer_point, scale);
                        select_item.src_rect = drag_status.src_start_rect.Scale(ancer_point, scale);
                    }
                    break;
            }
            is_reflesh = true;
        }

    }
}
