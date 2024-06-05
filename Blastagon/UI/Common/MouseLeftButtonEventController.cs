using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Windows.Forms;

namespace Blastagon.UI.Common
{
    /// <summary>
    /// マウスの右クリックの挙動について、クリック、ダブルクリック、ドラッグのイベントを、
    /// 競合しないようにまとめたもの
    /// </summary>
    public class MouseLeftButtonEventController
    {
        // 状態
        public enum State{
            None,
            MouseDownAfter_DragWait,        // マウスダウンによる、ドラッグの可能性がある
            MouseMoveAfter_Drag,            // マウスダウン後のマウスムーブ *
            MouseMoveAfter_DragEnd,         // 「マウスダウン後のマウスムーブ」後のマウスアップ、でのドラッグ確定処理 *
            MouseMoveAfter_DragCansel,      // 「マウスダウン後のマウスムーブ」のキャンセル
            MouseUpAfter_SingleWait,        // マウスアップによる、シングルクリックの可能性がある
            MouseDownAfter_DoubleWait,      // マウスダウン（ダブルクリックタイミング）による、シングルクリックの可能性がなくなった、ダブルクリック確定
            Timer_Do,                       // タイマー実行
            Timer_Do_SingleClick,           // タイマーによるシングルクリック確定処理 *
            Timer_Do_DoubleClick,           // タイマーによるダブルクリック確定処理 *
        };

        public State state = State.None;
        public Point drag_start_point;
        public Point drag_now_point;
        public Point click_start_point;
        public Point mouse_point;

        //public Point click_start_point_by_screen; // スクリーン座標でのマウス位置

        public Action<MouseLeftButtonEventController> event_single_crick;
        public Action<MouseLeftButtonEventController> event_double_crick;
        public Action<MouseLeftButtonEventController> event_drag_start;
        public Action<MouseLeftButtonEventController> event_drag;
        public Action<MouseLeftButtonEventController> event_drag_end;
        public System.Windows.Forms.Timer timer;

        // 下記の考慮したいけど、めんどうなのでキャンセル…
        // ドラッグ操作として認識されるために、マウスを移動する必要がある
        // 距離を表す四角形のサイズ (ピクセル単位)
        // SystemInformation.DragSize

        private Size config_drag_size;

        public MouseLeftButtonEventController()
        {
            timer = new Timer();
            //timer.Interval = SystemInformation.DoubleClickTime; // ダブルクリックの期間を最大とする
            timer.Interval = 100; // 長いので...直指定...
            timer.Tick += Timer_Tick;
            config_drag_size = SystemInformation.DragSize;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            //public void Update_Timer(int mouse_x, int mouse_y)
            //{
            //mouse_point = new Point(mouse_x, mouse_y);

            timer.Stop();
            switch (state)
            {
                default:
                    break;
                case State.MouseUpAfter_SingleWait:
                    if (event_single_crick != null) event_single_crick(this);
                    state = State.None;
                    break;
                case State.MouseDownAfter_DoubleWait:
                    // 本当にダブルクリックとして扱えるか → 移動していないか

                    if (event_double_crick != null) event_double_crick(this);
                    state = State.None;
                    break;
            }
        }
        //    }

        public void Update_MouseDown(int mouse_x, int mouse_y)
        {
            mouse_point = new Point(mouse_x, mouse_y);

            switch (state)
            {
                default:
                    drag_start_point = new Point(mouse_x, mouse_y);
                    click_start_point = drag_start_point;
                    //click_start_point_by_screen = new Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
                    state = State.MouseDownAfter_DragWait; // 初期状態からは、ドラッグするかどうかの待ちになる
                    break;
                case State.MouseUpAfter_SingleWait:
                    // 本当にダブルクリックとして扱えるか → 移動していないか
                    // マウスが移動しているかどうかを確認する
                    if (IsDragSizeOver(click_start_point,mouse_point) ){
                        Console.WriteLine("A");
                        // 一旦シングルクリックの処理をしてしまう
                        var tmp_mouse_point = mouse_point;
                        mouse_point = click_start_point;

                        if (event_single_crick != null) event_single_crick(this);

                        click_start_point = tmp_mouse_point;
                        mouse_point       = tmp_mouse_point;

                    }
                    else
                    {
                        Console.WriteLine("B");
                        state = State.MouseDownAfter_DoubleWait; // マウスアップ後にタイマー前にさらにマウスダウンに入った → ダウブルクリック確定
                    }

                    break;
            }
        }

        public void Update_MouseUp(int mouse_x, int mouse_y)
        {
            mouse_point = new Point(mouse_x, mouse_y);

            // クリックタイマー開始
            // ダブルクリックへの状態確定
            // ドラッグ系処理の完了
            switch (state)
            {
                default:
                    break;

                case State.MouseDownAfter_DragWait: // ドラッグ（ムーブ）の可能性が消えて、シングルクリックかダブルクリックのどちらか、そのためタイマーを回す
                    state = State.MouseUpAfter_SingleWait;
                    timer.Start();
                    break;

                case State.MouseMoveAfter_Drag: // ドラッグ中
                    drag_now_point = new Point(mouse_x, mouse_y);
                    if (event_drag_end != null) event_drag_end(this);
                    state = State.None;
                    break;

                    //case State.MouseUpAfter_SingleWait: // マウスアップしてシングルクリック以上が確定後で、さらにマウスアップがあったので、ダブルクリックとする
                    //    //state = State.MouseDownAfter_DoubleWait;
                    //    break;
            }
        }

        public void Update_MouseMove(int mouse_x, int mouse_y)
        {
            mouse_point = new Point(mouse_x,mouse_y);

            switch (state)
            {
                default:
                    break;

                case State.MouseDownAfter_DragWait:
                    //if ((mouse_x != drag_start_point.X) || (mouse_y != drag_start_point.Y)) // 移動を検出ドラッグ中になる
                    if (IsDragSizeOver(mouse_point,drag_start_point))
                    {
                        timer.Stop();
                        drag_now_point = new Point(mouse_x, mouse_y);
                        state = State.MouseMoveAfter_Drag; 
                        if (event_drag_start != null) event_drag_start(this);
                    }
                    break;

                case State.MouseMoveAfter_Drag: // ドラッグ中
                    drag_now_point = new Point(mouse_x, mouse_y);
                    if (event_drag != null) event_drag(this);
                    break;

                case State.MouseUpAfter_SingleWait:  // すでにマウスは離された後
                    break;

                case State.MouseDownAfter_DoubleWait: // ダブルクリック確定後に、タイマーイベント前にマウスが動かされた → とくに何もしない
                    break;
            }
        }



        
        public bool IsCtrlKey()
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                return true;
            }
            return false;
        }

        public bool IsShiftKey()
        {
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                return true;
            }
            return false;
        }

        public void Reset()
        {
            state = State.None;
            timer.Stop();
        }

        private bool IsDragSizeOver( Point a, Point b )
        {
            var x = Math.Abs(a.X - b.X);
            var y = Math.Abs(a.Y - b.Y);
            if ( ( x >= config_drag_size.Width) || (y >= config_drag_size.Height) )
            {
                return true;
            }

            return false;
        }
    }
}
