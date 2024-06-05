using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;

using Blastagon.Common;
using Blastagon.UI;
using Blastagon.UI.Common;
using Blastagon.UI.Menu;
using Blastagon.ResourceManage;
using Blastagon.ThreadManager;
using Blastagon.PluginFileConector;

namespace Blastagon.App
{

    public class AppCore : IDisposable
    {
        const string Title = "Blastagon";
        public static AppCore core;


        public Form form;
        ImageManager image_manager;
        SaveLoadManager save_load_manager;
        public ImageLibrary image_library;
        public ImageView image_view;
        public PickUpView picup_view;
        public ThumbnailView thumbnail_view;
        public ScrollBarV    thumbnail_scrollbar_v;
        public DragBarV main_drag_bar_v;
        public TreeList tree_list;
        public PopupLog popup_log;
        public FileConectorManager file_conect_manager;
        public MenuPanel menu_panel;
        public LayoutController layout_controller;
        public Config config;

        public Panel buttons_panel;

        public ProgressPanel progress_panel;

        ImageLibrary.Tag select_tag;

        string last_tag_search_word = "";
        int last_tag_search_index = 0;

        private TextBox text_box_tag_search;
        private bool is_setup_layout = true; // レイアウト初期化中

        public Button focusButton;
        //enum TargetMode
        //{
        //    None,
        //    TreeView,
        //    ImageView,
        //}

        private bool DebugConfig = true;

        public AppCore(Form form)
        {
            core = this;
            this.form = form;
            form.MouseWheel += Form_MouseWheel;
            form.Resize     += Form_Resize;
            form.Move       += Form_Move;
            form.KeyDown    += Form_KeyDown;
            form.KeyPreview = true;

            image_manager = new ImageManager();
            save_load_manager = new SaveLoadManager();
            image_library = new ImageLibrary();
            popup_log = new PopupLog();
            popup_log.Update = () => {
                thumbnail_view.Refresh();
            };

            thumbnail_view = new ThumbnailView(form);
            thumbnail_view.SelectImage = ThumbnailView_SelectImage;
            thumbnail_view.SelectClipImage = ThumbnailView_SelectClipImage;
            thumbnail_view.Scroll = (v) =>
            {
                thumbnail_scrollbar_v.SetScrollValue(v, false);
            };
            thumbnail_view.ResizeThumbnailSpace = () =>
            {
                //thumbnail_scrollbar_v.SetScrollValue(v, false);
                thumbnail_scrollbar_v.SetScrollMax(thumbnail_view.GetScrollValueMax());
            };
            thumbnail_view.DragEnter += Thumbnail_view_DragEnter;
            thumbnail_view.DragDrop  += Thumbnail_view_DragDrop;
            thumbnail_view.AllowDrop = true;

            thumbnail_scrollbar_v = new ScrollBarV(form);
            thumbnail_scrollbar_v.SetScrollAddValue(200);
            thumbnail_scrollbar_v.BringToFront();
            thumbnail_scrollbar_v.Scroll = (v) =>
            {
                form.Text = Title + " (" + thumbnail_view.thumbnails.Count() + ") " + v + "/" + thumbnail_scrollbar_v.GetScrollMax();
                thumbnail_view.SetScrollValue(v, false);
            };
            if (false) // todo: 一旦省略、
            {
                thumbnail_scrollbar_v.PaintEx = (g, x, y, w, h) =>
                {
                    g.SmoothingMode = SmoothingMode.None;

                    var brush = new SolidBrush(Color.FromArgb(255, 20, 20, 20));
                    var scroll_height = thumbnail_view.GetScrollHeight();
                    foreach( var t in thumbnail_view.thumbnails ) 
                    {
                        if (scroll_height <= 0) return;
                        if (t.is_tag_in)
                        {
                            var x1 = 0;
                            var y1 = y + t.pos.Y * h / scroll_height;
                            var w1 = w;
                            var h1 = t.size.Height * h / scroll_height;
                            if (h1 <= 0) h1 = 1;
                            g.FillRectangle(brush, x1, y1, w1, h1);
                        }
                    }
                };
            }

            tree_list = new TreeList(form, 1022, 120 + 10);
            tree_list.SelectItem       = TreeList_SelectItem;
            tree_list.EditName         = TreeList_EditName;
            tree_list.ChangeItemParent = TreeList_ChangeItemParent;
            tree_list.DragEnter += Tree_list_DragEnter;
            tree_list.DragDrop  += Tree_list_DragDrop;
            tree_list.AllowDrop = true;

            image_view = new ImageView(form);
            image_view.BringToFront();
            picup_view = new PickUpView(); form.Controls.Add(picup_view);
            picup_view.BringToFront();

            file_conect_manager = new FileConectorManager();

            buttons_panel = new Panel();
            form.Controls.Add(buttons_panel);

            menu_panel = new MenuPanel(form);
            form.Controls.Add(menu_panel);
            menu_panel.Size = form.ClientSize;
            menu_panel.BringToFront();
            menu_panel.Hide();

            main_drag_bar_v = new DragBarV(form);
            //main_drag_bar_v.BringToFront();
            main_drag_bar_v.event_drag = Form_ResizeByDragBar;

            config = new App.Config(@"data/tmp/config_a.txt", @"data/tmp/config_pickup_01.txt");
            config.auto_save_pickup_view.pickup_view = picup_view;
            //config.AutoSaveConfig_Save();

            // 初期化終了
            var main_drag_bar_v_position_x = 0;
            // コンフィグの読み込みと適応
            if (config.auto_save_config.Load())
            {
                form.Location = new Point(config.auto_save_config.window_position.X, config.auto_save_config.window_position.Y);

                if ((config.auto_save_config.window_size.Width != 0) && (config.auto_save_config.window_size.Height != 0))
                {
                    form.Size = new Size(config.auto_save_config.window_size.Width, config.auto_save_config.window_size.Height);
                }

                if (false) // 最大化表示が安定しないのでちょっと封印
                {
                    if (config.auto_save_config.window_size_maximum)
                    {
                        var sd = Blastagon.UI.Common.Common.ScreenCheck(form);
                        form.Size = new Size(sd.target_screen.Bounds.Width, sd.target_screen.Bounds.Height);
                        form.WindowState = FormWindowState.Maximized;
                    }
                }

                main_drag_bar_v_position_x = config.auto_save_config.main_drag_bar_v_position_x;

                if (main_drag_bar_v_position_x > form.ClientSize.Width) main_drag_bar_v_position_x = form.ClientSize.Width - 100;

                thumbnail_view.SetLineNum(config.auto_save_config.thumbnail_view_line_num);

            }
            else
            {
                form.Size = new Size(1500, 800);
                main_drag_bar_v_position_x = form.Size.Width - 320;

                // config.auto_save_config の初期値を代入
                config.auto_save_config.window_size = new Size( form.Size.Width, form.Size.Height);
                config.auto_save_config.window_position = new Point(form.Location.X, form.Location.Y);
                config.auto_save_config.thumbnail_view_line_num = 5;
                config.auto_save_config.main_drag_bar_v_position_x = main_drag_bar_v_position_x;
            }
            layout_controller = new LayoutController(form);

            SetupLocations(main_drag_bar_v_position_x);
            SetFormTitle("");
            _DebugInit();

            is_setup_layout = false;
            Form_Resize();

            image_library.UpdateTagStatus();
            tree_list.UpdateTagViewStatus();

            config.auto_save_pickup_view.Load();
            //tree_list.UpdateTagViewStatus();

            focusButton = new Button();
            focusButton.Location = new Point(0, 0);
            form.Controls.Add(focusButton);
            focusButton.Focus();
        }

        private void Tree_list_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // ドラッグ中のファイルやディレクトリの取得
                string[] drags = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string d in drags)
                {
                    if (!(System.IO.File.Exists(d) || System.IO.Directory.Exists(d)))
                    {
                        // ファイルまたはフォルダではない
                        return;
                    }
                }

                e.Effect = DragDropEffects.Copy;
            }
        }

        private void Tree_list_DragDrop(object sender, DragEventArgs e)
        {
            // ドラッグ＆ドロップされたファイル
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            var import_file_manager = new ImportFileManager();

            var mouse_pos_by_form = form.PointToClient(new Point(e.X,e.Y));
            var mouse_pos = new Point(mouse_pos_by_form.X - tree_list.Location.X, mouse_pos_by_form.Y - tree_list.Location.Y);
            var li = tree_list.CheckHitItem(mouse_pos.X, mouse_pos.Y);

            progress_panel = new ProgressPanel(form, "ファイルを読み込み中です\nタグが埋め込まれているか確認するため、時間がかかります");
            form.Controls.Add(progress_panel);
            progress_panel.BringToFront();

            if (li==null)
            {
                var tag_name = SaveLoadManager.NONE_TAG_IMAGE_IN_TAG_NAME;
                var library_tag = AppCore.core.image_library.GetTag(tag_name);
                if (library_tag == null)
                {
                    library_tag = AppCore.core.image_library.AddTag(tag_name);
                }
                import_file_manager.ImportFilesInTagTree(files, library_tag);
            }
            else
            {
                import_file_manager.ImportFilesInTagTree(files, (ImageLibrary.Tag)li.tmp_data);
            }



            //progress_panel = new ProgressPanel(form, "ファイルを読み込み中です\nタグが埋め込まれているか確認するため、時間がかかります");
            //form.Controls.Add(progress_panel);
            //progress_panel.BringToFront();

            //SaveLoadManager.LoadImageByDrop(files);

        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (image_view.is_show)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    image_view.is_show = false;
                    image_view.Hide();
                    // 無効音防止
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Left)
                {
                    var back_image_data = AppCore.GetBackImageByThumbnailView(image_view.image_data_ex_word);
                    if (back_image_data != null) image_view.ViewImage(back_image_data, image_view.is_clip);
                }
                else if (e.KeyCode == Keys.Right)
                {
                    var next_image_data = AppCore.GetNextImageByThumbnailView(image_view.image_data_ex_word);
                    if (next_image_data != null) image_view.ViewImage(next_image_data, image_view.is_clip);
                }
                e.Handled = true; 
            }
            else if (picup_view.is_show)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    picup_view.is_show = false;
                    picup_view.Hide();
                    // 無効音防止
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }

                e.Handled = true;
            }
            else
            {
                if (e.Control)
                {
                    if (e.KeyCode == Keys.S)
                    {
                        // 管理データの保存
                        SaveImageLibrary();
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.Oemplus)
                    {
                        if (image_view.is_show)
                        {
                            image_view.UpdateScaleAndOffset(image_view.scale * 1.1);
                            image_view.Refresh();
                            e.Handled = true;
                        }
                    }
                    else if (e.KeyCode == Keys.OemMinus)
                    {
                        if (image_view.is_show)
                        {
                            image_view.UpdateScaleAndOffset(image_view.scale / 1.1);
                            image_view.Refresh();
                            e.Handled = true;
                        }
                    }
                    else if (e.KeyCode == Keys.T)
                    {
                        var tag = default(ImageLibrary.Tag);
                        tag = CreateTag("New Tag", select_tag);
                        var li = tree_list.GetListItemByTmpData(tag);
                        tree_list.SetSelectItem(li);
                        tree_list.is_refresh = true;
                        tree_list.EditTagName(); // 名前変更状態にする

                        select_tag = null;
                        // thumbnail_view.select_tag = null;
                        
                        thumbnail_view.SetSelectTag(null);
                        thumbnail_view.is_reflesh_body = true;
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.I)
                    {
                        if (select_tag!=null)
                        {
                            tree_list.DeleteSelectItems();
                        }
                        e.Handled = true;
                    }

                }
                else if (e.Alt)
                {
                    if (e.KeyCode == Keys.Up)
                    {
                        if (select_tag != null)
                        {
                            var tag = tree_list.GetListItemByTmpData(select_tag);
                            var up_tag = tree_list.GetUpItem();

                            if (up_tag == null)
                            {
                            }
                            else
                            {
                                tree_list.MoveItem(tag, up_tag, TreeList.InsertType.UpperInsert);
                                tree_list.is_refresh = true;
                            }
                        }
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.Down)
                    {
                        if (select_tag != null)
                        {
                            var tag = tree_list.GetListItemByTmpData(select_tag);
                            var down_tag = tree_list.GetDownItem();

                            if (down_tag != null)
                            {
                                while (down_tag.parent == tag)
                                {
                                    tree_list.SetSelectItem(down_tag);
                                    down_tag = tree_list.GetDownItem();
                                    if (down_tag == null) break;
                                }
                            }

                            if (down_tag == null)
                            {

                            }
                            else if (down_tag.is_chiled_show && (down_tag.parent != tag) && (down_tag.chiled_list.Count() > 0))
                            {
                                tree_list.MoveItem(tag, down_tag, TreeList.InsertType.InChiled);
                                tree_list.SetSelectItem(tag);
                                tree_list.is_refresh = true;
                            }
                            else
                            {
                                tree_list.MoveItem(tag, down_tag, TreeList.InsertType.UnderInsert);
                                tree_list.SetSelectItem(tag);
                                tree_list.is_refresh = true;
                            }
                        }
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.Left)
                    {
                        if (select_tag != null)
                        {
                            var tag = tree_list.GetListItemByTmpData(select_tag);
                            //var up_tag = tree_list.GetDownItem();
                            var up_tag = tag.parent;

                            if (up_tag == null)
                            {
                                if (tree_list.items.Count() > 0) {
                                    up_tag = tree_list.items[0];
                                    tree_list.MoveItem(tag, up_tag, TreeList.InsertType.UpperInsert);
                                    tree_list.SetSelectItem(tag);
                                    tree_list.is_refresh = true;
                                }
                            }
                            else
                            {
                                tree_list.MoveItem(tag, up_tag, TreeList.InsertType.UpperInsert);
                                tree_list.SetSelectItem(tag);
                                tree_list.is_refresh = true;
                            }
                        }
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.Right)
                    {
                        if (select_tag != null)
                        {
                            var tag = tree_list.GetListItemByTmpData(select_tag);
                            var up_tag = tree_list.GetUpItem();

                            if (up_tag == null)
                            {
                                //if (tree_list.items.Count() > 0)
                                //{
                                //    up_tag = tree_list.items[0];
                                //    tree_list.MoveItem(tag, up_tag, TreeList.InsertType.UpperInsert);
                                //    tree_list.SetSelectItem(tag);
                                //    tree_list.is_refresh = true;
                                //}
                            }
                            else
                            {
                                tree_list.MoveItem(tag, up_tag, TreeList.InsertType.InChiled);
                                tree_list.SetViewChiledItems(up_tag, true);
                                tree_list.SetSelectItem(tag);
                                tree_list.is_refresh = true;
                            }
                        }
                        e.Handled = true;
                    }

                }
                else
                {
                    if (tree_list.is_key_input_box_use == true)
                    {

                    }
                    else if (e.KeyCode == Keys.T)
                    {
                        thumbnail_view.SetIsShowTagList(true != thumbnail_view.GetIsShowTagList());
                        thumbnail_view.Refresh();
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.W)
                    {
                        var li = tree_list.GetUpItem();
                        if (li != null)
                        {
                            tree_list.SetSelectItem(li);
                            select_tag = (ImageLibrary.Tag)li.tmp_data;
                            thumbnail_view.SetSelectTag(select_tag);


                        }
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.S)
                    {
                        var li = tree_list.GetDownItem();
                        if (li != null)
                        {
                            tree_list.SetSelectItem(li);
                            select_tag = (ImageLibrary.Tag)li.tmp_data;
                            thumbnail_view.SetSelectTag(select_tag);
                        }
                        e.Handled = true; 

                    }
                    else if (e.KeyCode == Keys.D)
                    {
                        var li = tree_list.GetListItemByTmpData(select_tag);
                        if (li != null)
                        {
                            tree_list.SetViewChiledItems(li, true);
                            tree_list.UpdateListSourtAsParent();
                            tree_list.is_refresh = true;
                        }
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.A)
                    {
                        var li = tree_list.GetListItemByTmpData(select_tag);
                        if (li != null)
                        {
                            tree_list.SetViewChiledItems(li, false);
                            tree_list.UpdateListSourtAsParent();
                            tree_list.is_refresh = true;
                        }
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.Q)
                    {
                        if (select_tag != null && select_tag.parent != null)
                        {
                            var li = tree_list.GetListItemByTmpData(select_tag.parent);
                            tree_list.SetSelectItem(li);
                            select_tag = select_tag.parent;
                            thumbnail_view.SetSelectTag(select_tag);
                        }
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.F5)
                    {
                        SetSelectTagByTreeListTagOption();
                    }

                    if (DebugConfig)
                    {
                        if (e.KeyCode == Keys.F3)
                        {
                            // todo : 開発用
                            //var ed = new ExportData.ExportCVData();
                            //ed.ExportPositive(select_tag);
                        }
                        else if (e.KeyCode == Keys.F4)
                        {
                            // todo : 開発用
                            var ed = new ExportData.ExportCVData();
                            ed.ExportNegative(select_tag);
                        }
                        else if (e.KeyCode == Keys.F6)
                        {
                            var ed = new ExportData.ExportCVData();
                            ed.ExportGANImage(select_tag);
                        }
                        else if (e.KeyCode == Keys.F7)
                        {
                            var ed = new ExportData.ExportCVData();
                            ed.ExportCNNImageTagSet(select_tag);
                        }
                        else if (e.KeyCode == Keys.F8)
                        {
                            var ed = new ExportData.ExportCVData();
                            ed.ExportTagImages(select_tag);
                        }
                        else if (e.KeyCode == Keys.Escape)
                        {
                        }
                    }

                }

            }

        }

        private void Form_Move(object sender, EventArgs e)
        {
            // 最大化だと位置が(0,0)となるので回避する
            // 最小化だと位置が不定になるので回避する
            if ( (form.WindowState != FormWindowState.Maximized) && (form.WindowState != FormWindowState.Minimized))
            {
                config.auto_save_config.window_position.X = form.Location.X;
                config.auto_save_config.window_position.Y = form.Location.Y;
                config.auto_save_config.Save();
            }
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            if (is_setup_layout) return;

            if (form.WindowState == FormWindowState.Minimized)
            {

            }
            else
            {
                Form_Resize();

                if (form.WindowState == FormWindowState.Maximized)
                {
                    var sd = Blastagon.UI.Common.Common.ScreenCheck(form);

                    config.auto_save_config.window_size_maximum = true;
                    config.auto_save_config.Save();
                }
                else
                {
                    config.auto_save_config.window_size = new Size(form.Size.Width, form.Size.Height);
                    config.auto_save_config.main_drag_bar_v_position_x = main_drag_bar_v.Location.X;
                    config.auto_save_config.window_size_maximum = false;
                    config.auto_save_config.Save();
                }
            }
        }

        private void Form_ResizeByDragBar(DragBarV db)
        {
            layout_controller.Main_Update();
            if (form.WindowState != FormWindowState.Maximized)  { 
                config.auto_save_config.main_drag_bar_v_position_x = db.Location.X;
               config.auto_save_config.Save();
            }
        }

        public void Form_Resize()
        {
            if (image_view.is_show)
            {
                // todo : これだと、拡大しても、画面比率に追従してサイズと位置が変わらない…ん～
                image_view.Size = new Size(form.ClientSize.Width, form.ClientSize.Height);
            }

            if (menu_panel.is_show)
            {
                menu_panel.Size = form.ClientRectangle.Size;
            }
            else if (picup_view.is_show)
            {
                picup_view.Size = form.ClientRectangle.Size;
            }
            else
            {
                layout_controller.Main_Update();
            }

        }

        private void SetupLocations( int main_drag_bar_v_x)
        {
            var scroll_bar_width = 24;
            //var side_area_w = 420 + 6;

            var form_client_size = form.ClientSize;
            var form_w = form_client_size.Width;
            var form_h = form_client_size.Height;
            var thumbinail_view_w = main_drag_bar_v_x - scroll_bar_width;

            thumbnail_view.Size = new Size(thumbinail_view_w, form_h);

            main_drag_bar_v.Location = new Point(main_drag_bar_v_x, 0);


            layout_controller.Main_Update();
        }

        private void Thumbnail_view_DragDrop(object sender, DragEventArgs e)
        {
            // ドラッグ＆ドロップされたファイル
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);


            progress_panel = new ProgressPanel(form, "ファイルを読み込み中です\nタグが埋め込まれているか確認するため、時間がかかります");
            form.Controls.Add(progress_panel);
            progress_panel.BringToFront();

            SaveLoadManager.LoadImageByDrop(files);

            //UpdateTreeListByImageLibraryTag();
            //tree_list.Refresh();
        }

        private void Thumbnail_view_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // ドラッグ中のファイルやディレクトリの取得
                string[] drags = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string d in drags)
                {
                    if (!(System.IO.File.Exists(d)|| System.IO.Directory.Exists(d)))
                    {
                        // ファイルまたはフォルダではない
                        return;
                    }
                }
                e.Effect = DragDropEffects.Copy;
            }
        }


        private void Form_MouseWheel(object sender, MouseEventArgs e)
        {

            if (image_view.is_show)
            {
                int numberOfTextLinesToMove = e.Delta * SystemInformation.MouseWheelScrollLines / 360;
                
                var scale = 1.1;
                if (numberOfTextLinesToMove > 0)
                {
                    scale = Math.Pow(scale, numberOfTextLinesToMove);
                }
                else
                {
                    scale = Math.Pow(scale, numberOfTextLinesToMove);

                }

                image_view.UpdateScaleAndOffset(image_view.scale * scale);
                image_view.Refresh();
            }
            else if ( picup_view.is_show)
            {
                int numberOfTextLinesToMove = e.Delta * SystemInformation.MouseWheelScrollLines / 360;
                picup_view._MouseWheel(numberOfTextLinesToMove);
            }
            else
            {
                if ((tree_list.Location.X <= e.X) && (e.X <= tree_list.Location.X + tree_list.Size.Width) &&
                    (tree_list.Location.Y <= e.Y) && (e.Y <= tree_list.Location.Y + tree_list.Size.Height))
                {
                    tree_list._MouseWheel(sender, e);
                }
                else
                {
                    thumbnail_view._MouseWheel(sender, e);
                }

            }
        }

        public void _DebugInit()
        {
            TextBox text_box;
            text_box = new TextBox();
            buttons_panel.Controls.Add(text_box);
            buttons_panel.BackColor = Color.FromArgb(120, 120, 120);
            text_box.Location = new Point(0, 2);
            text_box.Size = new Size(400-40, 32);
            text_box.Text = "検索してください";
            text_box.Font = new Font("メイリオ", 13, FontStyle.Regular);
            text_box.ImeMode = ImeMode.NoControl;
            //text_box.Leave += (s, e) =>
            //{
            //    _EditTagName_Fix(si, text_box_name_change.Text);
            //};
            text_box.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    SearchTagByTreeList(text_box.Text);
                    // 無効音防止
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    // フォーカスを外す
                    text_box.Enabled = false;
                    text_box.Enabled = true;

                    // 無効音防止
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }

            };
            text_box_tag_search = text_box;

            var y_offset = 34;
            System.Windows.Forms.Control btn;
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 400-40, 0, 60, 36, "");
            btn.BackgroundImage = UserInterfaceCommon.BitmapFromFile(@"data/Eldorado mini - search.png", 30, 30, 1.0, true, Color.FromArgb(255, 255, 255));
            btn.Click += (s, e) =>
            {
                var text = text_box.Text;
                SearchTagByTreeList(text);
            };
            btn.Focus();
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 0, y_offset + 30 * 0, 180, 30, "タグを全て表示");
            btn.Click += (s, e) =>
            {
                tree_list.SetTagViewMode(null, TreeList.SetTagViewModeType.ShowAll);
                tree_list.Refresh();
                ResetFocus();
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 180, y_offset + 30 * 0, 180, 30, "サムネイルを更新");
            btn.Click += (s, e) =>
            {
                SetSelectTagByTreeListTagOption();
                ResetFocus();
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 0, y_offset + 30 * 1, 180, 30, "サムネイルにタグ表示");
            btn.Click += (s, e) =>
            {
                thumbnail_view.SetIsShowTagList(true != thumbnail_view.GetIsShowTagList());
                thumbnail_view.Refresh();
                ResetFocus();
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 180, y_offset + 30 * 1, 180, 30, "ログを消去");
            btn.Click += (s, e) =>
            {
                popup_log.Clear();
                ResetFocus();
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 0, y_offset + 30 * 2, 180, 60, "管理データを保存");
            btn.Click += (s, e) =>
            {
                SaveImageLibrary();
                ResetFocus();
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 0, y_offset + 30 * 4, 180, 30, "ピックアップ");
            btn.Click += (s, e) =>
            {
                if (picup_view.is_show)
                {
                    picup_view.Hide();
                    picup_view.is_show = false;
                }
                else
                {
                    picup_view.Size = form.ClientSize;
                    picup_view.Show();
                    picup_view.is_show = true;
                }
                ResetFocus();
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 180, y_offset + 30 * 2, 180, 30, "管理データを最適化");
            btn.Click += (s, e) =>
            {
                // ・タグを最適化（詳細タグがある場合、上のタグを外す）
                // ・存在しない管理データを削除
                // ・重複しているタグの削除

                Optimization(true, true);
                SetSelectTagByTreeListTagOption();
                image_library.UpdateTagStatus();
                tree_list.Refresh();
                ResetFocus();
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 180, y_offset + 30 * 3, 180, 30, "メニュー");
            btn.Click += (s, e) =>
            {
                menu_panel.ShowEx();
                ResetFocus();
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 360, y_offset + 30 * 0, 60, 30, "列1");
            btn.Click += (s, e) =>
            {
                var num = 1;
                config.auto_save_config.thumbnail_view_line_num= num;
                config.auto_save_config.Save();
                thumbnail_view.SetLineNum(num);
                ResetFocus();
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 360, y_offset + 30 * 1, 60, 30, "列2");
            btn.Click += (s, e) =>
            {
                var num = 2;
                config.auto_save_config.thumbnail_view_line_num = num;
                config.auto_save_config.Save();
                thumbnail_view.SetLineNum(num);
                ResetFocus();
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 360, y_offset + 30 * 2, 60, 30, "列3");
            btn.Click += (s, e) =>
            {
                var num = 3;
                config.auto_save_config.thumbnail_view_line_num = num;
                config.auto_save_config.Save();
                thumbnail_view.SetLineNum(num);
                ResetFocus();
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 360, y_offset + 30 * 3, 60, 30, "列4");
            btn.Click += (s, e) =>
            {
                var num = 4;
                config.auto_save_config.thumbnail_view_line_num = num;
                config.auto_save_config.Save();
                thumbnail_view.SetLineNum(num);
                ResetFocus();
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 360, y_offset + 30 * 4, 60, 30, "列5");
            btn.Click += (s, e) =>
            {
                var num = 5;
                config.auto_save_config.thumbnail_view_line_num = num;
                config.auto_save_config.Save();
                thumbnail_view.SetLineNum(num);
                ResetFocus();
            };
            btn = UserInterfaceCommon.CreateButton(new BButton(), buttons_panel, 360, y_offset + 30 * 5, 60, 30, "列7");
            btn.Click += (s, e) =>
            {
                var num = 7;
                config.auto_save_config.thumbnail_view_line_num = num;
                config.auto_save_config.Save();
                thumbnail_view.SetLineNum(num);
                ResetFocus();
            };

            layout_controller.Main_InitButtonPanel(420, 220);

            LoadImageLibrary();
            tree_list.UpdateTagViewStatus();


            thumbnail_view._UpdatePosThumbnail();

        }

        public ImageLibrary.Tag CreateTag( string name, ImageLibrary.Tag up_item )
        {
            var up_list_item = tree_list.GetListItemByTmpData(up_item);
            var i = tree_list.AddItem( name, up_list_item);
            var tag = new ImageLibrary.Tag();
            tag.name = name;
            i.tmp_data = tag;
            image_library.tags.Add(tag);

            if (up_item!=null)
            {
                tag.parent = up_item.parent;
                if (up_item.parent != null)
                {
                    tag.parent.chiled.Add(tag);
                }
            }
            
            tree_list.is_refresh = true;
            return tag;
        }

        // フルパスで階層化されたタグを生成する \: が区切り
        public ImageLibrary.Tag CreateTagByFullName(string full_name)
        {
            var split_names = full_name.Replace(@"\:","\t").Split('\t');
            var tag = (ImageLibrary.Tag)null;
            foreach ( var split_name in split_names)
            {
                if (tag == null)
                {
                    tag = image_library.GetTag(split_name);
                    if (tag == null)
                    {
                        tag = CreateTag(split_name, null);
                    }
                }
                else
                {
                    var tag2 = (ImageLibrary.Tag)null;
                    foreach ( var chiled_tag in tag.chiled)
                    {
                        if(chiled_tag.name== split_name)
                        {
                            tag2 = chiled_tag;
                            break;
                        }
                    }
                    if(tag2==null)
                    {
                        //tag2 = CreateTag(split_name, tag);
                        var parent_list_item = tree_list.GetListItemByTmpData(tag);
                        //var i = tree_list.AddItem(split_name, parent_list_item);
                        var i = tree_list.AddItemToParent(split_name, parent_list_item);
                        //parent_list_item.chiled_list.Add(i);
                        //i.parent = parent_list_item;

                        tag2 = new ImageLibrary.Tag();
                        tag2.name = split_name;
                        tag2.parent = tag;
                        i.tmp_data = tag2;
                        tag.chiled.Add(tag2);
                        image_library.tags.Add(tag2);
                    }

                    tag = tag2;
                }
            }

            return tag;
        }

        public void DeleteTag(ImageLibrary.Tag tag)
        {
            //foreach( var t in tag.chiled )
            //{
            //    DeleteTag(t);
            //}
            while (tag.chiled.Count() > 0) {
                DeleteTag(tag.chiled[0]);
            }
            if (tag.parent != null)
            {
                tag.parent.chiled.Remove(tag);
            }

            var li = tree_list.GetListItemByTmpData(tag);
            if (li == null)
            {
                AppCore.core.popup_log.AddMessage("DeleteTag : タグ一覧に指定のタグはありません " + tag.GetFullName());
            }
            else
            {
                tree_list.DeleteItem(li);
            }

            image_library.tags.Remove(tag);

            image_library.image_datas.Foreach( (id) => {
                foreach (var t in id.Value.tags)
                {
                    if (tag == t.tag)
                    {
                        id.Value.tags.Remove(t);
                        break;
                    }
                }
            });

            tree_list.Refresh();
        }

        public void DeleteTagAll()
        {
            image_library.tags.Clear();
            tree_list.DeleteItemAll();

            var tag_name = SaveLoadManager.NONE_TAG_IMAGE_IN_TAG_NAME;
            var tag = AppCore.core.image_library.GetTag(tag_name);
            tag = AppCore.core.image_library.AddTag(tag_name); // 未登録タグ
            var li = tree_list.AddItem(tag.name, 0);
            li.tmp_data = tag;

            image_library.image_datas.Foreach((id) =>
            {
                id.Value.tags.Clear();
                id.Value.tags.Add( new ImageLibrary.ImageTag(tag, "" ));
            });

            tree_list.Refresh();
        }

        public void SelectTag( ImageLibrary.Tag tag )
        {
            // Todo : 不十分
            if (tag == null)
            {
                select_tag = null;
                thumbnail_view.SetSelectTag(select_tag);
                return;
            }
            else if (select_tag == tag)
            {
                return;
            }
            select_tag = tag;
            thumbnail_view.SetSelectTag(select_tag);
        }

        private void TreeList_SelectItem(TreeList.ListItem li)
        {
            if (li == null)
            {
                select_tag = null;
                thumbnail_view.SetSelectTag(select_tag);
                return;
            }
            else if (select_tag == li.tmp_data)
            {
                return;
            }

            select_tag = (ImageLibrary.Tag)li.tmp_data;

            thumbnail_view.SetSelectTag(select_tag);
        }

        // 表示したいタグと、そこから除外させたいタグを設定する
        // ここは入り口で、スレッドを立てるだけ
        volatile Thread thread_set_select_tag_by_tree_list_tag_option;
        volatile bool is_end_select_tag_by_tree_list_tag_option = false;
        public void SetSelectTagByTreeListTagOption()
        {
            // todo: 2万件ほどの表示のばあい、連続で呼び出されると、ここで待機状態になってもたつく
            var is_loop = true;
            while (is_loop)
            {
                if (thread_set_select_tag_by_tree_list_tag_option==null)
                {
                    is_loop = false;
                    is_end_select_tag_by_tree_list_tag_option = false;
                    thread_set_select_tag_by_tree_list_tag_option = new Thread(new ThreadStart(
                        () =>
                        {
                            SetSelectTagByTreeListTagOption_Main();
                            thread_set_select_tag_by_tree_list_tag_option = null;
                        }
                    ));
                    thread_set_select_tag_by_tree_list_tag_option.Start();
                    break;
                }
                is_end_select_tag_by_tree_list_tag_option = true;
            }
        }
        public void SetSelectTagByTreeListTagOption_Main()
        {
            var star_tags      = new List<ImageLibrary.Tag>();
            var show_tags      = new List<ImageLibrary.Tag>();
            var exclusion_tags = new List<ImageLibrary.Tag>();
            tree_list.ForeachItem( (li)=> {
                var tag = (ImageLibrary.Tag)li.tmp_data;
                if (tag.is_show) show_tags.Add(tag);
                if (tag.show_mode == ImageLibrary.Tag.ShowMode.Exclusion) exclusion_tags.Add(tag);
                if (tag.show_mode == ImageLibrary.Tag.ShowMode.Star)      star_tags.Add(tag);
                return false;
            });

            var show_images = new List<ImageLibrary.ImageDataExWordSet>();
            if (star_tags.Count == 0)
            {
                //foreach (var i in image_library.image_datas)

                image_library.image_datas.Foreach((id) =>
                {
                    foreach (var tag in id.Value.tags)
                    {
                        var is_ok = false;
                        var ex_word = "";
                        foreach (var tag2 in show_tags)
                        {
                            if (tag.tag == tag2)
                            {
                                is_ok = true;
                                ex_word = tag.ex_word;
                                show_images.Add(new ImageLibrary.ImageDataExWordSet(id.Value, ex_word));
                                //break;
                            }
                        }

                        //if (is_ok)
                        //{
                        //    break;
                        //}
                        //var is_ok = false;
                        //var ex_word = "";
                        //foreach (var tag2 in show_tags)
                        //{
                        //    if (tag.tag == tag2)
                        //    {
                        //        is_ok = true;
                        //        ex_word = tag.ex_word;
                        //        break;
                        //    }
                        //}

                        //if (is_ok)
                        //{
                        //    show_images.Add(new ImageLibrary.ImageDataExWordSet(id.Value, ex_word));
                        //    break;
                        //}
                    }
                    if (is_end_select_tag_by_tree_list_tag_option) return; // 中断
                });
            }
            else
            {
                // 子から星の始まりの親を紐付ける（key==valueのときあり）
                var star_link_chiled_to_top = new Dictionary<ImageLibrary.Tag, ImageLibrary.Tag>();
                foreach( var tag in show_tags)
                {
                    var tmp_tag = tag;
                    while (tmp_tag.show_mode != ImageLibrary.Tag.ShowMode.Star)
                    {
                        tmp_tag = tmp_tag.parent;
                        if (tmp_tag == null) break; // 何故かnullがかえる？
                    }

                    if(tmp_tag!=null) star_link_chiled_to_top.Add(tag, tmp_tag);
                }

                // 星ごとに、子をふくめて表示できそうなimage_dataのリストをつくる
                var stars_show_images = new Dictionary<ImageLibrary.Tag, List<ImageLibrary.ImageDataExWordSet>>();

                foreach (var star_tag in star_tags)
                {
                    var star_show_images = new List<ImageLibrary.ImageDataExWordSet>();
                    stars_show_images.Add(star_tag, star_show_images);
                }

                //foreach (var i in image_library.image_datas)

                image_library.image_datas.Foreach((id) =>
                {
                    foreach (var tag in id.Value.tags)
                    {
                        if (star_link_chiled_to_top.ContainsKey(tag.tag))
                        {
                            var star_tag = star_link_chiled_to_top[tag.tag];
                            var star_show_images = stars_show_images[star_tag];
                            star_show_images.Add(new ImageLibrary.ImageDataExWordSet(id.Value,tag.ex_word));
                        }
                    }
                });

                // 星ごとのリストが完成したので、それらをAND合成する
                // つまり、全ての星に入っているimage_dataのみ、有効とする
                var tmp_show_images = new List < ImageLibrary.ImageDataExWordSet> ();
                {
                    var star_show_images = stars_show_images[star_tags[0]];
                    foreach (var i in star_show_images) {
                        tmp_show_images.Add(i);
                    }
                }
                for ( var j=1; j< star_tags.Count; j++ )
                {
                    var star_show_images = stars_show_images[star_tags[j]];
                    var delete_images = new List<ImageLibrary.ImageDataExWordSet>();

                    foreach ( var i in tmp_show_images)
                    {
                        var is_ok = false;
                        foreach( var i2 in star_show_images)
                        {
                            if ( ( i.image_data==i2.image_data) && ( i.ex_word==i2.ex_word) )
                            {
                                is_ok = true;
                                break;
                            }
                        }
                        if(!is_ok)
                        {
                            // 発見できない → 削除
                            delete_images.Add(i);
                        }
                        //if (star_show_images.IndexOf(i) < 0 ) 
                        //{
                        //    // 発見できない → 削除
                        //    delete_images.Add(i);
                        //}
                    }
                    foreach (var i in delete_images)
                    {
                        tmp_show_images.Remove(i);
                    }
                }

                // 結果の代入
                foreach (var i in tmp_show_images)
                {
                    if (show_images.IndexOf(i) < 0) // 重複している場合があるので
                    {
                        show_images.Add(i);
                    }
                }


            }

            // 除外
            var exclusion_images = new List<ImageLibrary.ImageDataExWordSet>();

            if (exclusion_tags.Count > 0)
            {
                image_library.image_datas.Foreach((id) =>
                {
                    foreach (var tag in id.Value.tags)
                    {
                        var is_ok = false;
                        var ex_word = "";
                        foreach (var tag2 in exclusion_tags)
                        {
                            if (tag.tag == tag2)
                            {
                                is_ok = true;
                                ex_word = tag.ex_word;
                                break;
                            }
                        }

                        if (is_ok)
                        {
                            exclusion_images.Add( new ImageLibrary.ImageDataExWordSet(id.Value,ex_word));
                            break;
                        }
                    }
                });

                // todo: ex_word拡張した後で除去の挙動が安定しているか → 仕様が不安定…クリップしたものを、クリップやクリップなしのタグで除外できるようにすべきかが明瞭でない
                //      ひとまず、ex_word==""の除去は、ex_wordが一致せずとも除去するようにする
                foreach (var i in exclusion_images)
                {
                    var delete_images = new List<ImageLibrary.ImageDataExWordSet>();
                    foreach (var i2 in show_images)
                    {
                        if ( i2.image_data==i.image_data ) 
                        {
                            if ((i.ex_word == "") || (i2.ex_word == i.ex_word)) // i.ex_word == "" は、クリップしたものにも適応する…このへんは仕様として混乱するけど
                            {
                                delete_images.Add(i2);
                            }
                            //show_images.Remove(i2); // 複数ある可能性があるので…breakできない
                        }
                    }
                    foreach(var i2 in delete_images)
                    {
                        show_images.Remove(i2);
                    }
                }
            }

            // 重複を削除
            {
                var delete_images = new List<ImageLibrary.ImageDataExWordSet>();
                foreach(var i in show_images)
                {
                    if (delete_images.Exists((t) => { // 既に削除リストにはいっているものは検査しない（残したい方も消してしまうので）
                        if (t == i) return true;
                        return false;
                    }))
                    {
                        continue;
                    }

                    foreach (var i2 in show_images)
                    {
                        if (i == i2) continue;
                        if (i.image_data==i2.image_data)
                        {
                            if (i.ex_word==i2.ex_word)
                            {
                                delete_images.Add(i2);
                            }
                        }
                    }
                }

                foreach( var i in delete_images)
                {
                    show_images.Remove(i);
                }
            }


            thumbnail_view.ClearAndAddImages(show_images);
            thumbnail_view.is_reflesh_body = true;
        }

        public void ShowThubnail( ImageLibrary.Tag show_tag, bool is_group_tag )
        {
            thumbnail_view.ClearImages();

            //foreach (var i in image_library.image_datas)
            image_library.image_datas.Foreach((id) =>
            {
                var is_ok_add = false;
                var ex_word = "";
                if (is_group_tag)
                {
                    foreach (var tag in id.Value.tags)
                    {
                        var is_ok = show_tag == tag.tag;
                        if (!is_ok) is_ok = ImageLibrary.Tag.CheckGroupTag(show_tag, tag.tag) != null;
                        if (is_ok)
                        {
                            is_ok_add = true;
                            ex_word = tag.ex_word;
                            break;
                        }
                    }
                }
                else
                {
                    var is_no_add = false;
                    foreach (var tag in id.Value.tags)
                    {
                        var is_ok = show_tag == tag.tag;
                        var is_no = ImageLibrary.Tag.CheckGroupTag(show_tag, tag.tag) != null;
                        if (is_no) is_no_add = true;
                        if (is_ok)
                        {
                            is_ok_add = true;
                            ex_word = tag.ex_word;
                        }
                    }
                    if (is_no_add) is_ok_add = false;
                }

                if (is_ok_add)
                {
                    thumbnail_view.AddImages( new ImageLibrary.ImageDataExWordSet(id.Value,ex_word));
                    //var library_tag = image_library.GetTag(tag_name);
                }
            });
            thumbnail_view._UpdatePosThumbnail();
            thumbnail_view.SetScrollValue(0, true);

        }

        // ピンがとめらた画像を表示する
        public void ShowThubnailPin()
        {
            thumbnail_view.ClearImages();

            //foreach (var i in image_library.image_datas)
            image_library.image_datas_pin.Foreach((id) =>
            {
                thumbnail_view.AddImages(id);
            });
            thumbnail_view._UpdatePosThumbnail();
            thumbnail_view.SetScrollValue(0, true);

        }

        private void TreeList_EditName(TreeList.ListItem li, string old_name)
        {
            var tmp_data = (ImageLibrary.Tag)li.tmp_data;

            // todo 現状、タグの階層化に未対応だからこれでいい、階層化をすると名前のフルパスを取得する必用がある
            // また、影響範囲が子供のタグのデータにもひろがるので、対応範囲が広がる…
            // 変更前後の１セットデータを配列にしていっきに処理したほうがいいかもしれない
            var old_full_name = old_name;
            var new_full_name = li.text;

            image_library.image_datas.Foreach((id) =>
            {
                foreach (var tag in id.Value.tags)
                {
                    if (tag.tag.name == old_full_name)
                    {
                        tag.tag.name = new_full_name;
                    }
                }
            });

            tmp_data.name = li.text;

        }

        private void TreeList_ChangeItemParent( TreeList.ListItem li, TreeList.ListItem new_parent, TreeList.ListItem old_parent)
        {
            var tmp_data     = (ImageLibrary.Tag)li.tmp_data;
            var new_parent_data = (ImageLibrary.Tag)null;
            var old_parent_data = (ImageLibrary.Tag)null;

            if (new_parent != null) new_parent_data = (ImageLibrary.Tag)new_parent.tmp_data;
            if (old_parent != null) old_parent_data = (ImageLibrary.Tag)old_parent.tmp_data;

            image_library.ChangeTagParent(tmp_data, new_parent_data, old_parent_data);
        }

        public void ThumbnailView_SelectImage( ThumbnailImage ti )
        {
            if (select_tag != null)
            {
                if (ti.is_tag_in)
                {
                    ti.data.image_data.tags.Add(new ImageLibrary.ImageTag(select_tag,""));
                }
                else
                {
                    foreach ( var tag in ti.data.image_data.tags )
                    {
                        if (tag.tag.name==select_tag.name)
                        {
                            ti.data.image_data.tags.Remove(tag);
                            break;
                        }
                    }
                }
                image_library.UpdateTagStatus(); // Todo : この処理が重い
                //tree_list.Refresh();
                tree_list.is_refresh = true;
            }
        }

        public void ThumbnailView_SelectClipImage(ThumbnailImage ti, Rectangle rect)
        {
            if (select_tag != null)
            {
                var ex_word = ImageLibrary.ImageTag.GetExWordByClip(rect);
                var tt = new ImageLibrary.ImageTag(select_tag, ex_word);
                ti.data.image_data.tags.Add(tt);
                ti.in_clip_tags.Add(tt);

                image_library.UpdateTagStatus();
                tree_list.Refresh();
            }

        }

        public void SaveImageLibrary()
        {
            { // 核となるデータの保存
                var file_path = "dox";

                //ファイルを上書きし、UTF-8で書き込む
                using (var sw = new System.IO.StreamWriter(
                    file_path,
                    false,
                    System.Text.Encoding.GetEncoding("UTF-8")))
                {
                    var counter = 0;
                    var counter_max = image_library.image_datas.Count();

                    image_library.image_datas.Foreach((id) =>
                    {
                        var file_path_b = StringBase64.ToBase64(id.Value.file_path);
                        var tag = "";
                        foreach (var t in id.Value.tags)
                        {
                            tag += t.tag.GetFullName() + @"\," + t.ex_word + "\n";
                        }
                        var tag_b = StringBase64.ToBase64(tag);

                        var w = id.Value.size.Width;
                        var h = id.Value.size.Height;
                        if (w == 0) // 画像のサイズ取得漏れがあった場合
                        {
                            if (File.Exists(id.Value.file_path))
                            {
                                var plugin = FileConectorManager.GetFileConector(id.Value.file_path, false);
                                using (var image = plugin.image_conector.FromFile(null, id.Value.file_path, ""))
                                {
                                    id.Value.size.Width = image.Width;
                                    id.Value.size.Height = image.Height;
                                    w = id.Value.size.Width;
                                    h = id.Value.size.Height;
                                }
                            }
                        }
                        sw.WriteLine("{0},{1},{2},{3}", file_path_b, w, h, tag_b);

                        counter++;
                        form.Text = Title + " " + counter + " / " + counter_max;
                    });
                }
                form.Text = Title;
            }

            { // タグの状態を保存
                var file_path = "doi";

                // タグのツリー上の並びを保存したいので、その対応から進める
                var tags = new List<ImageLibrary.Tag>();
                tree_list.ForeachItem((e) => {
                    tags.Add((ImageLibrary.Tag)e.tmp_data);
                    return false;
                });


                //ファイルを上書きし、UTF-8で書き込む
                using (var sw = new System.IO.StreamWriter(
                    file_path,
                    false,
                    System.Text.Encoding.GetEncoding("UTF-8")))
                {
                    foreach (var tag in tags)
                    {
                        var tag_full_name = tag.GetFullName();
                        var li = tree_list.GetListItemByTmpData(tag);

                        var tag_full_name_b= StringBase64.ToBase64(tag_full_name);
                        var chiled_show = "ON";

                        var show_mode = "";
                        switch (tag.show_mode)
                        {
                            case ImageLibrary.Tag.ShowMode.Show:       show_mode = "Show";       break;
                            case ImageLibrary.Tag.ShowMode.ShowChiled: show_mode = "ShowChiled"; break;
                            case ImageLibrary.Tag.ShowMode.Hide:       show_mode = "Hide";       break;
                            case ImageLibrary.Tag.ShowMode.Exclusion:  show_mode = "Exclusion";  break;
                            case ImageLibrary.Tag.ShowMode.Star:       show_mode = "Star";       break;
                        }

                        if (!li.is_chiled_show) chiled_show = "OFF";

                        sw.WriteLine("{0},{1},{2}", tag_full_name_b, chiled_show, show_mode);
                    }
                }
            }
        }

        public void LoadImageLibrary()
        {

            var tree_list_tags_open_status = new Dictionary<ImageLibrary.Tag, bool>();
            {
                var file_path = "doi";

                if (System.IO.File.Exists(file_path))
                {
                    using (var file = new System.IO.StreamReader(file_path))
                    {
                        string line;
                        while ((line = file.ReadLine()) != null)
                        {
                            string[] s = line.Split(',');

                            //var tag_full_name = StringBase64.ToUTF8(s[0]);
                            var tag_word = StringBase64.ToUTF8(s[0]);
                            var tag_word_split = tag_word.Replace(@"\,", "\t").Split('\t');
                            var tag_full_name = tag_word_split[0];
                            var tag_ex_word = ""; if (tag_word_split.Count()>1) tag_ex_word = tag_word_split[1];

                            var is_chiled_show = s[1] == "ON";
                            var tag = image_library.GetTag(tag_full_name);

                            if (tag == null)
                            {
                                tag = AppCore.core.image_library.AddTag(tag_full_name);
                            }

                            if (s.Count() > 2)
                            {
                                var show_mode = s[2];
                                switch (show_mode)
                                {
                                    case "Show": tag.show_mode = ImageLibrary.Tag.ShowMode.Show; break;
                                    case "ShowChiled": tag.show_mode = ImageLibrary.Tag.ShowMode.ShowChiled; break;
                                    case "Hide": tag.show_mode = ImageLibrary.Tag.ShowMode.Hide; break;
                                    case "Exclusion": tag.show_mode = ImageLibrary.Tag.ShowMode.Exclusion; break;
                                    case "Star": tag.show_mode = ImageLibrary.Tag.ShowMode.Star; break;
                                }
                            }

                            if (tree_list_tags_open_status.ContainsKey(tag))
                            {
                                // Todo : ここにはいってしまうことがある？重複して保存している？
                            }
                            else
                            {
                                tree_list_tags_open_status.Add(tag, is_chiled_show);
                            }
                            //var li = tree_list.GetListItemByTmpData(tag);
                            //if (li != null)
                            //{
                            //    li.is_chiled_show = is_chiled_show;
                            //}

                        }
                    }
                    tree_list.UpdateListSourtAsParent();
                }
            }

            {
                var file_path = "dox";

                if (System.IO.File.Exists(file_path) == false) return;

                using (var file = new System.IO.StreamReader(file_path))
                {
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        string[] s = line.Split(',');

                        var image_file_path = StringBase64.ToUTF8(s[0]);
                        var image_data = new ImageLibrary.ImageData();
                        image_data.file_path = image_file_path;
                        image_data.size = new Size(int.Parse(s[1]), int.Parse(s[2]));

                        var tag_names = StringBase64.ToUTF8(s[3]).Split('\n');

                        foreach (var tag_name_src in tag_names)
                        {
                            if (tag_name_src == "") continue;
                            var tag_name_src_split = tag_name_src.Replace(@"\,","\t").Split('\t');
                            var tag_name = tag_name_src_split[0];
                            var ex_word = "";
                            if (tag_name_src_split.Count() > 1) ex_word = tag_name_src_split[1];

                            var library_tag = image_library.GetTag(tag_name);
                            if (library_tag == null)
                            {
                                library_tag = image_library.AddTag(tag_name);
                            }
                            image_data.tags.Add(new ImageLibrary.ImageTag(library_tag, ex_word));
                        }

                        // タグが無い場合、「未登録」タグに、タグを入れる
                        if (image_data.tags.Count == 0)
                        {
                            var tag_name = SaveLoadManager.NONE_TAG_IMAGE_IN_TAG_NAME;
                            var library_tag = AppCore.core.image_library.GetTag(tag_name);
                            if (library_tag == null)
                            {
                                library_tag = AppCore.core.image_library.AddTag(tag_name);
                            }
                            image_data.tags.Add(new ImageLibrary.ImageTag(library_tag,""));
                        }

                        image_library.image_datas.Add(image_file_path, image_data);
                    }
                }
            }
            

            // tree_listの更新、途中でやるとややこしいので…
            UpdateTreeListByImageLibraryTag();
            foreach( var i in tree_list_tags_open_status)
            {
                var li = tree_list.GetListItemByTmpData(i.Key);

                if (li != null)
                {
                    li.is_chiled_show = i.Value;
                }

            }
            tree_list.UpdateListSourtAsParent();
            tree_list.UpdateScrollBarMax();

        }

        // image_libraryのtagを元に、tree_listを更新(追加)する、強化版
        // 一旦、tree_listのタグを削除するので遅い
        public void UpdateTreeListByImageLibraryTagForce()
        {
            tree_list.is_refresh = false;
            tree_list.is_repaint_cancel = true;

            var tmp_list = new Dictionary<object, TreeList.ListItem>();
            foreach( var li in tree_list.items)
            {
                tmp_list.Add(li.tmp_data, li);
            }
            //tree_list.DeleteItemAll();
            tree_list.items.Clear();

            //UpdateTreeListByImageLibraryTag();
            foreach (var tag in image_library.tags)
            {
                if (tag.parent == null)
                {
                    var li = tree_list.AddItem(tag.name, 0, false);
                    li.tmp_data = tag;
                    UpdateTreeListByImageLibraryTagForce_ChiledTagUpdate(li, tag, 1);
                }
            }

            foreach (var li in tree_list.items)
            {
                if (tmp_list.ContainsKey(li.tmp_data)) {
                    var li2 = tmp_list[li.tmp_data];
                    li.is_chiled_show = li2.is_chiled_show;
                }
            }

            tree_list.UpdateScrollBarMax();
            if (image_library.tags.Count() > 0) // ツリーの開閉をもとに戻すために開閉処理をやる
            {
                var target_li = tree_list.GetListItemByTmpData(image_library.tags[0]);
                var tmp_is = target_li.is_chiled_show;
                tree_list.SetViewChiledItems(target_li, !tmp_is);
                tree_list.SetViewChiledItems(target_li, tmp_is);
            }

            tree_list.UpdateListSourtAsParent();

            tree_list.is_repaint_cancel = false;
            tree_list.is_refresh = true;

            //TreeListReflesh();


        }

        private void UpdateTreeListByImageLibraryTagForce_ChiledTagUpdate(TreeList.ListItem paretn_list_item, ImageLibrary.Tag parent_tag, int depth)
        {
            foreach (var tag in parent_tag.chiled)
            {
                var li = tree_list.AddItem(tag.name, depth, false);
                li.tmp_data = tag;
                li.parent = paretn_list_item;
                paretn_list_item.chiled_list.Add(li);
                UpdateTreeListByImageLibraryTagForce_ChiledTagUpdate(li, tag, depth + 1);
                //Console.WriteLine(tag.GetFullName());
            }
        }

        // image_libraryのtagを元に、tree_listを更新(追加)する
        public void UpdateTreeListByImageLibraryTag()
        {
            foreach( var tag in image_library.tags)
            {
                if (tag.parent == null)
                {
                    var li = tree_list.GetListItemByTmpData(tag);
                    if (li == null)
                    {
                        li = tree_list.AddItem(tag.name, 0, false);
                        li.tmp_data = tag;
                        UpdateTreeListByImageLibraryTag_ChiledTagUpdate(li, tag, 1);
                    }
                }
            }
            //image_library.tags.Foreach((tag) =>
            //{
            //    if (tag.parent == null)
            //    {
            //        var li = tree_list.GetListItemByTmpData(tag);
            //        if (li == null)
            //        {
            //            li = tree_list.AddItem(tag.name, 0, false);
            //            li.tmp_data = tag;
            //            UpdateTreeListByImageLibraryTag_ChiledTagUpdate(li, tag, 1);
            //        }
            //    }
            //});
        }

        private void UpdateTreeListByImageLibraryTag_ChiledTagUpdate(TreeList.ListItem paretn_list_item, ImageLibrary.Tag parent_tag, int depth)
        {
            foreach ( var tag in parent_tag.chiled)
            {
                var li = tree_list.GetListItemByTmpData(tag);
                if (li == null)
                {
                    li = tree_list.AddItem(tag.name, depth, false);
                    li.tmp_data = tag;
                    li.parent = paretn_list_item;
                    paretn_list_item.chiled_list.Add(li);
                    UpdateTreeListByImageLibraryTag_ChiledTagUpdate(li, tag, depth+1);
                }
            }
        }

        // タグの情報を画像ファイルに埋め込む
        public void WriteTagDataInImageFiles()
        {
            var counter = 0;
            var counter_max = image_library.image_datas.Count();

            image_library.image_datas.Foreach((id) =>
            {
                WriteTagDataInImageFiles_Frame(id.Value);
                counter++;
                form.Text = Title + " " + counter + " / " + counter_max;
            });
            form.Text = Title;
        }

        public void WriteTagDataInImageFiles_Frame(ImageLibrary.ImageData image_data)
        {
            if (!System.IO.File.Exists(image_data.file_path))
            {
                popup_log.AddMessage("ファイルが存在しません : WriteTagDataInImageFiles " + image_data.file_path);
                return;
            }

            // ファイルにタグ情報をよみとって、書き込みが不要かどうか判断する
            var is_need_write = false;
            {
                var plugin = FileConectorManager.GetFileConector(image_data.file_path, true); // ファイルへの書き込みのため、ファイル内容を詳細に確認しておく
                if (plugin==null)
                {
                    popup_log.AddMessage("拡張子とファイル内容が一致しないため、書き込みできません : WriteTagDataInImageFiles " + image_data.file_path);
                    return;
                }

                var old_tags = plugin.tag_conector.Read(image_data.file_path);

                foreach (var tag in old_tags)
                {
                    if (tag.name == "")
                    {
                        old_tags.Remove(tag);
                    }
                }

                if (old_tags.Count == image_data.tags.Count)
                {
                    foreach (var tag in old_tags)
                    {
                        if (tag.name == "") continue;

                        var is_exsist = false;
                        foreach (var tag2 in image_data.tags)
                        {
                            var tag_name = tag2.tag.GetFullName();
                            if ((tag2.tag.GetFullName() == tag.name) && (tag2.ex_word == tag.ex_word))
                            {
                                is_exsist = true;
                                break;
                            }
                        }
                        if (!is_exsist)
                        {
                            is_need_write = true;
                            break;
                        }
                    }
                }
                else
                {
                    is_need_write = true;
                }
            }

            // 書き込み
            if (is_need_write)
            {
                try
                {
                    DateTime dt_update_time = System.IO.File.GetLastWriteTime(image_data.file_path);
                    WriteTagDataInImageFiles_Core(image_data);
                    System.IO.File.SetLastWriteTime(image_data.file_path, dt_update_time);
                }
                catch (System.Exception e)
                {
                    popup_log.AddMessage(e.Message + " --- " + image_data.file_path);
                    //popup_log.AddMessage("タグの情報を埋め込みに失敗 " + id.Value.file_path);
                }
            }

        }

        private void WriteTagDataInImageFiles_Core( ImageLibrary.ImageData image_data )
        {
            //if (image_data.tag.Count() == 0) return; // とりあえず削除

            //var itm = new FileText.ImageTagManager();

            var plugin = FileConectorManager.GetFileConector(image_data.file_path, true); // 
            //var tags = new List<PluginFileConector.Tag>();
            var word = "";
            //var tags = new Tags();
            foreach ( var tag in image_data.tags )
            {
                //word += tag.name + @"\," + tag.good_count + "\n";
                var good_count = 0;
                word += tag.tag.GetFullName() + @"\," + good_count + @"\," + tag.ex_word + "\n";
            }

            try
            {
                plugin.tag_conector.Write(image_data.file_path, word);
            }
            catch(System.Exception e)
            {
                popup_log.AddMessage(e.Message + " " + image_data.file_path);
            }
            
        }

        public static void ViewImage( ImageLibrary.ImageDataExWordSet image_data )
        {
            core.text_box_tag_search.Focus(); // Todo : 強引に不具合対応で、ImaveViewの左右キーがきかなくなる
            //core.ResetFocus();
            core.image_view.ViewImage(image_data, true);
        }

        public static ImageLibrary.ImageDataExWordSet GetNextImageByThumbnailView(ImageLibrary.ImageDataExWordSet image_data_ex_word)
        {
            return core.thumbnail_view.GetNextImage(image_data_ex_word);
        }

        public static ImageLibrary.ImageDataExWordSet GetBackImageByThumbnailView(ImageLibrary.ImageDataExWordSet image_data_ex_word)
        {
            return core.thumbnail_view.GetBackImage(image_data_ex_word);
        }

        public void ShowImageByNoneTag( int count_max )
        {
            thumbnail_view.ClearImages();
            var count = 0;
            image_library.image_datas.ForeachBreak((id) =>
            {
                var is_ok_add = false;
                if (id.Value.tags.Count == 0) is_ok_add = true;

                if (is_ok_add)
                {
                    thumbnail_view.AddImages(new ImageLibrary.ImageDataExWordSet( id.Value, "")); // タグ未設定なのでex_wordもなしで良い
                    count++;
                    //var library_tag = image_library.GetTag(tag_name);
                    if (count >= count_max) return true;
                }
                return false;
            });
            thumbnail_view._UpdatePosThumbnail();
            thumbnail_view.SetScrollValue(0, true);
        }

        public void Optimization( bool is_no_exist_file_erase, bool tag_optimaize )
        {
            if (is_no_exist_file_erase)
            {
                var eraze_list = new List<ImageLibrary.ImageData>();

                image_library.image_datas.Foreach((id) =>
                //foreach (var id in image_library.image_datas)
                {
                    if (!File.Exists(id.Key))
                    {
                        eraze_list.Add(id.Value);
                    }
                });

                foreach( var image_data in eraze_list)
                {
                    image_library.image_datas.Remove(image_data.file_path);

                }

            }

            if (tag_optimaize)
            {
                image_library.image_datas.Foreach((id) =>
                {
                    // 同一タグの削除
                    // todo: ex_word違いをどう検出して対応できるか
                    var recreate_list = new Dictionary<string, ImageLibrary.ImageTag>();
                    foreach (var tag in id.Value.tags)
                    {
                        var tag_full_name_and_ex_word = tag.tag.GetFullName() + @"\," + tag.ex_word;
                        if (!recreate_list.ContainsKey(tag_full_name_and_ex_word))
                        {
                            recreate_list.Add(tag_full_name_and_ex_word, tag);
                        }
                    }
                    id.Value.tags.Clear();
                    foreach ( var rl in recreate_list)
                    {
                        id.Value.tags.Add(rl.Value);
                    }

                    // 詳細タグがある場合、上階層のタグを削除する
                    var eraze_list = new List<ImageLibrary.Tag>();
                    foreach (var tag1 in id.Value.tags)
                    {
                        foreach (var tag2 in id.Value.tags)
                        {
                            var tag = ImageLibrary.Tag.CheckGroupTag(tag1.tag, tag2.tag);
                            eraze_list.Add(tag);
                        }
                    }
                    foreach (var tag in eraze_list)
                    {
                        //id.Value.tag.Remove(tag);
                        id.Value.DeleteTag(tag);
                    }
                });
            }
        }

        public void SetTagAllThubnail( ImageLibrary.Tag tag )
        {
            foreach (var t in thumbnail_view.thumbnails)
            {
                t.data.image_data.tags.Add( new ImageLibrary.ImageTag(tag,""));
            }
            thumbnail_view.Refresh();
            image_library.UpdateTagStatus();
            tree_list.Refresh();
        }

        public void UnSetTagAllThubnail(ImageLibrary.Tag tag)
        {
            foreach (var t in thumbnail_view.thumbnails) {
                //t.data.tags.Remove(tag);
                t.data.image_data.DeleteTag(tag);
            }
            thumbnail_view.Refresh();
            image_library.UpdateTagStatus();
            tree_list.Refresh();
        }

        public void Dispose()
        {
            if (menu_panel != null) menu_panel.is_progress_thread_end = true;

            is_end_select_tag_by_tree_list_tag_option = true;
            image_manager.Dispose();
            save_load_manager.Dispose();
        }

        public void ReleaseTagByThubnail(ImageLibrary.Tag tag )
        {
            thumbnail_view.ReleaseTagByThubnail(tag);
        }

        delegate void _delegate_SetFormTitlex(string option);
        private void _Invoke_SetFormTitle(string option)
        {
            form.Text = Title + " " + option;
        }
        public void SetFormTitle( string option )
        {
            try
            {
                form.Invoke(new _delegate_SetFormTitlex(_Invoke_SetFormTitle), new Object[] { option });
            }
            catch
            {
                // 既に終了しているばあいがある
            }

        }

        public void SearchTagByTreeList(string word, bool is_nest = false)
        {
            var start_index = 0;
            if (last_tag_search_word == word)
            {
                start_index = last_tag_search_index+1;
            }

            var count = 0;
            var is_search_ok = false;
            tree_list.ForeachItem((li) =>
            {
                if (count < start_index)
                {
                    count++;
                }
                else
                {
                    // とりあえずは完全一致検索
                    if (li.text == word)
                    {
                        tree_list.SetSelectItem(li);
                        last_tag_search_word = word;
                        last_tag_search_index = count;
                        is_search_ok = true;
                        count++;
                        return true;
                    }
                    count++;
                }

                return false;
            });

            if (!is_search_ok)
            {
                //if (start_index > 0)
                //if (start_index < last_tag_search_index)
                if(!is_nest)
                {
                    last_tag_search_index = -1;
                    SearchTagByTreeList(word, true);
                }
            }

        }

        public void SetPinImageData( ImageLibrary.ImageDataExWordSet image_data_ex_word, bool is_pin_on)
        {
            if(is_pin_on)
            {
                image_library.image_datas_pin.Add(image_data_ex_word);
            }
            else
            {
                image_library.image_datas_pin.Remove(image_data_ex_word);
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
                form.Invoke(new delegate_action(invork_ProgressPanelDispose));
            }
            catch {}
        }
        private void invork_TreeListReflesh()
        {
            tree_list.Refresh();
        }
        public void TreeListReflesh()
        {
            try
            {
                form.Invoke(new delegate_action(invork_TreeListReflesh));
            }
            catch { }
        }

        private void ResetFocus()
        {
            text_box_tag_search.Enabled = false;
            focusButton.Focus();
            text_box_tag_search.Enabled = true;
        }

        // 検索バーのフォーカスを解除する
        public void ReleaseFocusTagSearchBox()
        {
            // フォーカスを外す
            if (text_box_tag_search.Focused)
            {
                text_box_tag_search.Enabled = false;
                text_box_tag_search.Enabled = true;
            }

        }

    }
}
