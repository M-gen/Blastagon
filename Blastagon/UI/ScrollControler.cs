using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using Blastagon.Common;
using Blastagon.App;

namespace Blastagon.UI
{
    public class ScrollControler : PictureBox
    {
        Rectangle button_up   = new Rectangle(0, 0, 45, 60);
        Rectangle button_down = new Rectangle(0, 60, 45, 60);

        Int64 scroll_value = 0;
        Int64 scroll_value_max = 0;
        Int64 button_scroll_add = 0;

        Int64 scroll_view_scale = 0;
        Int64 scroll_view_pos   = 0;
        Int64 scroll_view_h     = 120-10; // ボタンの大きさ分マイナス

        Timer mouse_down_timer = new Timer();

        Bitmap bmp_base;
        Bitmap bmp_scroll_button;

        public Action<Int64> Scroll; 

        enum DownButton
        {
            Down,
            Up,
            None
        }
        DownButton down_button = DownButton.None;

        public ScrollControler(System.Windows.Forms.Control form )
        {
            form.Controls.Add(this);
            this.Location = new Point(1010+10+2, 4);
            this.Size = new Size(45+32, 120);

            InitBitmaps();

            mouse_down_timer.Interval = 50;
            mouse_down_timer.Tick += Mouse_down_timer_Tick;

            SetScrollMax(1000);
            button_scroll_add = scroll_view_scale;

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            //this.BackColor = Color.FromArgb(0, 0, 0, 0);
            this.BackColor = Color.Transparent;
            

            this.Paint += _Paint;
            this.MouseDown += _MouseDown;
            this.MouseUp   += _MouseUp;
            this.MouseMove += _MouseMove;
            this.Refresh();
        }

        private void Mouse_down_timer_Tick(object sender, EventArgs e)
        {
            switch(down_button)
            {
                case DownButton.Down:
                    AddScrollValue(scroll_view_scale);
                    break;
                case DownButton.Up:
                    AddScrollValue(-scroll_view_scale);
                    break;
            }
        }

        public void AddScrollValue(Int64 add_value, bool is_acion = true)
        {
            scroll_value += add_value;
            SetScrollValue(scroll_value + add_value, is_acion);
        }

        public void SetScrollValue(Int64 value, bool is_acion )
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

            scroll_view_pos = scroll_value / scroll_view_scale;


            this.Refresh();
        }

        public void SetScrollMax(Int64 max)
        {
            scroll_value_max = max;

            if (scroll_value_max > scroll_view_h)
            {
                scroll_view_scale = scroll_value_max / scroll_view_h;
            }
            else
            {
                scroll_view_scale = 1;
            }
        }

        private void _MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void _MouseUp(object sender, MouseEventArgs e)
        {
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
                    AddScrollValue(-scroll_view_scale);
                    mouse_down_timer.Start();
                    down_button = DownButton.Up;
                }
            }

            {
                var rc = button_down;
                if ((rc.X <= e.X) && (e.X < (rc.X + rc.Width)) &&
                     (rc.Y <= e.Y) && (e.Y < (rc.Y + rc.Height)))
                {
                    AddScrollValue(scroll_view_scale);
                    mouse_down_timer.Start();
                    down_button = DownButton.Down;
                }
            }
        }


        private void InitBitmaps()
        {
            bmp_base = new Bitmap(45 + 32, 120);
            using (var g = Graphics.FromImage(bmp_base))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; ;

                // 上ボタン
                {
                    var w = button_up.Width;
                    var h = button_up.Height;
                    var ox = button_up.X;
                    var oy = button_up.Y;
                    UserInterfaceCommon.DrawButtonOff(g, ox, oy, w, h, false, null);
                    UserInterfaceCommon.DrawDelta(g, ox + 10, oy + 17, 24, 24, false);
                }

                // 下ボタン
                {
                    var w = button_down.Width;
                    var h = button_down.Height;
                    var ox = button_down.X;
                    var oy = button_down.Y;
                    UserInterfaceCommon.DrawButtonOff(g, ox, oy, w, h, false, null);
                    UserInterfaceCommon.DrawDelta(g, ox + 10, oy + 17, 24, 24, true);
                }

                // 全体スクロールバー
                {
                    var w = 32;
                    var h = 120;
                    var ox = 45;
                    var oy = 0;
                    UserInterfaceCommon.DrawPattarnRect(g, ox, oy, w, h, 2);
                }
            }

            bmp_scroll_button = new Bitmap(32, 10);
            using (var g = Graphics.FromImage(bmp_scroll_button))
            {
                var w = 32;
                //var ox = 45;
                //var oy = 0;
                UserInterfaceCommon.DrawButtonOff(g, 0, 0, w, 10, false, null);
            }
        }

        private void _Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.DrawImage(bmp_base, 0, 0);
            g.DrawImage(bmp_scroll_button, 45, scroll_view_pos); // 全体スクロールバーのバーの部分（動くので別枠

        }

        public void SetScrollAddValue(Int64 add_value)
        {
            button_scroll_add = add_value;
        }
    }
}
