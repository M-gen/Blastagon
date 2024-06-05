using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;

using Blastagon.Common;

namespace Blastagon.UI.Common
{
    //public class BButton : Button
    public class BButton : Panel
    {
        private bool is_mouse_down = false;
        private bool is_mouse_on = false;

        public BButton()
        {
            this.Paint += _Paint;
            this.MouseDown += _MouseDown;
            this.MouseUp += _MouseUp;
            this.MouseHover += _MouseHover;
            this.MouseLeave += _MouseLeave;

        }

        private void _MouseLeave(object sender, EventArgs e)
        {
            is_mouse_on = false;
            this.Refresh();
        }

        private void _MouseHover(object sender, EventArgs e)
        {
            is_mouse_on = true;
            this.Refresh();
        }

        private void _MouseDown(object sender, MouseEventArgs e)
        {
            is_mouse_down = true;
            this.Refresh();
        }

        private void _MouseUp(object sender, MouseEventArgs e)
        {
            is_mouse_down = false;
            this.Refresh();
        }

        private void _Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var margin = 1;

            UserInterfaceCommon.DrawButtonOffEx(g, 0 + margin, 0 + margin, Size.Width - margin * 2, Size.Height - margin * 2, is_mouse_on, 0.5,(r)=>
            {
                if(is_mouse_down)
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(40, 0, 0, 0)), 0+ margin, 0+ margin, Size.Width- margin*2, Size.Height- margin*2);
                }
            }
            );

            var font = new Font("メイリオ", 11);
            {
                // テキストに付随する情報の表示
                using (StringFormat sf = new StringFormat(StringFormat.GenericTypographic))
                {
                    //var size = g.MeasureString(this.Text, font, this.Width, sf); ;
                    var size = g.MeasureString(this.Text, font, 1000, sf); ;

                    //if (size.Height > Size.Height)
                    //{
                    //    font = new Font("メイリオ", 7);
                    //    size = g.MeasureString(this.Text, font, this.Width, sf); ;
                    //}

                    //g.DrawString(string.Format("{0}", count), font_mini, fb2, x + 6 + size2.Width + 10, y + 4 + 3.5f);
                    //var x = (Size.Width - size.Width) / 2;
                    var x = 4;
                    var y = (Size.Height - size.Height) / 2;
                    if (y < 0) y = -y;

                    g.DrawString(this.Text, font, new SolidBrush(Color.FromArgb(255, 255, 255, 255)), x, y+1);
                }

            }

            if(this.BackgroundImage!=null)
            {
                var x = (Size.Width - this.BackgroundImage.Width) / 2;
                var y = (Size.Height - this.BackgroundImage.Height) / 2;
                g.DrawImage(this.BackgroundImage, x, y);
            }

            if(is_mouse_down)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(10, 0, 0, 0)), 0, 0, Size.Width, Size.Height);
            }

        }
    }
}
