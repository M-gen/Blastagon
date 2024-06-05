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
using Blastagon.UI;
using Blastagon.UI.Common;

namespace Blastagon.UI.Common
{
    public class MenuItemLines
    {
        public List<MenuItemLine> items = new List<MenuItemLine>();
        int ox = 30;
        int oy = 30;

        public void Draw( Graphics g )
        {
            var x = ox;
            var y = oy;
            foreach( var il in items )
            {
                var h = 0;
                foreach( var item in il.items)
                {
                    item.Draw(g, x, y);
                    if (h < item.h) h = item.h;
                }
                y += h;
            }
        }

        public void Update()
        {
            var x = ox;
            var y = oy;
            foreach (var il in items)
            {
                var w = MenuItemLine.LINE_WIDTH;
                var h = 0;
                foreach (var item in il.items)
                {
                    item.Update(x,y,w);
                    w -= item.rw;
                    if (h < item.h) h = item.h;
                }
                y += h;
            }

        }

        public MenuItemLine AddLine( MenuItem item )
        {
            var il = new MenuItemLine();
            il.items.Add(item);
            items.Add(il);
            return il;
        }

    }

    public class MenuItemLine
    {
        public const int LINE_WIDTH = 1000;

        public List<MenuItem> items = new List<MenuItem>();
        //public int h;
    }

    public class MenuItem
    {
        public int h = 0;
        public int rw = 0;

        public virtual void Draw(Graphics g, int x, int y )
        {
        }

        public virtual void Update(int x, int y, int w)
        {
        }
    }
    
    public class MenuTitle : MenuItem
    {
        public string title;
        public MenuTitle(string title)
        {
            this.title = title;
            this.h = 55 + 20;
        }

        public override void Draw(Graphics g, int x, int y)
        {
            var font = new Font("メイリオ", 20, FontStyle.Regular);
            var blush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
            var pen = new Pen(Color.FromArgb(180, 255, 255, 255),1.5f);

            g.DrawString(title, font, blush, x, y);

            var y2 = y + 55;
            g.DrawLine(pen, new Point(x, y2), new Point(x + MenuItemLine.LINE_WIDTH, y2));
        }
    }

    public class MenuText : MenuItem
    {
        public string text;
        public MenuText(string text)
        {
            this.text = text;
            this.h = 40;
        }

        public override void Draw(Graphics g, int x, int y)
        {
            var font = new Font("メイリオ", 16, FontStyle.Regular);
            var blush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
            var pen = new Pen(Color.FromArgb(180, 255, 255, 255), 1.5f);

            var y1 = y + 3;
            g.DrawString(text, font, blush, x, y1);
        }
        

    }

    public class MenuTextB : MenuItem
    {
        public string text;
        public MenuTextB(string text)
        {
            this.text = text;
            this.h = 40 + 5;
        }

        public override void Draw(Graphics g, int x, int y)
        {
            var font = new Font("メイリオ", 16, FontStyle.Bold);
            var blush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
            var pen = new Pen(Color.FromArgb(180, 255, 255, 255), 1.5f);

            var y1 = y + 5;
            g.DrawString(text, font, blush, x, y1);

        }
    }

    public class MenuButton : MenuItem
    {
        public System.Windows.Forms.Control button;
        public MenuButton(System.Windows.Forms.Control button )
        {
            this.button = button;
            this.h = button.Size.Height + 10;
            this.rw = button.Size.Width + 10;
        }

        public override void Update(int x, int y, int w)
        {
            var y2 = y + 5;
            button.Location = new Point(x + w - button.Size.Width, y2);
        }
    }
}
