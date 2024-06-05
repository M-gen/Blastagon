using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;

using Blastagon.Common;
using Blastagon.UI.Common;
using Blastagon.App;


namespace Blastagon.UI
{
    // todo : このアプリ専用の色が強く、抽象化は失敗している...ここは対応が難しい

    public class TreeList : PictureBox
    {

        public class ListItem
        {
            public string text;
            public int    depth;
            public bool is_chiled_show = true; // 子のアイテムを展開しているかどうか
            public bool is_show = true; // 表示されている・展開されているかどうか
            public int x;
            public int y;
            public int w;
            public int h;

            public ListItem parent;
            public List<ListItem> chiled_list = new List<ListItem>();

            public void _AddChiled( ListItem chiled, int index = -1)
            {
                chiled.parent = this;
                if ( index == -1 )
                {
                    this.chiled_list.Add(chiled);
                }
                else
                {
                    this.chiled_list.Insert(index,chiled);
                }
            }
            public void _RemoveChiled( ListItem chiled)
            {
                chiled.parent = null;
                this.chiled_list.Remove(chiled);
            }

            public object tmp_data;
        }

        //
        private const int BUTTON_POS_X = 50;
        private int line_height = 36;
        private int line_depth_w = 12;
        private int items_offset_x = 24;
        private int hide_offset_y = 0;                 // 隠れているアイテムの高さの合計
        private ListItem hover_list_item = null;       // マウスが重なったアイテムであることを示す
        private ListItem select_list_item = null;      // 選択中のアイテム
        private ListItem old_select_list_item = null;      // 選択中のアイテム
        private List<ListItem> select_list_items = new List<ListItem>(); // 選択中のアイテムが複数の場合

        private bool is_context_menu_show = false;

        private MouseLeftButtonEventController mouse_left_button_event_controller;
        private bool is_mouse_down_left_drag_item_move = false; // マウスの右ボタンのドラッグによるアイテムの移動中である
        private bool is_mouse_down_left_drag_show_and_hide = false;            // 表示・非表示の列をドラッグしている場合
        private List<ListItem> left_drag_show_and_hide = new List<ListItem>(); // すでに表示・非表示をドラッグで切替済みかどうか
        private ImageLibrary.Tag.ShowMode left_drag_show_and_hide_mode = ImageLibrary.Tag.ShowMode.Show;

        private TextBox text_box_name_change = null;

        private VScrollBar scrollbar = null;
        private Common.WheelScrollController wheel_scroll_controller = null;

        private Bitmap icon_view_show;              // 表示
        private Bitmap icon_view_show_parent_hide;  // タグとしては表示だが、親タグが非表示のため非表示
        private Bitmap icon_view_show_chiled;       // 子タグのみ表示
        private Bitmap icon_view_exclusion;         // 除外
        private Bitmap icon_view_star;              // 星、このタグに含まれているファイルのみ表示される
        private Bitmap icon_add_tag;                // タグを追加
        private Bitmap icon_del_tag;                // タグを削除

        private Rectangle button_add_tag;
        private Rectangle button_del_tag;

        public enum InsertType
        {
            None,
            UpperInsert, // 上に同階層で挿入
            InChiled,    // 中に子要素として挿入
            UnderInsert, // 下に同階層で挿入
        }
        private InsertType mouse_down_left_drag_event_insert_type = InsertType.None;
        private ListItem mouse_down_left_drag_target_item = null; // 

        public LockList<ListItem> items = new LockList<ListItem>();

        public Action<ListItem> SelectItem;
        public Action<ListItem, string> EditName;
        public Action<ListItem, ListItem, ListItem> ChangeItemParent; // 親の変更、引数は自身、新しい親、古い親

        //System.Diagnostics.Stopwatch mouse_down_left_watch = new System.Diagnostics.Stopwatch();
        private ListItem mouse_down_left_select_item = null;

        private int mouse_area_h = 0;
        public bool is_key_input_box_use = false;
        
        System.Windows.Forms.Timer main_loop_timer;
        public bool is_refresh = false;
        public bool is_repaint_cancel = false;
        volatile Thread thread_update_tag_view_status = null;
        Point last_mouse_on_pos = new Point();

        private System.Windows.Forms.Timer select_item_release_timer; // 選択中のアイテムをクリックで外すさいに、すこし期間をおかないと不都合がでるので…ダブルクリックとか

        public enum SetTagViewModeType
        {
            ShowAll,             // 全てを表示状態にする
            ShowTargetInChileds, // 対象とそれ以下のアイテムの表示状態を表示にする
            ShowTargetOnly,      // 対象のみ表示状態にする、親は子供のみ表示、他は非表示
            Exclusion,           // 対象とそれ以下のアイテムを除外状態にする
            Star,                // 対象を星状態にする
            HideAll,             // 全て非表示にする
        }

        public TreeList(System.Windows.Forms.Control form, int x, int y)
        {
            Location = new Point(x, y);
            Size = new Size(400, 700);
            Parent = form;
            Parent.Controls.Add(this);

            scrollbar = new VScrollBar();
            scrollbar.Location = new Point(Size.Width, 0);
            scrollbar.Size = new Size(20, Size.Height);
            scrollbar.Scroll += (s, e) => { this.Refresh(); };
            this.Controls.Add(scrollbar);

            this.Paint += _Paint;
            this.MouseMove += _MouseMove;
            this.MouseDown += _MouseDown;
            this.MouseUp   += _MouseUp;
            this.Click     += _Click;
            this.Resize    += _Resize;
            

            button_add_tag = new Rectangle(Size.Width- BUTTON_POS_X, 10, 40,40);
            button_del_tag = new Rectangle(Size.Width- BUTTON_POS_X, 60, 40,40);

            icon_view_show             = UserInterfaceCommon.BitmapFromFile(@"data/Eldorado mini - eye.png",  20, 20, 1.0, true,  Color.FromArgb(130,220,130));
            icon_view_show_parent_hide = UserInterfaceCommon.BitmapFromFile(@"data/Eldorado mini - eye.png",  20, 20, 0.2, false, Color.FromArgb(0, 0, 0));
            icon_view_show_chiled      = UserInterfaceCommon.BitmapFromFile(@"data/Eldorado mini - eye.png",  20, 20, 1.0, true,  Color.FromArgb(220, 160, 100));
            icon_view_exclusion        = UserInterfaceCommon.BitmapFromFile(@"data/Eldorado mini - del2.png", 20, 20, 1.0, true, Color.FromArgb(250, 120, 120));
            icon_view_star             = UserInterfaceCommon.BitmapFromFile(@"data/Eldorado mini - star.png", 20, 20, 1.0, true, Color.FromArgb(255, 255, 100));
            icon_add_tag               = UserInterfaceCommon.BitmapFromFile(@"data/Eldorado mini - add.png", button_add_tag.Width, button_add_tag.Height, 1.0, true, Color.FromArgb(140, 140, 140));
            icon_del_tag               = UserInterfaceCommon.BitmapFromFile(@"data/Eldorado mini - del.png", button_add_tag.Width, button_add_tag.Height, 1.0, true, Color.FromArgb(140, 140, 140));
            mouse_down_left_select_item = select_list_item;

            main_loop_timer = new System.Windows.Forms.Timer();
            main_loop_timer.Interval = 10;
            main_loop_timer.Tick += Main_loop_timer_Tick;
            main_loop_timer.Start();

            select_item_release_timer = new System.Windows.Forms.Timer();
            select_item_release_timer.Interval = 100;
            select_item_release_timer.Tick += Select_item_release_timer_Tick;

            wheel_scroll_controller = new Common.WheelScrollController(WheelScrollController_Update, 200, 0.05 );

            mouse_left_button_event_controller = new MouseLeftButtonEventController();
            mouse_left_button_event_controller.event_single_crick = mouse_left_button_event_controller_SingleClick;
            mouse_left_button_event_controller.event_double_crick = mouse_left_button_event_controller_DoubleClick;
            mouse_left_button_event_controller.event_drag_start   = mouse_left_button_event_controller_DragStart;
            mouse_left_button_event_controller.event_drag         = mouse_left_button_event_controller_Drag;
            mouse_left_button_event_controller.event_drag_end     = mouse_left_button_event_controller_DragEnd;
        }

        private void WheelScrollController_Update( Int64 value )
        {
            SetScroll(scrollbar.Value - (int)value);
            is_refresh = true;
        }

        private void Select_item_release_timer_Tick(object sender, EventArgs e)
        {
            select_list_items.Remove(select_list_item);

            old_select_list_item = select_list_item;
            select_list_item = null;
            SelectItem(select_list_item);
            select_item_release_timer.Stop();
            is_refresh = true;
        }

        private void Main_loop_timer_Tick(object sender, EventArgs e)
        {
            if ( wheel_scroll_controller.IsScroll() )
            {
                wheel_scroll_controller.UpdateValue();
            }

            if (is_refresh)
            {
                this.Refresh();
                is_refresh = false;
            }
        }

        private void _Resize(object sender, EventArgs e)
        {
            scrollbar.Location = new Point(Size.Width-20, 0);
            scrollbar.Size = new Size(20, Size.Height);

            var x = Size.Width - BUTTON_POS_X - scrollbar.Width;
            var BUTTON_POS_X_MIN = 100;
            if (x < BUTTON_POS_X_MIN) x = BUTTON_POS_X_MIN;
            button_add_tag.X = x;// = new Rectangle(, 10, 40, 40);
            button_del_tag.X = x;// new Rectangle(Size.Width - BUTTON_POS_X, 60, 40, 40);

            foreach( var li in items)
            {
                var x1 = li.depth * line_depth_w;
                //var y = (items.Count()) * line_height - hide_offset_y;
                //li.x = x;
                //li.y = y;
                li.w = Size.Width - 1 - x1;
                //li.w = this.Size.Width;
            }
        }

        public ListItem AddItem( string text, int depth, bool is_update = true) // depth指定はテスト用
        {
            var li = new ListItem();
            UpdateItemBasicStatus( li, text, depth);
            items.Add(li);

            if (is_update)
            {
                UpdateScrollBarMax();
            }

            return li;
        }

        // 上（親ではない）のアイテムを指定して追加
        public ListItem AddItem(string text, ListItem up_item)
        {
            var li = new ListItem();
            UpdateItemBasicStatus(li, text, 0);
            if (up_item != null)
            {
                li.parent = up_item.parent;
                if (li.parent != null)
                {
                    li.parent.chiled_list.Insert(0, li);
                }
            }

            if (up_item!=null)
            {
                var pos = items.IndexOf(up_item);
                items.Insert(pos, li);
            }
            else if(li.parent == null)
            {
                items.Insert(0, li);
            }
            else
            {
                items.Add(li);
            }

            UpdateListSourtAsParent();
            UpdateScrollBarMax();

            return li;
        }

        // 親のアイテム指定で追加する
        public ListItem AddItemToParent(string text, ListItem parent_item)
        {
            var li = new ListItem();
            UpdateItemBasicStatus(li, text, 0);

            // 親子関係の設定
            parent_item.chiled_list.Add(li);
            li.parent = parent_item;

            items.Add(li);

            UpdateListSourtAsParent();
            UpdateScrollBarMax();

            return li;
        }

        public void DeleteItem(ListItem li)
        {

            if (li.parent != null)
            {
                li.parent._RemoveChiled(li);
            }
            items.Remove(li);

            UpdateListSourtAsParent();
            UpdateScrollBarMax();
        }

        public void DeleteItemAll()
        {
            items.Clear();

            UpdateListSourtAsParent();
            UpdateScrollBarMax();
        }

        delegate void _delegate_UpdateScrollBarMax(int value);
        private void _Invoke_UpdateScrollBarMax(int value)
        {
            if (value <= 0)
            {
                value = 0;
            }

            if (scrollbar.Value >= value)
            {
                SetScroll(value);
            }
            scrollbar.Maximum = value;
        }
        public void UpdateScrollBarMax()
        {
            try
            {
                Invoke(new _delegate_UpdateScrollBarMax(_Invoke_UpdateScrollBarMax), new Object[] { mouse_area_h });
            }
            catch
            {

            }
        }

        public void ForeachItem( Func<ListItem,bool> func )
        {
            foreach (var li in items)
            {
                if (func(li)) break;
            }
        }

        private void UpdateItemBasicStatus(ListItem li, string text, int depth)
        {
            li.text = text;
            li.depth = depth;

            var x = li.depth * line_depth_w;
            var y = (items.Count()) * line_height - hide_offset_y;
            li.x = x;
            li.y = y;
            li.w = Size.Width - 1 - x;
            li.h = line_height;

            if (!li.is_show)
            {
                hide_offset_y += line_height;
            }
        }

        public void MoveItem( ListItem li, ListItem target_li, InsertType insert_type )
        {
            // 移動不可の場所（自分、自身の子）
            {
                //if (li == target_li) return;
                var tmp_list = GetListItemsAsChiledAll(li);

                foreach (var li2 in tmp_list)
                {
                    if (li2 == target_li) return;
                }
            }

            var old_parent = li.parent;
            var new_parent = (ListItem)null;

            // 今の親からはずれる
            if ( li.parent!=null)
            {
                li.parent._RemoveChiled(li);
            }
            // いったんリストから除外
            items.Remove(li);

            switch (insert_type)
            {
                case InsertType.InChiled:
                    // 子供に入ればいいので簡単
                    target_li._AddChiled(li, 0);
                    {
                        new_parent = target_li;
                        var index = items.IndexOf(target_li);
                        items.Insert(index + 1, li);

                        UpdateDepth(li, target_li.depth + 1);
                    }
                    break;
                case InsertType.UpperInsert:
                    new_parent = target_li.parent;
                    if ( target_li.parent!=null)
                    {
                        var index0 = target_li.parent.chiled_list.IndexOf(target_li);
                        target_li.parent._AddChiled(li, index0);
                        UpdateDepth(li, target_li.depth + 1);
                    }
                    else
                    {
                        var index0 = items.IndexOf(target_li);
                        li.parent = null;
                        items.Insert(index0, li);
                    }
                    break;
                case InsertType.UnderInsert:
                    new_parent = target_li.parent;
                    if (target_li.parent != null)
                    {
                        var index0 = target_li.parent.chiled_list.IndexOf(target_li) + 1;
                        target_li.parent._AddChiled(li, index0);
                        UpdateDepth(li, target_li.depth + 1);
                    }
                    else
                    {
                        var index0 = items.IndexOf(target_li) + 1;
                        li.parent = null;
                        items.Insert(index0, li);
                    }
                    break;

            }

            // 親子の構成が終わったら、Itemsの構成を再構築...こまごまやるより楽
            UpdateListSourtAsParent();

            if (ChangeItemParent != null) ChangeItemParent(li,new_parent,old_parent);
        }

        /// <summary>
        /// リストが挿入などで親子関係が変わった場合、親子関係からリストを構築し直す
        /// </summary>
        public void UpdateListSourtAsParent()
        {
            UpdateChiledShowOrHideStatus();

            var root_list = new List<ListItem>();
            foreach (var li in items)
            {
                if (li.parent == null)
                {
                    root_list.Add(li);
                }
            }
            items.Clear();

            hide_offset_y = 0;
            UpdateListSourtAsParent_Sub(root_list);
            
            //mouse_area_h = Items.Count * line_height - hide_offset_y - ( Size.Height - line_height * 2);
            mouse_area_h = items.Count() * line_height - hide_offset_y - (Size.Height/2);
            UpdateScrollBarMax();
        }

        private void UpdateListSourtAsParent_Sub(List<ListItem> list )
        {
            foreach (var li in list)
            {
                var depth = 0;
                if (li.parent != null) depth = li.parent.depth + 1;

                UpdateItemBasicStatus(li, li.text, depth);
                items.Add(li);

                if (li.chiled_list.Count > 0)
                {
                    UpdateListSourtAsParent_Sub(li.chiled_list);
                }
            }            
        }

        private List<ListItem> GetListItemsAsChiledAll(ListItem li)
        {
            var tmp_list = new List<ListItem>();
            tmp_list.Add(li);

            foreach (var li2 in li.chiled_list)
            {
                var tmp_list2 = GetListItemsAsChiledAll(li2);
                tmp_list.AddRange(tmp_list2);
            }

            return tmp_list;

        }

        // tmp_data が一致するリストアイテムを返す
        public ListItem GetListItemByTmpData(object tmp_data)
        {
            foreach ( var li in items)
            {
                if (li.tmp_data == tmp_data) return li;
            }

            return null;
        }

        /// <summary>
        /// ListItemのサブ含む子供の総数を取得する
        /// </summary>
        /// <param name="li">対象のListItem</param>
        /// <param name="is_show_only">表示のみ確認するかどうか</param>
        /// <returns></returns>
        private int GetChiledLastIndexOf( ListItem li, bool is_show_only=true )
        {
            if (li.chiled_list.Count == 0) return 0;

            var i = 0;
            foreach( var li2 in li.chiled_list )
            {
                i += GetChiledLastIndexOf(li2);
                i++;
            }
            return i;
        }

        private void UpdateDepth( ListItem li, int depth )
        {
            int index = items.IndexOf(li);
            
            li.depth = depth;
            var x = li.depth * line_depth_w;
            var y = index * line_height;
            li.x = x;
            li.y = y;
            li.w = Size.Width - 1 - x;

            foreach (var li2 in li.chiled_list)
            {
                UpdateDepth(li2, depth + 1);
            }
        }

        private void _Paint(object sender, PaintEventArgs e)
        {
            if (is_repaint_cancel) return;

            // タグについては、更新が重なる描画に失敗しやすいため、
            // 失敗時の再描画を保険として入れておく
            try
            {
                _Paint_Core(sender, e);
            }
            catch
            {
                //this.Refresh();
                is_refresh = true; // todo : どっかばぐってる。。。原因はどこ？あと、個タグの反映がおかしいんだが…これは、完了前に描画しているせいか？
            }
        }

        private void _Paint_Core(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            
            var font = new Font("メイリオ", 16, FontStyle.Regular);
            var font_mini = new Font("メイリオ", 12.5F, FontStyle.Regular);
            if (items.Count() == 0)
            {

                Pen blackPen = new Pen(Color.FromArgb(150, 255, 255, 255), 5);
                blackPen.DashStyle = DashStyle.Dot;
                //g.DrawLine(blackPen, 10, 70, 200, 70);
                var margin = 20;
                g.DrawRectangle(blackPen, margin, margin, Size.Width - margin * 2 - 20, Size.Height - margin * 2);
                g.DrawString("タグ一覧", new Font("メイリオ", 20), new SolidBrush(Color.FromArgb(60, 0, 0, 0)), 30, 30);
                //g.DrawString("対応形式 : PNG JPEG", new Font("メイリオ", 20), new SolidBrush(Color.FromArgb(100, 255, 255, 255)), 100, 150);
            }

            foreach (var li in items)
            {
                if (!li.is_show)
                {
                    continue;
                }

                var x = li.depth * line_depth_w + items_offset_x;
                var y = li.y - scrollbar.Value;

                //if (select_list_item == li) // セレクト優先
                if (IsSelected( li)) // セレクト優先
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 255, 255, 255)), x, y, li.w, li.h);
                }
                else if (hover_list_item == li)
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 235, 235, 235)), x, y, li.w, li.h);
                }
                else
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 245, 245, 245)), x, y, li.w, li.h);
                }

                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 160, 160, 160)), 0, y, items_offset_x, li.h);
                g.DrawRectangle(new Pen(Color.FromArgb(255, 140, 140, 140), 1),     0, y, items_offset_x, li.h);
                for (var i = 0; i < li.depth; i++)
                {
                    var ox = line_depth_w * i + items_offset_x;
                    if ((i % 6) < 3)
                    {
                        g.FillRectangle(new SolidBrush(Color.FromArgb(255, 145, 145, 145)), ox, y, line_depth_w, li.h);
                        g.DrawRectangle(new Pen(Color.FromArgb(255, 120, 120, 120), 1), ox, y, line_depth_w, li.h);
                    }
                    else
                    {
                        g.FillRectangle(new SolidBrush(Color.FromArgb(255, 160, 160, 160)), ox, y, line_depth_w, li.h);
                        g.DrawRectangle(new Pen(Color.FromArgb(255, 120, 120, 120), 1), ox, y, line_depth_w, li.h);
                    }
                }

                var fp = new Pen(Color.FromArgb(255, 0, 0, 0), 1);
                var fb = new SolidBrush(Color.FromArgb(255, 64, 64, 64));
                var fb2 = new SolidBrush(Color.FromArgb(255, 160, 160, 160));
                // テキストの表示
                g.DrawString(li.text, font, fb, x + 6, y + 4);

                // テキストに付随する情報の表示
                using (StringFormat sf = new StringFormat(StringFormat.GenericTypographic))
                {
                    var count = ((ImageLibrary.Tag)li.tmp_data).belong_image_count;
                    var size2 = g.MeasureString(li.text, font, this.Width, sf);;
                    g.DrawString( string.Format("{0}",count), font_mini, fb2, x + 6 + size2.Width + 10, y + 4 + 3.5f);
                }

                //var top_sq_size = 10;

                var tag = (ImageLibrary.Tag)li.tmp_data;
                switch(tag.show_mode)
                {
                    case ImageLibrary.Tag.ShowMode.Show:
                        if (tag.is_show)
                        {
                            g.DrawImage(icon_view_show, 2, y + 9);
                        }
                        else
                        {
                            g.DrawImage(icon_view_show_parent_hide, 2, y + 9);
                        }
                        break;
                    case ImageLibrary.Tag.ShowMode.ShowChiled:
                        if (tag.is_show_chiled)
                        {
                            g.DrawImage(icon_view_show_chiled, 2, y + 9);
                        }
                        else
                        {
                            g.DrawImage(icon_view_show_parent_hide, 2, y + 9);
                        }
                        break;
                    case ImageLibrary.Tag.ShowMode.Exclusion:
                        g.DrawImage(icon_view_exclusion, 2, y + 9);
                        break;
                    case ImageLibrary.Tag.ShowMode.Star:
                        g.DrawImage(icon_view_star, 2, y + 9);
                        break;
                }


                g.DrawRectangle(new Pen(Color.FromArgb(255, 120, 120, 120), 1), 0, y, x, li.h);
                g.DrawRectangle(new Pen(Color.FromArgb(255, 200, 200, 200), 1), x, y, li.w, li.h);
                {

                    var size = 10;
                    var ox = li.x + items_offset_x;
                    var oy = y + li.h - size;
                    var ow = size;
                    var oh = size;
                    Point[] ps = {new Point(ox, oy),
                        new Point(ox, oy+oh),
                        new Point(ox+ow, oy+oh),
                        new Point(ox, oy),
                    };

                    if (li.chiled_list.Count == 0)
                    {

                    }
                    else if (li.is_chiled_show)
                    {
                        g.DrawLines(new Pen(Color.FromArgb(255, 200, 200, 200), 1), ps);
                    }
                    else
                    {
                        g.FillPolygon(new SolidBrush(Color.FromArgb(255, 180, 180, 180)), ps);
                        g.DrawLines(new Pen(Color.FromArgb(255, 160, 160, 160), 1), ps);
                    }
                }


            }

            // 選択強調用
            // 複数表示用...
            foreach (var si in select_list_items)
            {
                if (!si.is_show) continue;
                var li = si;
                var x = li.depth * line_depth_w + items_offset_x;
                var y = li.y - scrollbar.Value;
                var m = 3;
                //g.FillRectangle(new SolidBrush(Color.FromArgb(255, 255, 255, 255)), x, y, li.w, li.h);
                //g.DrawRectangle(new Pen(Color.FromArgb(255, 240, 240, 240), 4), x, y, li.w, li.h);
                g.DrawRectangle(new Pen(Color.FromArgb(255, 255, 255, 255), 4), x, y, li.w, li.h);
                g.DrawRectangle(new Pen(Color.FromArgb(80, 0, 0, 0), 1), x - m, y - m, li.w + m * x, li.h + m * 2);
                //g.DrawLine(new Pen(Color.FromArgb(80, 0, 0, 0), 1), x - m, y + li.h + m, li.w + m * x, y + li.h + m);
                //g.DrawLine(new Pen(Color.FromArgb(60, 0, 0, 0), 1), x - m, y + li.h + m + 1, li.w + m * x, y + li.h + m + 1);
                //g.DrawLine(new Pen(Color.FromArgb(40, 0, 0, 0), 1), x - m, y + li.h + m + 2, li.w + m * x, y + li.h + m + 2);

            }
            if ( (select_list_item != null) && (select_list_item.is_show) )
            {
                var li = select_list_item;
                var x = li.depth * line_depth_w + items_offset_x;
                var y = li.y - scrollbar.Value;
                //g.FillRectangle(new SolidBrush(Color.FromArgb(255, 255, 255, 255)), x, y, li.w, li.h);
                g.DrawRectangle(new Pen(Color.FromArgb(255, 255, 255, 255), 4), x, y, li.w, li.h);
                var m = 3;
                g.DrawRectangle(new Pen(Color.FromArgb(80, 0, 0, 0), 1), x-m, y-m, li.w+m*x, li.h+m*2);
                g.DrawLine(new Pen(Color.FromArgb(80, 0, 0, 0), 1), x - m, y + li.h + m, li.w + m * x, y + li.h + m);
                g.DrawLine(new Pen(Color.FromArgb(60, 0, 0, 0), 1), x - m, y + li.h + m + 1, li.w + m * x, y + li.h + m + 1);
                g.DrawLine(new Pen(Color.FromArgb(40, 0, 0, 0), 1), x - m, y + li.h + m + 2, li.w + m * x, y + li.h + m + 2);

            }

            if (mouse_left_button_event_controller.state == MouseLeftButtonEventController.State.MouseMoveAfter_Drag)
            {
                switch (mouse_down_left_drag_event_insert_type)
                {
                    case InsertType.InChiled:
                        {
                            var li = mouse_down_left_drag_target_item;
                            var li_y = li.y - scrollbar.Value;
                            g.FillRectangle(new SolidBrush(Color.FromArgb(64, 200, 200, 240)), li.x, li_y, li.w, li.h);
                            g.DrawRectangle(new Pen(Color.FromArgb(255, 150, 150, 150), 2), li.x+1, li_y + 1, li.w-1, li.h - 1);
                        }
                        break;
                    case InsertType.UpperInsert:
                        {
                            var li = mouse_down_left_drag_target_item;
                            var li_y = li.y - scrollbar.Value;
                            //var y = (mouse_left_button_event_controller.drag_now_point.Y / line_height) * line_height + 2 - scrollbar.Value;
                            var y = li_y + 2 ;
                            g.FillRectangle(new SolidBrush(Color.FromArgb(64, 200, 200, 240)), li.x, li_y, li.w, li.h);
                            g.DrawLine(new Pen(Color.FromArgb(255, 150, 255, 150), 3), li.x, y, li.x + li.w, y);
                        }
                        break;
                    case InsertType.UnderInsert:
                        {
                            var li = mouse_down_left_drag_target_item;
                            var li_y = li.y - scrollbar.Value;
                            //var y = (mouse_left_button_event_controller.drag_now_point.Y / line_height) * line_height + line_height - 2 - scrollbar.Value;
                            var y = li_y - 2 + line_height;
                            g.FillRectangle(new SolidBrush(Color.FromArgb(64, 200, 200, 240)), li.x, li_y, li.w, li.h);
                            g.DrawLine(new Pen(Color.FromArgb(255, 255, 150, 150), 3), li.x, y, li.x+li.w, y);
                        }
                        break;
                }

            }

            g.DrawImage(icon_add_tag, button_add_tag.X, button_add_tag.Y);
            g.DrawImage(icon_del_tag, button_del_tag.X, button_del_tag.Y);

        }

        private void _MouseMove(object sender, MouseEventArgs e)
        {
            last_mouse_on_pos = new Point(e.X, e.Y);
            //var mx = e.X;
            //var my = e.Y + scrollbar.Value;
            //if (Items.Count <= 0) return;    // UBoxがないとできないので

            mouse_left_button_event_controller.Update_MouseMove(e.X, e.Y);

            if ( (mouse_left_button_event_controller.state != MouseLeftButtonEventController.State.MouseMoveAfter_Drag) && ( items.Count() > 0) )
            {
                var mx = e.X;
                var my = e.Y + scrollbar.Value;

                // 非ドラッグ時のマウス移動
                foreach (var li in items)
                {
                    if (!li.is_show) continue;
                    if (li.x <= mx && mx <= li.x + li.w && li.y <= my && my <= li.y + li.h)
                    {
                        hover_list_item = li;
                        break;
                    }
                }
                is_refresh = true;
            }

            //if (is_mouse_down_left_drag == false && items.Count() > 0)
            //{
            //    foreach (var li in items)
            //    {
            //        if (!li.is_show) continue;
            //        if (li.x <= mx && mx <= li.x + li.w && li.y <= my && my <= li.y + li.h)
            //        {
            //            hover_list_item = li;
            //            break;
            //        }
            //    }
            //}
            //else 


        }

        private void _MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseEdit();

            if (wheel_scroll_controller.IsScroll())
            {
                wheel_scroll_controller.Stop();
                return;
            }

            // スクロールバー上の挙動は感知しない
            if ( ( scrollbar.Location.X <= e.X && e.X <= scrollbar.Location.X + scrollbar.Size.Width) &&
                  (scrollbar.Location.Y <= e.Y && e.Y <= scrollbar.Location.Y + scrollbar.Size.Height) )
            {
                return;
            }

            //var items_height = items.Count() * line_height - hide_offset_y; // タグの全体の高さ

            if ( e.Button == MouseButtons.Left )
            {
                // コンテキストメニュー表示後のマウス押下を一回無視する
                if (is_context_menu_show)
                {
                    is_context_menu_show = false;
                    return;
                }

                mouse_left_button_event_controller.Update_MouseDown(e.X, e.Y);
                
                //mouse_down_left_watch.Restart();
                //mouse_down_left_select_item = select_list_item;

            }
            else if (e.Button == MouseButtons.Right)
            {

                if (e.X < items_offset_x)
                {
                    is_context_menu_show = true;
                    select_list_item = CheckHitItem(e.X, e.Y);

                    ContextMenuStrip menu = new ContextMenuStrip();
                    menu.Items.Add("このタグのみ表示(&S)", null, (s, e2) => {
                        SetTagViewMode(select_list_item, SetTagViewModeType.ShowTargetOnly);
                        Refresh();
                        is_context_menu_show = false;
                    });
                    menu.Items.Add("このタグ以下を全て表示(&W)", null, (s, e2) => {
                        SetTagViewMode(select_list_item, SetTagViewModeType.ShowTargetInChileds);
                        Refresh();
                        is_context_menu_show = false;
                    });
                    menu.Items.Add("全て表示(&A)", null, (s, e2) => {
                        SetTagViewMode(select_list_item, SetTagViewModeType.ShowAll);
                        Refresh();
                        is_context_menu_show = false;
                    });
                    menu.Items.Add("全て非表示(&Z)", null, (s, e2) => {
                        SetTagViewMode(select_list_item, SetTagViewModeType.HideAll);
                        Refresh();
                        is_context_menu_show = false;
                    });
                    menu.Items.Add("子タグのみ表示(&R)", null, (s, e2) => {
                        SetTagShowMode(select_list_item, ImageLibrary.Tag.ShowMode.ShowChiled);
                        Refresh();
                        is_context_menu_show = false;
                    });
                    menu.Items.Add("除外(&D)", null, (s, e2) => {
                        SetTagViewMode(select_list_item, SetTagViewModeType.Exclusion);
                        Refresh();
                        is_context_menu_show = false;
                    });
                    menu.Items.Add("星マーク(&X)", null, (s, e2) => {
                        SetTagViewMode(select_list_item, SetTagViewModeType.Star);
                        Refresh();
                        is_context_menu_show = false;
                    });
                    menu.Show(Cursor.Position, ToolStripDropDownDirection.AboveLeft);
                }
                else { 
                    is_context_menu_show = true;
                    select_list_item = CheckHitItem(e.X, e.Y);

                    ContextMenuStrip menu = new ContextMenuStrip();
                    menu.Items.Add("名前を変更する(&R)", null, (s, e2) => {
                        EditTagName();
                        is_context_menu_show = false;
                    });
                    menu.Items.Add("タグを追加する(&A)", null, (s, e2) => {
                        var tag = default(ImageLibrary.Tag);
                        if (select_list_item == null)
                        {
                            tag = AppCore.core.CreateTag("New Tag", null);
                        }
                        else
                        {
                            tag = AppCore.core.CreateTag("New Tag", (ImageLibrary.Tag)select_list_item.tmp_data);
                        }
                        var ti = GetListItemByTmpData(tag);
                        select_list_item = ti;
                        SelectItem(ti);
                        is_refresh = true;

                        is_context_menu_show = false;
                    });
                    menu.Items.Add("タグを削除する(&D)", null, (s, e2) => {
                        DeleteSelectItems();
                        //var res = MessageBox.Show(select_list_item.text + "を削除しますか？", "確認", MessageBoxButtons.OKCancel);
                        //if (res == DialogResult.OK)
                        //{
                        //    AppCore.core.DeleteTag((ImageLibrary.Tag)select_list_item.tmp_data);
                        //    this.Refresh();
                        //    AppCore.core.SetSelectTagByTreeListTagOption();
                        //}
                        is_context_menu_show = false;
                    });
                    menu.Items.Add("タグを全て削除する(&K)", null, (s, e2) => {
                        var res = MessageBox.Show("全てのタグを削除しますか？", "確認", MessageBoxButtons.OKCancel);
                        if (res == DialogResult.OK)
                        {
                            AppCore.core.DeleteTagAll();
                        }
                        is_context_menu_show = false;
                    });
                    menu.Items.Add(new ToolStripSeparator());
                    menu.Items.Add("サムネイルの全ての画像をタグに登録(&Q)", null, (s, e2) => {
                        AppCore.core.SetTagAllThubnail((ImageLibrary.Tag)select_list_item.tmp_data);
                        is_context_menu_show = false;
                    });
                    menu.Items.Add("サムネイルの全ての画像をタグから外す(&B)", null, (s, e2) => {
                        AppCore.core.UnSetTagAllThubnail((ImageLibrary.Tag)select_list_item.tmp_data);
                        is_context_menu_show = false;
                    });

                    menu.Show(Cursor.Position, ToolStripDropDownDirection.AboveLeft);
                }
            }


        }
        
        public ListItem CheckHitItem()
        {
            return CheckHitItem(last_mouse_on_pos.X, last_mouse_on_pos.Y);
        }

        public ListItem CheckHitItem( int x, int y )
        {
            y += scrollbar.Value;

            foreach (var li in items)
            {
                if (li.is_show == false) continue;
                // xをそのまま運用すると、ややこしいので、横0px目から反応させる
                //if ((li.x <= x && x < li.x + li.w) && (li.y <= y && y < li.y + li.h))
                if ((0<= x && x < li.x + li.w) && (li.y <= y && y < li.y + li.h))
                {
                    return li;
                }
            }
            return null;

        }

        public void SetViewChiledItems( ListItem li, bool is_chiled_show)
        {
            // 子タグの展開
            if (li.chiled_list.Count != 0)
            {
                //if (li.is_chiled_show) li.is_chiled_show = false;
                //else li.is_chiled_show = true;
                if ( li.is_chiled_show != is_chiled_show)
                {
                    li.is_chiled_show = is_chiled_show;

                    if (li.is_chiled_show)
                    {
                        // 開く
                        ShowChiledItems(li);
                    }
                    else
                    {
                        // 閉じる
                        HideChiledItems(li);
                    }

                    UpdateListSourtAsParent();

                }

            }
        }

        private void ShowChiledItems(ListItem parent_li)
        {
            foreach (var li in parent_li.chiled_list)
            {
                li.is_show = true;
                if( li.is_chiled_show ) ShowChiledItems(li);
            }
        }

        // 指定したアイテムの子供を全て非表示にする
        private void HideChiledItems( ListItem parent_li )
        {
            foreach( var li in parent_li.chiled_list)
            {
                li.is_show = false;
                HideChiledItems(li);
            }
        }

        private void _MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouse_left_button_event_controller.Update_MouseUp(e.X, e.Y);

                //if (select_list_item == null)
                //{
                //}
                //else if (is_mouse_down_left_drag
                //    && (mouse_down_left_drag_event_insert_type!= InsertType.None)
                //    && mouse_down_left_drag_target_item!=null)
                //{
                //    if (select_list_items.Count() >= 2)
                //    {
                //        foreach ( var sl in select_list_items)
                //        {
                //            MoveItem(
                //                sl,
                //                mouse_down_left_drag_target_item,
                //                mouse_down_left_drag_event_insert_type);
                //        }
                //    }
                //    else
                //    {
                //        MoveItem(
                //            select_list_item,
                //            mouse_down_left_drag_target_item,
                //            mouse_down_left_drag_event_insert_type);
                //        mouse_down_left_drag_event_insert_type = InsertType.None;
                //        mouse_down_left_drag_target_item = null;
                //        //select_list_item = null;
                //    }
                //    is_refresh = true;
                //}
            }

        }

        private void _Click(object sender, EventArgs e)
        {
            if ( hover_list_item!=null)
            {
                if ((Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left)
                {
                    select_list_item = hover_list_item;
                    if (SelectItem != null) SelectItem(select_list_item);
                }
                else if ((Control.MouseButtons & MouseButtons.Left) == MouseButtons.Right)
                {
                }

            }
        }

        public void EditTagName()
        {
            if (select_list_item == null) return;
            ReleaseEdit();

            is_key_input_box_use = true;
            var si = select_list_item;
            var margin = 1;
            text_box_name_change = new TextBox();
            text_box_name_change.Location = new Point(si.x- margin + /*si.depth * line_depth_w*/ + items_offset_x, si.y-scrollbar.Value- margin);
            text_box_name_change.Size = new Size(si.w+ margin*2, si.h+ margin*2);
            text_box_name_change.Font = new Font("メイリオ", 16, FontStyle.Regular);
            text_box_name_change.Text = si.text;
            text_box_name_change.ImeMode = ImeMode.NoControl;
            text_box_name_change.BorderStyle = BorderStyle.None;
            text_box_name_change.Leave += (s, e) =>
            {
                _EditTagName_Fix(si, text_box_name_change.Text);
            };
            text_box_name_change.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    _EditTagName_Fix(si, text_box_name_change.Text);
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    _EditTagName_Fix(si, "" );
                }
            };
            this.Controls.Add(text_box_name_change);
            text_box_name_change.Focus();

        }

        // 編集状態のものがあれば手放す
        public void ReleaseEdit()
        {
            if (text_box_name_change != null)
            {
                text_box_name_change.Dispose();
                text_box_name_change = null;
            }
        }
        //public PaintEventHandler( Object sender, Syst )

        private void _EditTagName_Fix( ListItem si, string fix_text)
        {
            is_key_input_box_use = false;
            if ((text_box_name_change == null) || (si.text == fix_text))
            {
                text_box_name_change.Dispose();
                text_box_name_change = null;
                return;
            }

            if (fix_text.IndexOf(@"\")>=0 ) {
                AppCore.core.popup_log.AddMessage(@"\はタグ名に利用できません");
                text_box_name_change.Dispose();
                text_box_name_change = null;
                return;
            }

            var old_text = si.text;
            si.text = fix_text;
            if (EditName != null) EditName(si, old_text);

            text_box_name_change.Dispose();
            text_box_name_change = null;

        }

        public void _MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //int numberOfTextLinesToMove = e.Delta * SystemInformation.MouseWheelScrollLines / 120;
            //SetScroll(scrollbar.Value-numberOfTextLinesToMove*10);
            wheel_scroll_controller.UpdateWheel(e.Delta * SystemInformation.MouseWheelScrollLines);
            //this.Refresh();
        }

        private void SetScroll( int value )
        {
            if (value < 0)
            {
                value = 0;
                wheel_scroll_controller.Stop();
            }
            if (scrollbar.Maximum < value)
            {
                value = scrollbar.Maximum;
                wheel_scroll_controller.Stop();
            }

            scrollbar.Value = value;
        }

        private void UpdateChiledShowOrHideStatus()
        {
            foreach (var li in items)
            {
                if (li.is_show && li.is_chiled_show)
                {
                    ShowChiledItems(li);
                }
                else
                {
                    HideChiledItems(li);
                }
            }
        }

        public void UpdateTagViewStatus()
        {
            var is_loop = true;
            while (is_loop)
            {
                if (thread_update_tag_view_status == null)
                {
                    is_loop = false;
                    thread_update_tag_view_status = new Thread(new ThreadStart(
                        () =>
                        {
                            UpdateTagViewStatus_Main();
                            thread_update_tag_view_status = null;
                        }
                    ));
                    thread_update_tag_view_status.Start();
                    break;
                }
            }
        }
        public void UpdateTagViewStatus_Main()
        {
            // 各タグの状態を更新
            // todo: 2万件ほどタグに入ってると、反応があからさまに鈍い

            // 星タグの検出
            var is_star = false;
            foreach (var li in items)
            {
                if (((ImageLibrary.Tag)li.tmp_data).show_mode ==  ImageLibrary.Tag.ShowMode.Star)
                {
                    is_star = true;
                    break;
                }
            }

            foreach (var li in items)
            {
                if (li.parent == null)
                {
                    UpdateTagShowMode(li, is_star);
                }
            }

            is_refresh = true;
            AppCore.core.SetSelectTagByTreeListTagOption();

        }

        private void SetTagShowMode( ListItem li, ImageLibrary.Tag.ShowMode show_mode )
        {
            var tag = (ImageLibrary.Tag)li.tmp_data;

            tag.show_mode = show_mode;
            UpdateTagViewStatus();
        }

        public void SetTagViewMode(ListItem li, SetTagViewModeType set_tag_view_mode_type)
        {
            switch (set_tag_view_mode_type)
            {
                case SetTagViewModeType.ShowAll:
                    foreach (var li2 in items)
                    {
                        var li_tag = (ImageLibrary.Tag)li2.tmp_data;
                        li_tag.show_mode = ImageLibrary.Tag.ShowMode.Show;
                    }

                    UpdateTagViewStatus();

                    break;

                case SetTagViewModeType.ShowTargetInChileds:
                    SetTagViewMode_ShowTargetInChileds(li);
                    UpdateTagViewStatus();

                    break;

                case SetTagViewModeType.ShowTargetOnly:
                    SetTagViewMode_ShowTargetOnly(li);
                    break;

                case SetTagViewModeType.Exclusion:
                    SetTagViewMode_Exclusion(li);
                    UpdateTagViewStatus();
                    break;

                case SetTagViewModeType.Star:
                    {
                        var is_ok = true;
                        var tmp_parent = li.parent;
                        while (tmp_parent != null)
                        {
                            if (((ImageLibrary.Tag)tmp_parent.tmp_data).show_mode == ImageLibrary.Tag.ShowMode.Star)
                            {
                                is_ok = false;
                                break;
                            }
                            tmp_parent = tmp_parent.parent;
                        }
                        // todo: こどもにもいないか、確認が必要

                        if (is_ok)
                        {
                            var tag = (ImageLibrary.Tag)li.tmp_data;
                            tag.show_mode = ImageLibrary.Tag.ShowMode.Star;
                            UpdateTagViewStatus();
                        }
                        else
                        {
                            AppCore.core.popup_log.AddMessage("親タグをたどると星があるため、星に設定できません");
                        }
                    }
                    break;
                case SetTagViewModeType.HideAll:
                    foreach (var li2 in items)
                    {
                        var li_tag = (ImageLibrary.Tag)li2.tmp_data;
                        li_tag.show_mode = ImageLibrary.Tag.ShowMode.Hide;
                    }

                    UpdateTagViewStatus();
                    break;
            }
        }
        
        /// <summary>
        /// アイテムの、ViewModeを変更する
        /// </summary>
        /// <param name="li">変更するアイテム</param>
        private void SetTagViewMode_ShowTargetOnly(ListItem li)
        {
            var li_tag = (ImageLibrary.Tag)li.tmp_data;
            foreach (var li2 in items)
            {
                var tag  = (ImageLibrary.Tag)li2.tmp_data;
                if (li==li2 )
                {
                    tag.show_mode = ImageLibrary.Tag.ShowMode.Show;
                }
                else if ((ImageLibrary.Tag.CheckGroupTag(tag, li_tag)) != null)
                {
                    tag.show_mode = ImageLibrary.Tag.ShowMode.ShowChiled;
                }
                else
                {
                    tag.show_mode = ImageLibrary.Tag.ShowMode.Hide;
                }
            }

            UpdateTagViewStatus();

        }


        private void SetTagViewMode_ShowTargetInChileds( ListItem li )
        {
            var li_tag = (ImageLibrary.Tag)li.tmp_data;
            li_tag.show_mode = ImageLibrary.Tag.ShowMode.Show;
            foreach (var li2 in li.chiled_list)
            {
                SetTagViewMode_ShowTargetInChileds(li2);
            }
        }


        private void SetTagViewMode_Exclusion(ListItem li)
        {
            var tag = (ImageLibrary.Tag)li.tmp_data;
            tag.show_mode = ImageLibrary.Tag.ShowMode.Exclusion;

            foreach (var li2 in li.chiled_list)
            {
                SetTagViewMode_Exclusion(li2);
            }
        }
        
        
        private void UpdateTagShowMode(ListItem li, bool is_star)
        {
            var tag = (ImageLibrary.Tag)li.tmp_data;

            if (tag.show_mode== ImageLibrary.Tag.ShowMode.Star )
            {
                tag.is_show = true;
                tag.is_show_chiled = true;
            }
            else if (UpdateTagShowMode_IsShowLink(li, is_star))
            {
                switch (tag.show_mode)
                {
                    case ImageLibrary.Tag.ShowMode.Show:
                        tag.is_show = true;
                        tag.is_show_chiled = true;
                        break;
                    case ImageLibrary.Tag.ShowMode.ShowChiled:
                        tag.is_show = false;
                        tag.is_show_chiled = true;
                        break;
                    case ImageLibrary.Tag.ShowMode.Hide:
                        tag.is_show = false;
                        tag.is_show_chiled = false;
                        break;
                    case ImageLibrary.Tag.ShowMode.Exclusion:
                        tag.is_show = false;
                        tag.is_show_chiled = false;
                        break;
                }
            }
            else 
            {
                tag.is_show = false;
                tag.is_show_chiled = false;
            }

            foreach (var li2 in li.chiled_list)
            {
                UpdateTagShowMode(li2, is_star);
            }

        }

        // 親(parent)まで、表示して良い状態かどうかを判断する
        // 子タグのみ表示、という指定があるので、1段関数をわけている
        private bool UpdateTagShowMode_IsShowLink(ListItem li, bool is_star)
        {
            if (li.parent == null)
            {
                if (is_star) return false;
                else return true;
            }

            var parent_tag = (ImageLibrary.Tag)li.parent.tmp_data;
            if (parent_tag.is_show) return true;

            if (parent_tag.show_mode == ImageLibrary.Tag.ShowMode.ShowChiled)
            {
                // 親をたどっていって、表示可能かどうかの判断が必要になる
                return UpdateTagShowMode_IsShowLink(li.parent, is_star);
            }

            return false;
        }

        public ListItem GetUpItem()
        {
            if (select_list_item == null) return null;

            foreach (var li in items)
            {
                if ((li.is_show) && (select_list_item.y == li.y + line_height) )
                {
                    return li;
                }
            }
            return null;

        }

        public ListItem GetDownItem()
        {
            if (select_list_item == null) return null;

            foreach (var li in items)
            {
                if ((li.is_show) && (select_list_item.y == li.y - line_height))
                {
                    return li;
                }
            }
            return null;

        }
        public void SetSelectItem( ListItem li )
        {
            select_list_item = li;

            // 選択したものが見えないと困るので

            // 枠内に入っているか検出
            //var is_hit = Hit.IsHit(0, scrollbar.Value, new Rectangle(li.x, li.y, li.w, li.h));
            var is_hit = Hit.IsHit(li.x, li.y, new Rectangle(0, scrollbar.Value, Size.Width, Size.Height-li.h));


            // 枠内に入ってない、移動させる
            if (!is_hit)
            {
                var pos = li.y - Size.Height / 2;
                if (pos < 0) pos = 0;
                scrollbar.Value = pos;
            }

            //Refresh();
            is_refresh = true;
        }

        /// <summary>
        /// 指定したリストアイテムが、選択されているかどうか
        /// 複数選択を考慮して値を返す
        /// </summary>
        /// <param name="li"></param>
        /// <returns></returns>
        private bool IsSelected( ListItem li )
        {
            if (li == null) return false;
            if (select_list_item == li) return true;
            foreach( var si in select_list_items)
            {
                if (si == li) return true;
            }
            return false;
        }

        public void DeleteSelectItems()
        {
            if (select_list_item == null) return;

            if (select_list_items.Count() > 1)
            {
                var items_str = "";
                foreach (var si in select_list_items) items_str += si.text + "\n";

                var res = MessageBox.Show(items_str + "を削除しますか？", "確認", MessageBoxButtons.OKCancel);
                if (res == DialogResult.OK)
                {
                    foreach (var si in select_list_items)
                    {
                        AppCore.core.DeleteTag((ImageLibrary.Tag)si.tmp_data);
                    }
                    AppCore.core.SetSelectTagByTreeListTagOption();
                    select_list_items.Clear();
                    select_list_item = null;
                    SelectItem(select_list_item);
                    is_refresh = true;
                }

            }
            else
            {
                var res = MessageBox.Show(select_list_item.text + "を削除しますか？", "確認", MessageBoxButtons.OKCancel);
                if (res == DialogResult.OK)
                {
                    AppCore.core.DeleteTag((ImageLibrary.Tag)select_list_item.tmp_data);
                    AppCore.core.SetSelectTagByTreeListTagOption();
                    select_list_item = null;
                    SelectItem(select_list_item);
                    is_refresh = true;
                }
            }

        }

        private void mouse_left_button_event_controller_SingleClick(MouseLeftButtonEventController ec)
        {
            Console.WriteLine("SingleClick " + new Random().Next(100));

            AppCore.core.ReleaseFocusTagSearchBox();

            var mouse_pos = ec.click_start_point;
            var target_item = CheckHitItem(mouse_pos.X, mouse_pos.Y);

            var items_height = items.Count() * line_height - hide_offset_y; // タグの全体の高さ
            
            {
                if (Hit.IsHit(mouse_pos.X, mouse_pos.Y, button_add_tag))
                {
                    Console.WriteLine("button_add_tag");
                    // タグ追加(+)
                    var tag = default(ImageLibrary.Tag);
                    if (select_list_item == null)
                    {
                        tag = AppCore.core.CreateTag("New Tag", null);
                    }
                    else
                    {
                        tag = AppCore.core.CreateTag("New Tag", (ImageLibrary.Tag)select_list_item.tmp_data);
                    }
                    var ti = GetListItemByTmpData(tag);
                    select_list_item = ti;
                    SelectItem(ti);
                    is_refresh = true;
                }
                else if (Hit.IsHit(mouse_pos.X, mouse_pos.Y, button_del_tag))
                {
                    Console.WriteLine("button_del_tag");
                    DeleteSelectItems();
                }
                else if (target_item == null)
                {
                    Console.WriteLine("target_item == null");
                    select_list_item = null;
                    select_list_items.Clear();
                    SelectItem(target_item);

                    is_refresh = true;
                }
                else if (0 <= mouse_pos.X && mouse_pos.X < items_offset_x)
                {
                    // View アイコン部分
                    Console.WriteLine("View アイコン部分");
                    var li = CheckHitItem(mouse_pos.X, mouse_pos.Y);
                    if (li != null)
                    {
                        var tag = (ImageLibrary.Tag)li.tmp_data;
                        switch (tag.show_mode)
                        {
                            case ImageLibrary.Tag.ShowMode.Show:
                                if (tag.is_show_chiled)
                                {
                                    SetTagShowMode(li, ImageLibrary.Tag.ShowMode.ShowChiled);
                                }
                                else
                                {
                                    SetTagShowMode(li, ImageLibrary.Tag.ShowMode.Hide);
                                }
                                break;
                            case ImageLibrary.Tag.ShowMode.ShowChiled:
                                SetTagShowMode(li, ImageLibrary.Tag.ShowMode.Hide);
                                break;
                            case ImageLibrary.Tag.ShowMode.Hide:
                                SetTagShowMode(li, ImageLibrary.Tag.ShowMode.Show);
                                break;
                            case ImageLibrary.Tag.ShowMode.Exclusion:
                                SetTagShowMode(li, ImageLibrary.Tag.ShowMode.Hide);
                                break;
                            case ImageLibrary.Tag.ShowMode.Star:
                                SetTagShowMode(li, ImageLibrary.Tag.ShowMode.Show);
                                break;
                        }

                        left_drag_show_and_hide_mode = tag.show_mode;
                        left_drag_show_and_hide.Clear();
                        left_drag_show_and_hide.Add(li);
                        Refresh();
                    }

                }
                else if (items_offset_x <= mouse_pos.X && mouse_pos.X < items_offset_x + line_depth_w * (target_item.depth))
                {
                    // 階層で空いたスペース
                    Console.WriteLine("階層で空いたスペース");
                    select_list_item = null;
                    select_list_items.Clear();
                    SelectItem(target_item);

                    is_refresh = true;
                }
                else if ((target_item.chiled_list.Count > 0) && (line_depth_w * (target_item.depth + 2) + items_offset_x > mouse_pos.X))
                {
                    // 子タグの展開
                    Console.WriteLine("子タグの展開");
                    if (target_item.chiled_list.Count != 0)
                    {
                        if (target_item.is_chiled_show) SetViewChiledItems( target_item , false);
                        else SetViewChiledItems(target_item, true);

                        //if (target_item.is_chiled_show)
                        //{
                        //    // 開く
                        //    ShowChiledItems(target_item);
                        //}
                        //else
                        //{
                        //    // 閉じる
                        //    HideChiledItems(target_item);
                        //}

                        //UpdateListSourtAsParent();
                    }
                    is_refresh = true;
                }
                else
                {
                    if (ec.IsCtrlKey())
                    {
                        Console.WriteLine("Select CtrlKey");
                        if (!IsSelected(target_item))
                        {

                            old_select_list_item = select_list_item;
                            select_list_item = target_item;
                            SelectItem(target_item);
                            is_refresh = true;

                            if ((select_list_items.Count() == 0) && old_select_list_item != null)
                            {
                                Console.WriteLine("a");
                                select_list_items.Add(old_select_list_item); // 複数選択1回目は直前の選択アイテムをふくめて選択を開始とする
                                select_list_items.Add(target_item);
                            }
                            else if (select_list_items.IndexOf(target_item) >= 0)
                            {
                                Console.WriteLine("b");
                                select_list_items.Remove(target_item);
                            }
                            else
                            {
                                Console.WriteLine("c");
                                select_list_items.Add(target_item);
                            }
                        }
                        else
                        {
                            select_list_item = target_item;
                            select_item_release_timer.Start();
                            is_refresh = true;
                            //select_list_items.Remove(hover_list_item);
                        }

                    }
                    else
                    {
                        Console.WriteLine("Select");

                        if (!IsSelected(target_item))
                        {
                            // アイテムの単体選択
                            select_list_item = target_item;
                            SelectItem(target_item);
                            if (select_list_items.Count() > 0) select_list_items.Clear();
                            is_refresh = true;
                        }
                        else
                        {
                            // アイテムの単体選択の解除
                            select_list_item = null;
                            SelectItem(null);
                            if (select_list_items.Count() > 0) select_list_items.Clear();
                            is_refresh = true;
                        }
                    }
                }
            }

            return;
        }

        private void mouse_left_button_event_controller_DoubleClick(MouseLeftButtonEventController ec)
        {
            Console.WriteLine("DoubleClick " + new Random().Next(100));

            var mouse_pos = ec.mouse_point;
            var target_item = CheckHitItem(mouse_pos.X, mouse_pos.Y);

            // ダブルクリック
            select_list_item = target_item;
            select_item_release_timer.Stop();
            EditTagName();
        }

        private void mouse_left_button_event_controller_DragStart(MouseLeftButtonEventController ec)
        {
            Console.WriteLine("DragStart " + new Random().Next(100));

            var mouse_pos = ec.drag_start_point;
            var target_item = CheckHitItem(mouse_pos.X, mouse_pos.Y);

            if (mouse_pos.X <= items_offset_x)
            {
                is_mouse_down_left_drag_show_and_hide = true;
            }
            else if (target_item!=null && (items_offset_x <= mouse_pos.X && mouse_pos.X < items_offset_x + line_depth_w * (target_item.depth)))
            {
            }
            else
            {
                is_mouse_down_left_drag_item_move = true;

                if (select_list_item == null)
                {
                    Console.WriteLine("a " + new Random().Next(100));
                    select_list_item = target_item;
                    SelectItem(target_item);
                    is_refresh = true;
                }
                else if (select_list_item != target_item)
                {
                    Console.WriteLine("b " + new Random().Next(100));
                    select_list_item = target_item;
                    SelectItem(target_item);
                    is_refresh = true;

                }
                else
                {
                    Console.WriteLine("b " + new Random().Next(100));
                }
            }

        }

        private void mouse_left_button_event_controller_Drag(MouseLeftButtonEventController ec)
        {
            Console.WriteLine("Drag " + new Random().Next(100));

            var mx = ec.drag_now_point.X;
            var my = ec.drag_now_point.Y + scrollbar.Value;

            if (is_mouse_down_left_drag_item_move)
            {
                var scroll_area = 100;
                if ((ec.drag_now_point.X == ec.drag_start_point.X) && (ec.drag_now_point.Y == ec.drag_start_point.Y))
                {
                    // 移動されてない
                }
                else
                {
                    if (ec.drag_now_point.Y < scroll_area) SetScroll(scrollbar.Value - 10);
                    if (ec.drag_now_point.Y > Size.Height - scroll_area) SetScroll(scrollbar.Value + 10);

                    var yy = ((ec.drag_now_point.Y + scrollbar.Value) / line_height);
                    var ymod = (ec.drag_now_point.Y + scrollbar.Value) % line_height;
                    var margin = 6;

                    var i = 0;
                    foreach (var li in items)
                    {
                        if (li.is_show == false) continue;
                        if (yy == i)
                        {
                            if (ymod < margin)
                            {
                                Console.WriteLine("a " + new Random().Next(100));
                                mouse_down_left_drag_event_insert_type = InsertType.UpperInsert;
                            }
                            else if (line_height - margin <= ymod)
                            {
                                Console.WriteLine("b " + new Random().Next(100));
                                mouse_down_left_drag_event_insert_type = InsertType.UnderInsert;
                            }
                            else
                            {
                                Console.WriteLine("c " + new Random().Next(100));
                                mouse_down_left_drag_event_insert_type = InsertType.InChiled;
                            }
                            mouse_down_left_drag_target_item = li;
                            is_refresh = true;
                            break;
                        }
                        i++;
                    }
                }
            }

            if (is_mouse_down_left_drag_show_and_hide)
            {
                var li = CheckHitItem(ec.mouse_point.X, ec.mouse_point.Y);
                if (li != null)
                {
                    var is_ok = true;
                    foreach (var li2 in left_drag_show_and_hide)
                    {
                        if (li == li2)
                        {
                            is_ok = false;
                            break;
                        }
                    }
                    if (is_ok)
                    {
                        var tag = (ImageLibrary.Tag)li.tmp_data;
                        SetTagShowMode(li, left_drag_show_and_hide_mode);
                        left_drag_show_and_hide.Add(li);
                        Refresh();
                    }
                }

            }
        }

        private void mouse_left_button_event_controller_DragEnd(MouseLeftButtonEventController ec)
        {
            Console.WriteLine("DragEnd " + new Random().Next(100));
            if (is_mouse_down_left_drag_item_move)
            {
                if (select_list_item == null)
                {
                }
                else if (
                    (mouse_down_left_drag_event_insert_type != InsertType.None)
                    && mouse_down_left_drag_target_item != null)
                {
                    Console.WriteLine("a " + new Random().Next(100));
                    if (select_list_items.Count() >= 2)
                    {
                        foreach (var sl in select_list_items)
                        {
                            MoveItem(
                                sl,
                                mouse_down_left_drag_target_item,
                                mouse_down_left_drag_event_insert_type);
                        }
                    }
                    else
                    {
                        MoveItem(
                            select_list_item,
                            mouse_down_left_drag_target_item,
                            mouse_down_left_drag_event_insert_type);
                        mouse_down_left_drag_event_insert_type = InsertType.None;
                        mouse_down_left_drag_target_item = null;
                        //select_list_item = null;
                    }
                    is_refresh = true;
                }
                is_mouse_down_left_drag_item_move = false;
            }

            if(is_mouse_down_left_drag_show_and_hide)
            {
                is_mouse_down_left_drag_show_and_hide = false;
            }

        }

    }
}
