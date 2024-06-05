using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

using Blastagon.App;

namespace Blastagon.UI
{
    public class PopupLog 
    {
        List<string> messages = new List<string>();

        public Action Update;

        public PopupLog()
        {
        }
        private void _MouseUp(object sender, MouseEventArgs e)
        {
        }

        //public void AddMessage( string message )
        //{
        //}

        delegate void _delegate_AddMessage(string option);
        private void _Invoke_AddMessage(string message)
        {
            messages.Add(message);

            using (var stream = new System.IO.StreamWriter("log.txt", true))
            {
                stream.WriteLine(message);
            }

            if (message.Count() < 20) // todo とりあえず、一時しのぎ、更新しても見えない領域なので
            {
                if (Update != null) Update();
            }
        }
        public void AddMessage(string message)
        {
            AppCore.core.form.Invoke(new _delegate_AddMessage(_Invoke_AddMessage), new Object[] { message });

        }

        public void Clear()
        {
            messages.Clear();
            if (Update != null) Update();
        }

        public void Paint(Graphics g, Size size)
        {
            var font = new Font("メイリオ", 12);

            var h = 24;
            var oy = size.Height - messages.Count() * h-10;
            var i = 0;
            foreach(var message in messages)
            {
                //var y = oy + i * h;
                var y = i * h;
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 255, 255, 255)), 0,y, 2000, h);
                g.DrawString(message, font, new SolidBrush(Color.FromArgb(255, 0, 0, 0)), 0, y);
                i++;
            }
            //g.DrawString()

        }
        

    }
}
