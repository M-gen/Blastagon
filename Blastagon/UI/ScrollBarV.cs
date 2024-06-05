using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using Blastagon.Common;

namespace Blastagon.UI
{
    public class ScrollBarV : PictureBox
    {
        Rectangle button_up = new Rectangle(0, 0, 24, 30);
        Rectangle button_down = new Rectangle(0, 0, 24, 30);
        Rectangle button_bar = new Rectangle(0, 0, 24, 30);
        Rectangle button_bar_area = new Rectangle(0,0,0,0);
        Int64 scroll_value = 0;
        Int64 scroll_value_max = 0;
        Int64 button_scroll_add = 0;

        double scroll_view_scale = 0;
        Int64 scroll_view_pos = 0;
        Int64 scroll_view_h = 1;

        Timer mouse_down_timer = new Timer();

        Bitmap bmp_base;
        Bitmap bmp_scroll_button;

        bool is_mouse_down_bar = false;

        public Action<Int64> Scroll;
        public Action<Graphics,int,int,int,int> PaintEx; // g,ox,oy,w,h

        enum DownButton
        {
            Down,
            Up,
            None
        }
        DownButton down_button = DownButton.None;

        public ScrollBarV(System.Windows.Forms.Control form)
        {
            form.Controls.Add(this);
            this.Location = new Point(1010 + 10 + 2, 0);
            this.Size = new Size(24, form.Size.Height);

            //RefleshBody();

            mouse_down_timer.Interval = 50;
            mouse_down_timer.Tick += Mouse_down_timer_Tick;

            SetScrollMax(1000);
            button_scroll_add = 200;
            scroll_view_h = form.Size.Height - 30 * 3;

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            //this.BackColor = Color.FromArgb(0, 0, 0, 0);
            this.BackColor = Color.Transparent;


            this.Paint += _Paint;
            this.MouseDown += _MouseDown;
            this.MouseUp += _MouseUp;
            this.MouseMove += _MouseMove;
            this.Resize += _Resize;
            this.Refresh();

            button_bar.Y = (int)(scroll_view_pos + 30);
            button_bar_area.Width = Size.Width;
            button_bar_area.Height = (int)(scroll_view_h-30);
            button_bar_area.Y = 30;
            button_bar_area.X = 0;
        }

        private void _Resize(object sender, EventArgs e)
        {
            SetScrollMax(scroll_value_max);
        }

        private void Mouse_down_timer_Tick(object sender, EventArgs e)
        {
            switch (down_button)
            {
                case DownButton.Down:
                    AddScrollValue(button_scroll_add);
                    break;
                case DownButton.Up:
                    AddScrollValue(-button_scroll_add);
                    break;
            }
        }

        public void AddScrollValue(Int64 add_value, bool is_acion = true)
        {
            scroll_value += add_value;
            SetScrollValue(scroll_value + add_value, is_acion);
        }

        public void SetScrollValue(Int64 value, bool is_acion)
        {
            if (is_acion && Scroll != null) Scroll(scroll_value);

            scroll_value = value;

            if (scroll_value < 0)
            {
                scroll_value = 0;
            }
            if (scroll_value > scroll_value_max)
            {
                scroll_value = scroll_value_max;
            }

            if (is_acion && Scroll != null) Scroll(scroll_value);

            scroll_view_pos = (Int64)(scroll_value / scroll_view_scale);

            button_bar.Y = (int)( scroll_view_pos + 30);

            this.Refresh();
        }

        public void SetScrollMax(Int64 max)
        {
            var old_scroll_view_h = scroll_view_h;

            scroll_view_h = Size.Height - button_up.Size.Height - button_down.Size.Height - button_bar.Height;
            button_bar_area.Height = (int)(scroll_view_h);
            scroll_value_max = max;

            //SetScrollValue(scroll_value, false);
            SetScrollValue(scroll_value, true); // todo : これが必要、やや場当たり的な対応
            //if ( Scroll != null) Scroll(scroll_value); // こっちだけだと、スクロールバーの位置が、最大値変更に合わせた位置に反映されない

            //var pos = (double)scroll_view_h / (double)old_scroll_view_h;
            //scroll_view_pos = (Int64)((double)scroll_view_pos * pos);

            //if (scroll_value_max > scroll_view_h)
            //{
            scroll_view_scale = (double)scroll_value_max / (double)scroll_view_h;
                Refresh();
            //}
            //else
            //{
            //    scroll_view_scale = 1;
            //}
        }

        private void _MouseMove(object sender, MouseEventArgs e)
        {
            if (is_mouse_down_bar)
            {
                var y = e.Y - button_up.Size.Height - button_bar.Height / 2;
                SetScrollValue((Int64)(y * scroll_view_scale), true);
            }
        }

        private void _MouseUp(object sender, MouseEventArgs e)
        {
            is_mouse_down_bar = false;

            mouse_down_timer.Stop();
            down_button = DownButton.None;
        }

        private void _MouseDown(object sender, MouseEventArgs e)
        {
            {
                var rc = button_up;
                if ((rc.X <= e.X) && (e.X < (rc.X + rc.Width)) &&
                     (rc.Y <= e.Y) && (e.Y < (rc.Y + rc.Height)))
                {
                    AddScrollValue(-button_scroll_add);
                    mouse_down_timer.Start();
                    down_button = DownButton.Up;
                }
            }

            {
                var rc = button_down;
                if ((rc.X <= e.X) && (e.X < (rc.X + rc.Width)) &&
                     (rc.Y <= e.Y) && (e.Y < (rc.Y + rc.Height)))
                {
                    AddScrollValue(button_scroll_add);
                    mouse_down_timer.Start();
                    down_button = DownButton.Down;
                }
            }

            {
                var rc = new Rectangle(button_bar_area.X, button_bar_area.Y, button_bar_area.Width, button_bar_area.Height+ button_bar.Height);
                if ((rc.X <= e.X) && (e.X < (rc.X + rc.Width)) &&
                     (rc.Y <= e.Y) && (e.Y < (rc.Y + rc.Height)))
                {
                    is_mouse_down_bar = true;
                    var y = e.Y - button_up.Size.Height- button_bar.Height / 2;
                    SetScrollValue( (Int64)(y * scroll_view_scale), true );
                }
            }
        }


        public void RefleshBody()
        {
            bmp_base = new Bitmap(24, this.Size.Height);

            using (var g = Graphics.FromImage(bmp_base))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; ;
                
                // 全体スクロールバー
                {
                    var w = Size.Width;
                    var h = Size.Height;
                    var ox = 0;
                    var oy = 0;
                    UserInterfaceCommon.DrawPattarnRect(g, ox, oy, w, h, 2);
                }

                // 上ボタン
                {
                    var w = button_up.Width;
                    var h = button_up.Height;
                    var ox = button_up.X;
                    var oy = button_up.Y;
                    UserInterfaceCommon.DrawButtonOff(g, ox, oy, w, h, false, null);
                    UserInterfaceCommon.DrawDelta(g, ox + 5, oy + 8, 12, 14, false);
                }

                // 下ボタン
                {
                    button_down.X = 0;
                    button_down.Y = Size.Height - button_down.Height;


                    var w = button_down.Width;
                    var h = button_down.Height;
                    var ox = button_down.X;
                    var oy = button_down.Y;
                    UserInterfaceCommon.DrawButtonOff(g, ox, oy, w, h, false, null);
                    UserInterfaceCommon.DrawDelta(g, ox + 5, oy + 8, 12, 14, true);
                }

            }


            bmp_scroll_button = new Bitmap(24, 30);
            using (var g = Graphics.FromImage(bmp_scroll_button))
            {
                var w = 24;
                //var ox = 45;
                //var oy = 0;
                UserInterfaceCommon.DrawButtonOff(g, 0, 0, w, 30, false, null);
            }
        }

        private void _Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.DrawImage(bmp_base, 0, 0);

            if (PaintEx != null) PaintEx(g, 0, button_up.Height, Size.Width, Size.Height - button_up.Height - button_down.Height);

            g.DrawImage(bmp_scroll_button, 0, button_bar.Y); // 全体スクロールバーのバーの部分（動くので別枠

        }

        public void SetScrollAddValue(Int64 add_value)
        {
            button_scroll_add = add_value;
        }

        public Int64 GetScrollMax()
        {
            return scroll_value_max;
        }
    }
}
