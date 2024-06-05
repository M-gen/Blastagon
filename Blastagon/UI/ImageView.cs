using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

using Blastagon.Common;
using Blastagon.App;
using Blastagon.PluginFileConector;

namespace Blastagon.UI
{
    public class ImageView : PictureBox
    {
        System.Windows.Forms.Control from;

        public ImageLibrary.ImageDataExWordSet image_data_ex_word;
        Image image;
        PointD offset = new PointD(0, 0);
        public double scale = 0;

        PointD mouse_image_pos = new PointD(0, 0);
        Point mouse_drag_start_pos = new Point(0, 0);
        Point mouse_drag_end_pos   = new Point(0, 0);
        PointD mouse_drag_offset   = new PointD(0,0);
        bool is_mouse_left_drag = false;
        bool is_mouse_left_drag_move = false;
        bool is_mouse_on = false;
        public bool is_show = false;
        public bool is_clip = false;
        Rectangle clip = new Rectangle(0,0,0,0);

        public ImageView(System.Windows.Forms.Control from)
        {
            this.from = from;
            from.Controls.Add(this);

            this.Size = new Size(1000, 500);
            //this.Location = new Point(0,30);
            this.MouseUp += _MouseUp;
            this.MouseDown += _MouseDown;
            this.MouseMove += _MouseMove;
            this.MouseHover += _MouseHover;
            this.MouseLeave += _MouseLeave;
            this.Paint += _Paint;
            this.Hide();

        }

        private void _MouseLeave(object sender, EventArgs e)
        {
            is_mouse_on = false;
        }

        private void _MouseHover(object sender, EventArgs e)
        {
            is_mouse_on = true;
        }

        private void _MouseMove(object sender, MouseEventArgs e)
        {
            //var target_image_pos = new Point(offset.X, offset.Y);
            //mouse_image_pos = new Point(e.X - target_image_pos.X, e.Y - target_image_pos.Y);
            mouse_image_pos.X = e.X - offset.X;
            mouse_image_pos.Y = e.Y - offset.Y;
            //Console.WriteLine("M {0},{1}", e.X, e.Y);
            //Console.WriteLine("P {0},{1}", mouse_image_pos.X, mouse_image_pos.Y);

            if (is_mouse_left_drag)
            {
                is_mouse_left_drag_move = true;
                mouse_drag_offset.X = e.X - mouse_drag_start_pos.X;
                mouse_drag_offset.Y = e.Y - mouse_drag_start_pos.Y;

                Refresh();
            }
        }

        private void _MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouse_drag_start_pos.X = e.X;
                mouse_drag_start_pos.Y = e.Y;
                mouse_drag_offset.X = 0;
                mouse_drag_offset.Y = 0;
                //= new Point(e.X, e.Y);
                is_mouse_left_drag = true;
                is_mouse_left_drag_move = false; // まだ動いてはいない
                //if (is_mouse_drag)
                //{
                //}
                //else
                //{
                //    is_mouse_drag = true;
                //}
            }
        }

        private void _MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                is_show = false;
                this.Hide();
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (is_mouse_left_drag_move)
                {
                    is_mouse_left_drag = false;
                    is_mouse_left_drag_move = false;

                    offset.X += mouse_drag_offset.X;
                    offset.Y += mouse_drag_offset.Y;
                    mouse_drag_offset.X = 0;
                    mouse_drag_offset.Y = 0;
                    mouse_drag_start_pos.X = 0;
                    mouse_drag_start_pos.Y = 0;
                    Refresh();
                }
                else
                {
                    is_mouse_left_drag = false;
                    is_mouse_left_drag_move = false;

                    if (e.X < Size.Width / 2)
                    {
                        var back_image_data = AppCore.GetBackImageByThumbnailView(image_data_ex_word);
                        if (back_image_data != null) this.ViewImage(back_image_data, is_clip);

                    }
                    else
                    {
                        var next_image_data = AppCore.GetNextImageByThumbnailView(image_data_ex_word);
                        if (next_image_data != null) this.ViewImage(next_image_data, is_clip);
                    }
                }
            }

        }

        private void _Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 0, 0, 0)), 0, 0, Size.Width, Size.Height);


            if (image != null)
            {
                var w = image.Width  * scale;
                var h = image.Height * scale;

                if (is_clip)
                {
                    image_data_ex_word.AnalyzeExWord(ref clip, ref is_clip);
                    w = clip.Width * scale;
                    h = clip.Height * scale;

                    var dst_rect = new RectangleF((float)(offset.X + mouse_drag_offset.X), (float)(offset.Y + mouse_drag_offset.Y), (float)w, (float)h);
                    g.DrawImage(image, dst_rect, clip, GraphicsUnit.Pixel);
                }
                else
                {
                    g.DrawImage(image, (float)(offset.X + mouse_drag_offset.X), (float)(offset.Y + mouse_drag_offset.Y), (float)w, (float)h);
                }
            }

        }

        public void ViewImage(ImageLibrary.ImageDataExWordSet image_data_ex_word, bool is_clip)
        {
            this.is_clip = is_clip;
            if (image_data_ex_word.ex_word=="")
            {
                this.is_clip = false;
            }

            this.is_show = true;
            this.Size = this.from.Size;

            if (this.image_data_ex_word == image_data_ex_word)
            {
                this.Show();
                return;
            }


            this.image_data_ex_word = image_data_ex_word;
            var plugin = FileConectorManager.GetFileConector(image_data_ex_word.image_data.file_path, false);
            image = plugin.image_conector.FromFile(null, image_data_ex_word.image_data.file_path, "");
            
            ViewImage_ScaleAndOffset(image);
            this.Refresh();
            this.Show();
        }

        private void ViewImage_ScaleAndOffset(Image image)
        {
            var w = 0;
            var h = 0;
            if (is_clip)
            {
                image_data_ex_word.AnalyzeExWord(ref clip, ref is_clip);
                //w = clip.Width * scale;
                //h = clip.Height * scale;

                var w1 = Size.Width;
                var h1 = Size.Width * clip.Height / clip.Width;

                var w2 = Size.Height * clip.Width / clip.Height;
                var h2 = Size.Height;

                if (h1 > Size.Height)
                {
                    w = w2;
                    h = h2;
                    offset.X = (Size.Width - w2) / 2;
                    offset.Y = 0;
                    scale = (double)w / (double)clip.Width;
                }
                else
                {
                    w = w1;
                    h = h1;
                    offset.X = 0;
                    offset.Y = (Size.Height - h2) / 2;
                    scale = (double)w / (double)clip.Width;
                }
            }
            else
            {
                var w1 = Size.Width;
                var h1 = Size.Width * image.Height / image.Width;

                var w2 = Size.Height * image.Width / image.Height;
                var h2 = Size.Height;

                if (h1 > Size.Height)
                {
                    w = w2;
                    h = h2;
                    offset.X = (Size.Width - w2) / 2;
                    offset.Y = 0;
                    scale = (double)w / (double)image.Width;
                }
                else
                {
                    w = w1;
                    h = h1;
                    offset.X = 0;
                    offset.Y = (Size.Height - h2) / 2;
                    scale = (double)w / (double)image.Width;
                }
            }
        }

        // 指定した座標を基準に拡大縮小とオフセットを更新する
        public void UpdateScaleAndOffset( double next_scale)
        {
            const double MIN = 0.000001;
            if (next_scale < MIN) next_scale = MIN;

            if (is_mouse_on)
            {
                var offset_scale = next_scale / scale;
                var next_mouse_image_pos_x = mouse_image_pos.X * offset_scale;
                var next_mouse_image_pos_y = mouse_image_pos.Y * offset_scale;
                offset.X = offset.X + mouse_image_pos.X - next_mouse_image_pos_x;
                offset.Y = offset.Y + mouse_image_pos.Y - next_mouse_image_pos_y;

                scale = next_scale;
                mouse_image_pos.X = next_mouse_image_pos_x;
                mouse_image_pos.Y = next_mouse_image_pos_y;
            }
            else
            {
                var offset_scale = next_scale / scale;
                var image_center_x = image.Width  * scale * 0.5;
                var image_center_y = image.Height * scale * 0.5;
                var next_mouse_image_pos_x = image_center_x * offset_scale;
                var next_mouse_image_pos_y = image_center_y * offset_scale;
                offset.X = offset.X + image_center_x - next_mouse_image_pos_x;
                offset.Y = offset.Y + image_center_y - next_mouse_image_pos_y;

                scale = next_scale;
                mouse_image_pos.X = next_mouse_image_pos_x;
                mouse_image_pos.Y = next_mouse_image_pos_y;

            }


            Refresh();
        }
    }
}
