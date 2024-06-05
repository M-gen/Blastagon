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
using Blastagon.App.ImageAnalyze;
using System.Threading;

namespace Blastagon.UI.Menu
{
    public class MenuPanel : Panel
    {
        System.Windows.Forms.Control parent;

        public bool is_show = false;

        Panel buttons_panel;
        Panel main_export_panel;
        Panel main_auto_tag_panel;
        Panel main_other_panel;

        ProgressPanel progress_panel;

        Thread progress_thread;
        public bool is_progress_thread_end = false;

        MenuItemLines menu_item_lines_export = new MenuItemLines();
        MenuItemLines menu_item_lines_auto_tag  = new MenuItemLines();
        MenuItemLines menu_item_lines_other  = new MenuItemLines();

        List<Panel> panels = new List<Panel>();

        public MenuPanel(System.Windows.Forms.Control parent )
        {
            this.parent = parent;
            this.Location = new Point(0, 0);

            this.Resize += _Resize;
            this.Paint += _Paint;

            buttons_panel = new Panel();
            this.Controls.Add(buttons_panel);
            buttons_panel.Size = new Size(300,600);

            main_export_panel = new Panel();
            this.Controls.Add(main_export_panel);
            main_export_panel.BackColor = Color.FromArgb(255, 80, 80, 80);
            main_export_panel.Paint += Main_export_panel_Paint;
            panels.Add(main_export_panel);

            main_other_panel = new Panel();
            this.Controls.Add(main_other_panel);
            main_other_panel.BackColor = Color.FromArgb(255, 80, 80, 80);
            main_other_panel.Paint += Main_other_panel_Paint;
            panels.Add(main_other_panel);

            main_auto_tag_panel = new Panel();
            this.Controls.Add(main_auto_tag_panel);
            main_auto_tag_panel.BackColor = Color.FromArgb(255, 80, 80, 80);
            main_auto_tag_panel.Paint += Main_auto_tag_panel_Paint;
            panels.Add(main_auto_tag_panel);


            System.Windows.Forms.Control btn;
            var h = 42;
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 0, h * 0, 300, 42, "戻る");
            btn.Click += (s, e) =>
            {
                HideEx();
            };
            //btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 0, h * 1, 300, 42, "オプション");
            //btn.Click += (s, e) =>
            //{
            //};
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 0, h * 1, 300, 42, "エクスポート");
            btn.Click += (s, e) =>
            {
                ShowSelectPanel(main_export_panel);
                //main_export_panel.Show();
                //main_other_panel.Hide();
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 0, h * 2, 300, 42, "自動タグ付け");
            btn.Click += (s, e) =>
            {
                ShowSelectPanel(main_auto_tag_panel);
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 0, h * 3, 300, 42, "その他");
            btn.Click += (s, e) =>
            {
                ShowSelectPanel(main_other_panel);
                //main_other_panel.Show();
                //main_export_panel.Hide();
            };

            //
            btn = UserInterfaceCommon.CreateButton(new BButton(), main_export_panel, 30, 200 + h * 4, 300, 42, "全ての画像に書き込む");
            btn.Click += (s, e) =>
            {
                CreateProgressPanel("タグの情報をファイルに書き込んでいます");
                progress_panel.side_message = "";
                progress_thread = new Thread(new ThreadStart(Thread_All_Write));
                progress_thread.Start();
            };
            var btn_export_all = btn;
            //
            btn = UserInterfaceCommon.CreateButton(new BButton(), main_export_panel, 30+300+30, 200 + h * 4, 400, 42, "サムネイルに表示されている画像に書き込む");
            btn.Click += (s, e) =>
            {
                CreateProgressPanel("タグの情報をファイルに書き込んでいます");
                progress_panel.side_message = "";
                progress_thread = new Thread(new ThreadStart(Thread_Thumbnail_Write));
                progress_thread.Start();
            };
            var btn_export_thumbnail = btn;
            //
            btn = UserInterfaceCommon.CreateButton(new BButton(), main_other_panel, 30 + 300 + 30, 200 + h * 4, 400, 42, "表示");
            btn.Click += (s, e) =>
            {
                AppCore.core.ShowImageByNoneTag(10000);
                this.HideEx();
            };
            var btn_other_free_images = btn;
            //
            btn = UserInterfaceCommon.CreateButton(new BButton(), main_auto_tag_panel, 30 + 300 + 30, 200 + h * 4, 400, 42, "サムネイルに表示されている画像にタグをつける");
            btn.Click += (s, e) =>
            {
                CreateProgressPanel("画像を解析しています");
                progress_panel.side_message = "";
                progress_thread = new Thread(new ThreadStart(Thread_AutoTagByColorPick_1));
                progress_thread.Start();
            };
            var btn_auto_tag_main_color_thumbnail = btn;
            btn = UserInterfaceCommon.CreateButton(new BButton(), main_auto_tag_panel, 30 + 300 + 30, 200 + h * 4, 400, 42, "サムネイルに表示されている画像にタグをつける");
            btn.Click += (s, e) =>
            {
                CreateProgressPanel("画像を解析しています");
                progress_panel.side_message = "";
                progress_thread = new Thread(new ThreadStart(Thread_AutoTagByColorPick_4));
                progress_thread.Start();
            };
            var btn_auto_tag_main_color_2_thumbnail = btn;
            btn = UserInterfaceCommon.CreateButton(new BButton(), main_auto_tag_panel, 30 + 300 + 30, 200 + h * 4, 400, 42, "サムネイルに表示されている画像にタグをつける");
            btn.Click += (s, e) =>
            {
                CreateProgressPanel("画像を解析しています");
                progress_panel.side_message = "";
                progress_thread = new Thread(new ThreadStart(Thread_AutoTagBySaturationStep));
                progress_thread.Start();
            };
            var btn_auto_tag_main_color_3_thumbnail = btn;

            // エクスポート
            menu_item_lines_export.AddLine(new MenuTitle("エクスポート"));
            menu_item_lines_export.AddLine(new MenuTextB("タグの情報を ファイルへ書き込む"));
            menu_item_lines_export.AddLine(new MenuText("これにより、パソコンを引っ越ししたときでも、ファイルからタグ情報を復元できます"));
            menu_item_lines_export.AddLine(new MenuText("JPEGの書き込みは、Exifの正規の利用からはずれています"));
            
            var il = new MenuItemLine();
            menu_item_lines_export.items.Add(il);
            il.items.Add(new MenuButton(btn_export_thumbnail));
            il.items.Add(new MenuButton(btn_export_all));
            menu_item_lines_export.Update();

            // 自動タグ付け
            menu_item_lines_auto_tag.AddLine(new MenuTitle("自動タグ付け"));
            menu_item_lines_auto_tag.AddLine(new MenuTextB("主要色による判定"));
            menu_item_lines_auto_tag.AddLine(new MenuText("主要色1つを抽出し、色タグを1つ設定します"));
            il = new MenuItemLine();
            menu_item_lines_auto_tag.items.Add(il);
            il.items.Add(new MenuButton(btn_auto_tag_main_color_thumbnail));
            menu_item_lines_auto_tag.AddLine(new MenuTextB("主要色による判定"));
            menu_item_lines_auto_tag.AddLine(new MenuText("主要色4つを抽出し、色タグを複数設定します"));
            il = new MenuItemLine();
            menu_item_lines_auto_tag.items.Add(il);
            il.items.Add(new MenuButton(btn_auto_tag_main_color_2_thumbnail));
            menu_item_lines_auto_tag.AddLine(new MenuTextB("モノクロ・彩度判定"));
            menu_item_lines_auto_tag.AddLine(new MenuText("モノクロか、彩度の最大値別にタグを設定します"));
            il = new MenuItemLine();
            menu_item_lines_auto_tag.items.Add(il);
            il.items.Add(new MenuButton(btn_auto_tag_main_color_3_thumbnail));
            //il.items.Add(new MenuButton(btn_export_all));
            menu_item_lines_auto_tag.Update();

            // その他
            menu_item_lines_other.AddLine(new MenuTitle("その他"));
            menu_item_lines_other.AddLine(new MenuTextB("どのタグにも含まれない画像をサムネイルに表示する"));
            menu_item_lines_other.AddLine(new MenuText("なんらかの操作で、どのタグからも紐付かなくなった画像をサムネイルに表示します"));

            il = new MenuItemLine();
            menu_item_lines_other.items.Add(il);
            il.items.Add(new MenuButton(btn_other_free_images));
            //il.items.Add(new MenuButton(btn_export_all));

            menu_item_lines_other.Update();
        }

        private void Main_export_panel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            menu_item_lines_export.Draw(g);
        }

        private void Main_other_panel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            menu_item_lines_other.Draw(g);
        }

        private void Main_auto_tag_panel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            menu_item_lines_auto_tag.Draw(g);
        }

        private void _Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 30, 30, 30)), 0, 0, Size.Width, Size.Height);

        }

        private void CreateProgressPanel(  string main_message)
        {
            progress_panel = new Common.ProgressPanel(this, main_message);
            this.Controls.Add(progress_panel);
            progress_panel.BringToFront();

        }

        private void _Resize(object sender, EventArgs e)
        {
            this.Size = parent.ClientSize;
            buttons_panel.Location = new Point(Size.Width - buttons_panel.Width, 0);
            buttons_panel.Size = new Size(buttons_panel.Width, Size.Height);

            foreach (var panel in panels)
            {
                panel.Size = new Size(Size.Width - buttons_panel.Width, Size.Height);
            }
            //main_other_panel.Size = new Size(Size.Width - buttons_panel.Width, Size.Height);
            if (progress_panel!=null) progress_panel.Size = this.Size;
        }

        private void Thread_All_Write()
        {
            GC.Collect();

            var counter = 0;
            var counter_max = AppCore.core.image_library.image_datas.Count();
            progress_panel.value_max = counter_max;
            AppCore.core.image_library.image_datas.ForeachBreak((id) =>
            {
                progress_panel.value = counter;
                progress_panel.side_message = counter + " / " +  counter_max;
                progress_panel.RefleshByOtherThread();
                AppCore.core.WriteTagDataInImageFiles_Frame(id.Value);
                counter++;
                //AppCore.core.form.Text = Title + " " + counter + " / " + counter_max;

                if (is_progress_thread_end) return true;
                else return false;
            });

            ProgressPanelDispose();
        }

        private void Thread_Thumbnail_Write()
        {
            GC.Collect();

            //AppCore.core.thumbnail_view.ForeachThumbnail((t) =>
            var image_datas = new List<ImageLibrary.ImageDataExWordSet>();
            foreach (var t in AppCore.core.thumbnail_view.thumbnails)
            {
                image_datas.Add(t.data);
            }
            var counter = 0;
            var counter_max = image_datas.Count();
            progress_panel.value_max = counter_max;

            foreach (var image_data in image_datas)
            //AppCore.core.image_library.image_datas.ForeachBreak((id) =>
            {
                progress_panel.value = counter;
                progress_panel.side_message = counter + " / " + counter_max;
                progress_panel.RefleshByOtherThread();
                AppCore.core.WriteTagDataInImageFiles_Frame(image_data.image_data);
                counter++;
                //AppCore.core.form.Text = Title + " " + counter + " / " + counter_max;
                
            }

            ProgressPanelDispose();
        }

        public void Thread_AutoTagByColorPick_1()
        {
            if (AppCore.core.thumbnail_view.thumbnails.Count() == 0) return; // 0コなので無視する

            GC.Collect();

            var image_datas = new List<ImageLibrary.ImageDataExWordSet>();
            foreach (var t in AppCore.core.thumbnail_view.thumbnails)
            {
                image_datas.Add(t.data);
            }
            var counter = 0;
            var counter_max = image_datas.Count();
            progress_panel.value_max = counter_max;

            foreach (var image_data in image_datas)
            {
                progress_panel.value = counter;
                progress_panel.side_message = counter + " / " + counter_max;
                progress_panel.RefleshByOtherThread();

                //// 複数色対応
                //// todo : 重複してタグを付けてしまう問題...
                //// どうも、彩度の低い色をつけてしまうと微妙な気がする…
                try
                {
                    var color_pick_up_front = new ColorPickUpFront(image_data.image_data, 1);
                    Thread_AutoTagByColorPick_ConectTag(image_data, ref color_pick_up_front.tag_names, @"Blastagon\:色1\:");
                }
                catch
                {
                    AppCore.core.popup_log.AddMessage("解析エラー : " + image_data.image_data.file_path + " : Thread_AutoTagByColorPick_1");
                }
                counter++;

            }
            AppCore.core.image_library.UpdateTagStatus();
            AppCore.core.tree_list.is_refresh = true;

            ProgressPanelDispose();
        }

        public void Thread_AutoTagByColorPick_4()
        {
            if (AppCore.core.thumbnail_view.thumbnails.Count() == 0) return; // 0コなので無視する

            GC.Collect();

            var image_datas = new List<ImageLibrary.ImageDataExWordSet>();
            foreach (var t in AppCore.core.thumbnail_view.thumbnails)
            {
                image_datas.Add(t.data);
            }
            var counter = 0;
            var counter_max = image_datas.Count();
            progress_panel.value_max = counter_max;

            foreach (var image_data in image_datas)
            {
                progress_panel.value = counter;
                progress_panel.side_message = counter + " / " + counter_max;
                progress_panel.RefleshByOtherThread();

                //// 複数色対応
                //// todo : 重複してタグを付けてしまう問題...
                //// どうも、彩度の低い色をつけてしまうと微妙な気がする…
                try
                {
                    var color_pick_up_front = new ColorPickUpFront(image_data.image_data, 5);
                    Thread_AutoTagByColorPick_ConectTag(image_data, ref color_pick_up_front.tag_names, @"Blastagon\:色4\:");
                }
                catch
                {
                    AppCore.core.popup_log.AddMessage("解析エラー : " + image_data.image_data.file_path + " : Thread_AutoTagByColorPick_4");
                }
                counter++;

            }
            AppCore.core.image_library.UpdateTagStatus();
            AppCore.core.tree_list.is_refresh = true;

            ProgressPanelDispose();
        }

        public void Thread_AutoTagBySaturationStep()
        {
            if (AppCore.core.thumbnail_view.thumbnails.Count() == 0) return; // 0コなので無視する

            GC.Collect();
            var tag_name_head = @"Blastagon\:彩度\:";

            // タグを整列させたいので順番に作っておく
            {
                var tag_names = new string[] { "0.1", "0.2", "0.3", "0.4", "0.5", "0.6", "0.7", "0.8", "0.9", "1.0" };
                foreach (var tag_name in tag_names)
                {
                    var tag_name_2 = tag_name_head + tag_name;
                    var library_tag = AppCore.core.image_library.GetTag(tag_name_2);
                    if (library_tag == null)
                    {
                        library_tag = AppCore.core.CreateTagByFullName(tag_name_2);
                    }
                }
            }

            //
            var image_datas = new List<ImageLibrary.ImageDataExWordSet>();
            foreach (var t in AppCore.core.thumbnail_view.thumbnails)
            {
                image_datas.Add(t.data);
            }
            var counter = 0;
            var counter_max = image_datas.Count();
            progress_panel.value_max = counter_max;

            foreach (var image_data in image_datas)
            {
                progress_panel.value = counter;
                progress_panel.side_message = counter + " / " + counter_max;
                progress_panel.RefleshByOtherThread();

                //// 複数色対応
                //// todo : 重複してタグを付けてしまう問題...
                //// どうも、彩度の低い色をつけてしまうと微妙な気がする…
                try
                {
                    var saturation_step = new SaturationStep(image_data.image_data);
                    Thread_AutoTagByColorPick_ConectTag(image_data, ref saturation_step.tag_names, tag_name_head);
                }
                catch
                {
                    AppCore.core.popup_log.AddMessage("解析エラー : " + image_data.image_data.file_path + " : Thread_AutoTagBySaturationStep");
                }
                counter++;

            }
            AppCore.core.image_library.UpdateTagStatus();
            AppCore.core.tree_list.is_refresh = true;

            ProgressPanelDispose();
        }

        public void Thread_AutoTagByColorPick_ConectTag( ImageLibrary.ImageDataExWordSet image_data, ref List<string> keys, string tag_name_head )
        {
            //var color_pick_up_front = new ColorPickUpFront(image_data.image_data);


            // 複数色対応
            // todo : 重複してタグを付けてしまう問題...
            // どうも、彩度の低い色をつけてしまうと微妙な気がする…
            foreach (var tag_name in keys)
            {
                var tag_name_2 = tag_name_head + tag_name;
                var library_tag = AppCore.core.image_library.GetTag(tag_name_2);
                if (library_tag == null)
                {
                    library_tag = AppCore.core.CreateTagByFullName(tag_name_2);
                }
                image_data.image_data.tags.Add(new ImageLibrary.ImageTag(library_tag, ""));
            }

        }


        delegate void delegate_action();
        private void invork_ProgressPanelDispose()
        {
            progress_panel.Dispose();
            progress_panel = null;
        }
        public void ProgressPanelDispose()
        {
            try
            {
                Invoke(new delegate_action(invork_ProgressPanelDispose));
            }
            catch
            {

            }
        }

        public void ShowEx()
        {
            is_show = true;
            AppCore.core.thumbnail_view.Hide();
            AppCore.core.thumbnail_scrollbar_v.Hide();
            AppCore.core.tree_list.Hide();
            Size = parent.ClientSize;

            Show();
        }

        public void HideEx()
        {
            is_show = false;
            AppCore.core.tree_list.Show();
            AppCore.core.thumbnail_view.Show();
            AppCore.core.thumbnail_scrollbar_v.Show();

            AppCore.core.Form_Resize();
            this.Hide();
        }

        // 指定したパネルを表示し、他のパネルを非表示にする
        private void ShowSelectPanel(Panel select_panel)
        {
            foreach (var panel in panels)
            {
                if(select_panel==panel)
                {
                    panel.Show();
                }
                else
                {
                    panel.Hide();
                }
            }
        }

    }
}
