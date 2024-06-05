using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using Blastagon.Common;
using Blastagon.App;
using Blastagon.UI;
using Blastagon.UI.Common;

namespace Blastagon.UI.Common
{
    public class ProgressPanel : PictureBox
    {
        System.Windows.Forms.Control parent;

        public int value_max = 1;
        public int value = 0;
        System.Windows.Forms.Timer timer;

        private int move_image_timer = 0;
        private string main_message = "";
        public string side_message = "";

        public ProgressPanel(System.Windows.Forms.Control parent, string main_message)
        {
            this.main_message = main_message;
            this.Size = parent.ClientSize;
            this.parent = parent;
            this.Paint += _Paint;
            this.Resize += _Resize;

            timer = new Timer();
            timer.Interval = 50;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            move_image_timer++;
            Refresh();
        }

        private void _Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var blush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
            var blush2 = new SolidBrush(Color.FromArgb(255, 80, 80, 80));

            g.FillRectangle(blush2, 0, 0, Size.Width, Size.Height);

            {
                //var x
                //        font = new Font("メイリオ", 7);

                using (var sf = new StringFormat(StringFormat.GenericTypographic))
                {
                    var text = main_message;
                    var font = new Font("メイリオ", 16, FontStyle.Regular);
                    var size = g.MeasureString(text, font, this.Width, sf);
                    var blush3 = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

                    var x = (Size.Width - size.Width) / 2;
                    var y = (Size.Height - size.Height) / 4;

                    g.DrawString(text, font,blush3, x, y);

                    text = side_message;
                    size = g.MeasureString(text, font, this.Width, sf);
                    x = (Size.Width - size.Width) / 2;
                    y = (Size.Height - size.Height) / 2;
                    g.DrawString(text, font, blush3, x, y-60);
                }

            }

            {
                var w = 600;
                var h = 30;
                var x = (Size.Width - w)/2;
                var y = (Size.Height - h) / 2;
                var blush3 = new SolidBrush(Color.FromArgb(255, 40, 40, 40));
                g.FillRectangle(blush3, x, y, w, h);

                w = value * w / value_max;
                var blush4 = new SolidBrush(Color.FromArgb(255, 130, 230, 230));
                g.FillRectangle(blush4, x, y, w, h);
            }

            {
                var circle = 80;
                var ox = Size.Width  / 2;
                var oy = Size.Height / 2 + circle + 60;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var max = 6;
                for( var i =0; i< max; i++)
                {
                    var ri = 2 * Math.PI / max;
                    var ai = 250 / max;

                    var r = (double)(move_image_timer) / 10.0 - i * ri;
                    var x = Math.Cos(r) * 80 + ox;
                    var y = Math.Sin(r) * 80 + oy;
                    var blush5 = new SolidBrush(Color.FromArgb(255-i* ai, 120, 120, 120));
                    g.FillPie(blush5, new Rectangle((int)x, (int)y, 30, 30), 0, 360);

                }

            }
        }

        private void _Resize(object sender, EventArgs e)
        {
            this.Size = parent.ClientSize;
            
        }

        delegate void delegate_action();
        private void invork_Reflesh()
        {
            this.Refresh();
        }
        public void RefleshByOtherThread()
        {
            try
            {
                Invoke(new delegate_action(invork_Reflesh));
            }
            catch
            {

            }
        }
            

    }
}
