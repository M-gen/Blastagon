using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Blastagon.UI
{
    public class LayoutController
    {
        class LayoutControl
        {
            public Control control;
            public Rectangle default_rect;
        }

        class Layout
        {
            public Size size;
            public List<LayoutControl> items = new List<LayoutControl>();
            public int w_now;
            public int w_min;
            public int w_max;
            public double scale = 1.0;

            public void Update()
            {
                var offset_x = 0;
                if (w_now < w_min)
                {
                    offset_x = w_now -w_min;
                    w_now = w_min;
                    Console.WriteLine(offset_x);
                }
                if (w_now > w_max) w_now = w_max;
                scale = (double)w_now / (double)size.Width;

                foreach( var ctrl in items)
                {
                    var x = (int)(ctrl.default_rect.X * scale);
                    var y = ctrl.default_rect.Y;
                    var w = (int)( ctrl.default_rect.Width * scale );
                    var h = ctrl.default_rect.Height;

                    ctrl.control.Location = new Point(x+ offset_x, y);
                    ctrl.control.Size = new Size(w, h);
                }
            }
        }

        Form main_form;
        Layout main_button_panel_layout = new Layout();

        public int main_form_last_w = 0;

        public LayoutController(Form main_form)
        {
            this.main_form = main_form;
            main_form_last_w = main_form.ClientSize.Width;

        }

        public void Main_InitButtonPanel( int default_width, int default_height )
        {
            var app_core = App.AppCore.core;
            var bp = app_core.buttons_panel;
            main_button_panel_layout.size = new Size(default_width, default_height); // bp.Size.Height
            main_button_panel_layout.w_now = main_button_panel_layout.size.Width;
            main_button_panel_layout.w_min = 120;
            //main_button_panel_layout.w_max = main_button_panel_layout.size.Width;
            main_button_panel_layout.w_max = default_width;

            foreach ( var ctrl in bp.Controls)
            {
                var lc = new LayoutControl();
                lc.control = (Control)ctrl;
                lc.default_rect = new Rectangle(lc.control.Location.X, lc.control.Location.Y, lc.control.Size.Width, lc.control.Size.Height);
                main_button_panel_layout.items.Add(lc);
            }

        }

        public void Main_Update()
        {

            var DB_X_MIN = 40;
            var DB_X_R_MIN = 0;


            var app_core = App.AppCore.core;
            var db = App.AppCore.core.main_drag_bar_v;
            
            var form_w = main_form.ClientSize.Width;
            var form_h = main_form.ClientSize.Height;
            var scroll_bar_width = app_core.thumbnail_scrollbar_v.Size.Width;
            var drag_bar_width = app_core.main_drag_bar_v.Size.Width;

            if (main_form_last_w != form_w)
            {
                var div = main_form_last_w - form_w;
                main_form_last_w = form_w;

                app_core.main_drag_bar_v.Location = new Point(app_core.main_drag_bar_v.Location.X - div, app_core.main_drag_bar_v.Location.Y);
            }
            var db_x = db.Location.X;

            if (db_x < DB_X_MIN)
            {
                db_x = DB_X_MIN;
            }

            if (db_x > main_form.ClientSize.Width - DB_X_R_MIN)
            {
                db_x = main_form.ClientSize.Width - DB_X_R_MIN;
            }

            var thumbnail_view_w = db_x - app_core.thumbnail_scrollbar_v.Size.Width;

            app_core.main_drag_bar_v.Size = new Size(drag_bar_width, form_h);

            // 高さが変わっただけでの変更が完全対応してない・・・
            if (form_h != app_core.thumbnail_scrollbar_v.Size.Height)
            {
                app_core.thumbnail_scrollbar_v.Size = new Size(app_core.thumbnail_scrollbar_v.Size.Width, form_h);
                app_core.thumbnail_scrollbar_v.RefleshBody();
            }

            //var is_width_resize = (thumbnail_view_w != app_core.thumbnail_view.Size.Width);
            
            var left_side_w = main_form.ClientSize.Width - db_x;
            var left_side_button_h = 220;
            //if (left_side_w < 100) left_side_w = 100;

            app_core.thumbnail_view.Size = new Size(thumbnail_view_w, form_h);
            app_core.thumbnail_view.Location = new Point(0, 0);
            app_core.thumbnail_view.ResizeOrLineNumChane();
            app_core.thumbnail_view.Refresh();
            app_core.thumbnail_scrollbar_v.Location = new Point(thumbnail_view_w, 0);


            app_core.tree_list.Location = new Point(thumbnail_view_w + scroll_bar_width + drag_bar_width, 0);
            app_core.tree_list.Size = new Size(left_side_w - drag_bar_width, form_h - left_side_button_h);
            app_core.tree_list.Refresh();

            main_button_panel_layout.w_now = left_side_w - drag_bar_width;
            main_button_panel_layout.Update();

            app_core.buttons_panel.Location = new Point(thumbnail_view_w + scroll_bar_width + drag_bar_width, form_h - left_side_button_h);
            app_core.buttons_panel.Size = new Size(left_side_w - drag_bar_width, left_side_button_h);
            app_core.buttons_panel.Refresh();

        }

    }
}
