using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.IO;

using Blastagon.Common;
using Blastagon.PluginFileConector;


namespace Blastagon.App
{
    public class Config
    {
        public class AutoSaveConfig
        {
            public string file_path = "";
            public Size window_size = new Size( 0, 0);
            public Point window_position = new Point(0, 0);
            public int main_drag_bar_v_position_x = 0;
            public int thumbnail_view_line_num = 0;
            public bool window_size_maximum = false;

            public bool Load()
            {
                if (!File.Exists(file_path)) return false;

                using (var file = new System.IO.StreamReader(file_path))
                {
                    var line = "";
                    while ((line = file.ReadLine()) != null)
                    {
                        string[] s = line.Split(' ');

                        //var tag_full_name = StringBase64.ToUTF8(s[0]);
                        //var tag_word = StringBase64.ToUTF8(s[0]);
                        //var tag_word_split = tag_word.Replace(@"\,", "\t").Split('\t');

                        if (s.Count() > 1)
                        {
                            switch (s[0])
                            {
                                case "window_position":
                                    window_position = new Point(int.Parse(s[1]), int.Parse(s[2]));
                                    break;
                                case "window_size":
                                    window_size = new Size(int.Parse(s[1]), int.Parse(s[2]));
                                    break;
                                case "window_size_maximum":
                                    window_size_maximum = s[1] == "True";
                                    break;
                                case "main_drag_bar_v_position_x":
                                    main_drag_bar_v_position_x = int.Parse(s[1]);
                                    break;
                                case "thumbnail_view_line_num":
                                    thumbnail_view_line_num = int.Parse(s[1]);
                                    break;
                            }
                        }

                    }
                }

                return true;
            }

            public void Save()
            {

                //ファイルを上書きし、UTF-8で書き込む
                using (var sw = new System.IO.StreamWriter(
                    file_path,
                    false,
                    System.Text.Encoding.GetEncoding("UTF-8")))
                {
                    sw.WriteLine("window_position {0} {1}", window_position.X, window_position.Y);
                    sw.WriteLine("window_size {0} {1}", window_size.Width, window_size.Height);
                    sw.WriteLine("window_size_maximum {0}", window_size_maximum);
                    sw.WriteLine("main_drag_bar_v_position_x {0}", main_drag_bar_v_position_x);
                    sw.WriteLine("thumbnail_view_line_num {0}", thumbnail_view_line_num);
                    //sw.WriteLine("{0},{1},{2},{3}", file_path_b, w, h, tag_b);
                }
            }
        }
        public class AutoSavePicupView
        {

            public string file_path = "";
            //public List<ReactData> items = new List<ReactData>();
            public Blastagon.UI.PickUpView pickup_view;

            public bool Load()
            {
                if (!File.Exists(file_path)) return false;

                using (var file = new System.IO.StreamReader(file_path))
                {
                    var line = "";
                    while ((line = file.ReadLine()) != null)
                    {
                        string[] s = line.Split(',');

                        if (line == "") continue;

                        var file_path = StringBase64.ToUTF8(s[0]);
                        if (!AppCore.core.image_library.image_datas.ContainsKey(file_path)) continue;

                        var image_data = AppCore.core.image_library.image_datas[file_path];
                        var item = new Blastagon.UI.PickUpView.ImageBox(image_data);

                        var view_x = double.Parse(s[1]);
                        var view_y = double.Parse(s[2]);
                        var view_w = double.Parse(s[3]);
                        var view_h = double.Parse(s[4]);
                        var src_x =  double.Parse(s[5]);
                        var src_y =  double.Parse(s[6]);
                        var src_w =  double.Parse(s[7]);
                        var src_h =  double.Parse(s[8]);

                        var plugin = FileConectorManager.GetFileConector(image_data.file_path, false);
                        item.image = plugin.image_conector.FromFile(null, image_data.file_path, "");
                        item.view_rect = new RectangleD(view_x, view_y, view_w, view_h);
                        item.src_rect  = new RectangleD(src_x, src_y, src_w, src_h);
                        pickup_view.items.Add(item);

                    }
                }

                return true;
            }

            public void Save()
            {

                //ファイルを上書きし、UTF-8で書き込む
                using (var sw = new System.IO.StreamWriter(
                    file_path,
                    false,
                    System.Text.Encoding.GetEncoding("UTF-8")))
                {
                    foreach( var item in pickup_view.items)
                    {
                        if (item == null) continue;
                        var s = "";
                        s += Common.StringBase64.ToBase64(item.image_data.file_path) + ",";
                        s += item.view_rect.X.ToString() + "," + item.view_rect.Y.ToString() + "," + item.view_rect.Width.ToString() + "," + item.view_rect.Height.ToString() + ",";
                        s += item.src_rect.X.ToString() + "," + item.src_rect.Y.ToString() + "," + item.src_rect.Width.ToString() + "," + item.src_rect.Height.ToString() + ",";
                        sw.WriteLine(s);
                    }
                }
            }
        }

        public AutoSaveConfig auto_save_config = new AutoSaveConfig();
        public AutoSavePicupView auto_save_pickup_view = new AutoSavePicupView();

        public Config(string auto_save_config_file_path, string auto_save_pickup_view_file)
        {
            auto_save_config.file_path = auto_save_config_file_path;
            auto_save_pickup_view.file_path = auto_save_pickup_view_file;
        }



    }
}
