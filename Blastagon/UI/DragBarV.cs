using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using Blastagon.UI.Common;

namespace Blastagon.UI
{
    public class DragBarV : PictureBox
    {
        private MouseLeftButtonEventController mouse_left_button_event_controller;
        private Point drag_start_pos;
        private Point drag_start_mouse_pos;
        private Point drag_now_mouse_pos;

        //public int size_left = 0;
        //public int size_right = 0;

        public Action<DragBarV> event_drag;

        public DragBarV(System.Windows.Forms.Control form)
        {
            form.Controls.Add(this);
            //this.Location = new Point(x, y);
            this.Size = new Size(6, form.ClientSize.Height);

            this.Cursor = Cursors.SizeWE;

            this.Paint += _Paint;
            this.MouseDown += _MouseDown;
            this.MouseUp += _MouseUp;
            this.MouseMove += _MouseMove;
            this.Resize += _Resize;

            mouse_left_button_event_controller = new MouseLeftButtonEventController();
            mouse_left_button_event_controller.event_drag_start = mouse_left_button_event_controller_DragStart;
            mouse_left_button_event_controller.event_drag       = mouse_left_button_event_controller_Drag;
            mouse_left_button_event_controller.event_drag_end   = mouse_left_button_event_controller_DragEnd;
        }
        private void _Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 80, 80, 80)), new Rectangle(0, 0, Size.Width, Size.Height));
        }
        private void _MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouse_left_button_event_controller.Update_MouseDown(e.X, e.Y);
            }
        }
        private void _MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouse_left_button_event_controller.Update_MouseUp(e.X, e.Y);
            }
        }
        private void _MouseMove(object sender, MouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Left)
            //{
            mouse_left_button_event_controller.Update_MouseMove(e.X, e.Y);
            //}
        }
        private void _Resize(object sender, EventArgs e)
        {
        }

        private void mouse_left_button_event_controller_DragStart(MouseLeftButtonEventController ec)
        {
            drag_start_pos = this.Location;

            drag_start_mouse_pos = new Point( System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
        }
        private void mouse_left_button_event_controller_Drag(MouseLeftButtonEventController ec)
        {
            drag_now_mouse_pos = new Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);

            var mx = drag_now_mouse_pos.X - drag_start_mouse_pos.X;
            var my = drag_now_mouse_pos.Y - drag_start_mouse_pos.Y;
            //this.Location = new Point(drag_start_pos.X - mx, drag_start_pos.Y - my);
            var next_x = drag_start_pos.X + mx;

            if (next_x > this.Parent.ClientSize.Width - this.Size.Width)
            {
                next_x = this.Parent.ClientSize.Width - this.Size.Width;
            }

            this.Location = new Point(next_x, 0);

            if (event_drag != null) event_drag(this);
        }

        private void mouse_left_button_event_controller_DragEnd(MouseLeftButtonEventController ec)
        {
        }
    }
}
