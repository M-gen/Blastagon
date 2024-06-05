using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Blastagon.Common;

namespace Blastagon.App.RectangleSort
{
    public class Sort
    {
        public class SortItem 
        {
            public object item_key; // アイテムを識別するために任意のオブジェクトを埋め込んでおく
            public RectangleD rect = new RectangleD(0,0,0,0);
            public double priority;
            public double scale;


        }

        // 1つの並べ方情報をまとめたもの
        public class SortPackage : IComparable
        {
            public double wh_base; // 面積の目標値
            public double width;
            public double height;
            public List<SortItem> sort_items = new List<SortItem>(); // 優先順に並べた順
            public double scale;

            // IComparable.CompareToの実装
            public int CompareTo(object obj)
            {
                // 引数がnullの場合はArgumentNullExceptionをスローする
                if (obj == null) throw new ArgumentNullException();

                // 引数をAccountに型変換
                var p = obj as SortPackage;

                // 引数をAccountに型変換できなかった場合はArgumentExceptionをスローする
                if (p == null) throw new ArgumentException();

                if (this.wh_base==p.wh_base &&
                    this.width == p.width &&
                    this.height == p.height &&
                    this.scale == p.scale 
                    )
                {
                    return 0;
                }
                if(this.scale < p.scale)
                {
                    return -1;
                }
                else if (this.scale > p.scale)
                {
                    return 1;
                }
                return -1;

                // より簡単に、以下のように記述することもできる
                //return this.id - other.id;
                // また、Integer.CompareToメソッドの結果を使用するように記述することもできる
                //return this.id.CompareTo(other.id);
            }
        }

        Random random = new Random();
        public RectangleD place = new RectangleD(0,0,0,0);  // 配置場所の大きさを知るためのもの
        public List<SortItem> items = new List<SortItem>();
        public List<SortPackage> stock_pakage = new List<SortPackage>();

        public SortPackage select_package;

        public Sort()
        {

        }

        public void Do( double client_width, double client_hight )
        {
            var packages = new List<SortPackage>();

            for( var i=0; i<10; i++)
            {
                var package = NewPackage(client_width, client_hight);
                packages.Add(package);
            }

            for (var j = 0; j < 500; j++)
            {
                var new_packages = new List<SortPackage>();
                for (var i = 0; i < 40; i++)
                {
                    var p1 = packages[random.Next(packages.Count())];
                    var p2 = packages[random.Next(packages.Count())];
                    var package = NewPackageByMix(client_width, client_hight, p1, p2);
                    new_packages.Add(package);
                }

                // scaleの大きいものを順に、指定した数だけ選択する
                packages.Clear();
                //var query = new_packages.OrderByDescending(s => s.scale).ThenBy(s => s);
                var query = new_packages.OrderByDescending(s => s.scale).ThenBy(s => s);
                var q_i_num = 10;
                var q_i = 0;
                foreach (var p in query)
                {
                    packages.Add(p);
                    q_i++;
                    if (q_i >= q_i_num) break;
                }
            }

            var max_scale = 0.0;
            stock_pakage.Clear();
            foreach ( var p in packages)
            {
                if (max_scale < p.scale)
                {
                    max_scale = p.scale;
                    select_package = p;
                }
                stock_pakage.Add(p);
            }
            Console.WriteLine($"scale {select_package.scale}");
        }

        // 前回の情報を利用してさらに更新する
        public void DoMore(double client_width, double client_hight)
        {
            var packages = new List<SortPackage>();

            for (var i = 0; i < stock_pakage.Count(); i++)
            {
                var package = stock_pakage[i];
                packages.Add(package);
            }

            for (var j = 0; j < 500; j++)
            {
                var new_packages = new List<SortPackage>();
                for (var i = 0; i < 40; i++)
                {
                    var p1 = packages[random.Next(packages.Count())];
                    var p2 = packages[random.Next(packages.Count())];
                    var package = NewPackageByMix(client_width, client_hight, p1, p2);
                    new_packages.Add(package);
                }

                // scaleの大きいものを順に、指定した数だけ選択する
                packages.Clear();
                //var query = new_packages.OrderByDescending(s => s.scale).ThenBy(s => s);
                var query = new_packages.OrderByDescending(s => s.scale).ThenBy(s => s);
                var q_i_num = 10;
                var q_i = 0;
                foreach (var p in query)
                {
                    packages.Add(p);
                    q_i++;
                    if (q_i >= q_i_num) break;
                }
            }

            var max_scale = 0.0;
            var last_select_package = select_package;
            stock_pakage.Clear();
            foreach (var p in packages)
            {
                if (max_scale < p.scale)
                {
                    max_scale = p.scale;
                    select_package = p;
                }
                stock_pakage.Add(p);
            }

            if (select_package.scale > last_select_package.scale)
            {
                Console.WriteLine($"scale {select_package.scale} Update");
            }
            else
            {
                Console.WriteLine($"scale {select_package.scale} No Update");
                select_package = last_select_package;
                stock_pakage.Add(last_select_package);
            }
        }

        // パッケージ2つから遺伝的アルゴリズムのように
        // ミックスと、一部のランダムな変更により新しいパッケージを算出する
        private SortPackage NewPackageByMix(double client_width, double client_height, SortPackage p1, SortPackage p2)
        {
            var package = new SortPackage();
            var i_num = p1.sort_items.Count();
            
            for( var i=0; i < i_num; i++)
            {
                var p1i = p1.sort_items[i];

                var i2 = new SortItem();
                i2.item_key = p1i.item_key;
                i2.rect = new RectangleD(p1i.rect);
                i2.scale = p1i.scale;

                // どちらかを適応する
                if (random.Next(100) < 50 )
                {
                    i2.priority = p1.sort_items[i].priority;
                }
                else
                {
                    i2.priority = p2.sort_items[i].priority;
                }

                if (random.Next(100)<3)
                {
                    i2.priority = random.NextDouble() * 100;
                }

                package.sort_items.Add(i2);
            }

            // どちらかを適応する
            if (random.Next(100) < 50)
            {
                package.width = p1.width;
            }
            else
            {
                package.width = p2.width;
            }
            if (random.Next(100) < 15)
            {
                package.width = 200 + random.Next(1000);
            }

            NewPackage_Core(client_width, client_height, package);

            return package;
        }

        private SortPackage NewPackage( double client_width, double client_height)
        {
            var package = new SortPackage();
            package.wh_base = 100 * 100;
            package.width = 200 + random.Next(1000);

            // 大きさを算出する
            foreach (var i in items)
            {
                var i2 = new SortItem();
                i2.rect.Width = package.wh_base / i.rect.Width;

                var hpw = i.rect.Height / i.rect.Width;      // 縦横比を算出 縦を1とした場合の横の数
                var scale_hpw = Math.Sqrt(package.wh_base / (1 * hpw)); // 縦横比を基準に、基準となるwh_baseとの倍率を計算

                i2.item_key = i.item_key;
                i2.rect.Width = scale_hpw * 1.0;
                i2.rect.Height = scale_hpw * hpw;

                package.sort_items.Add(i2);
            }

            // 優先度を設定する
            foreach (var i in package.sort_items)
            {
                i.priority = random.NextDouble() * 100;
            }
            NewPackage_Core( client_width, client_height, package);

            //var query = package.sort_items.OrderBy(s => s.priority).ThenBy(s => s);

            //var line_x = new List<double>(); // 配置できる可能性のある横の座標
            //var line_y = new List<double>(); // 配置できる可能性のある縦の座標
            //line_x.Add(0);
            //line_y.Add(0);
            //var fix_items = new List<SortItem>();
            //foreach (var qi in query)
            //{

            //    // 直接 foreach を2重で回すとうまく動かないので、ひとまず、場所を列挙する
            //    var pairs = new List<KeyValuePair<double, double>>();
            //    foreach (var ly in line_y)
            //    {
            //        foreach (var lx in line_x)
            //        {
            //            pairs.Add(new KeyValuePair<double, double>(lx, ly));
            //        }
            //    }

            //    var is_add = false;
            //    foreach (var t_pos in pairs)
            //    {
            //        // (lx, ly) 座標を左上に、矩形が当てはめられるかを検証する
            //        var lx = t_pos.Key;
            //        var ly = t_pos.Value;

            //        // 横幅が収まっているかどうか
            //        if (lx + qi.rect.Width > package.width)
            //        {
            //            continue;
            //        }

            //        qi.rect.X = lx;
            //        qi.rect.Y = ly;

            //        // 他の矩形と重なるかどうか
            //        var is_hit = false;
            //        foreach (var fi in fix_items)
            //        {
            //            if (IsHit(fi.rect, qi.rect))
            //            {
            //                is_hit = true;
            //                break;
            //            }
            //        }

            //        if (!is_hit)
            //        {

            //            // 場所の確定
            //            is_add = true;

            //            AddLine(ref line_x, qi.rect.X + qi.rect.Width);
            //            AddLine(ref line_y, qi.rect.Y + qi.rect.Height);
            //            fix_items.Add(qi);

            //            break;
            //        }

            //    }
            //    if (!is_add)
            //    {
            //        var a = 0;
            //    }
            //}

            //// 高さを算出する
            //package.height = 0;
            //foreach (var i in package.sort_items)
            //{
            //    var h = i.rect.Height + i.rect.Y;
            //    if (package.height < h)
            //    {
            //        package.height = h;
            //    }
            //}

            //var client_hpw = (double)client_hight / (double)client_width;
            //var package_hpw = package.height / package.width;
            //if (client_hpw < package_hpw)
            //{
            //    // 高さに合わせる
            //    package.scale = client_hight / package.height;
            //}
            //else
            //{
            //    // 横幅に合わせる
            //    package.scale = client_width / package.width;
            //}

            return package;
        }

        private void NewPackage_Core(double client_width, double client_hight, SortPackage package)
        {
            //var package = new SortPackage();
            //package.wh_base = 100 * 100;
            //package.width = 200 + (random.Next(100) * 10);

            //// 大きさを算出する
            //foreach (var i in items)
            //{
            //    var i2 = new SortItem();
            //    i2.rect.Width = package.wh_base / i.rect.Width;

            //    var hpw = i.rect.Height / i.rect.Width;      // 縦横比を算出 縦を1とした場合の横の数
            //    var scale_hpw = Math.Sqrt(package.wh_base / (1 * hpw)); // 縦横比を基準に、基準となるwh_baseとの倍率を計算

            //    i2.item_key = i.item_key;
            //    i2.rect.Width = scale_hpw * 1.0;
            //    i2.rect.Height = scale_hpw * hpw;

            //    package.sort_items.Add(i2);
            //}

            //// 優先度を設定する
            //foreach (var i in package.sort_items)
            //{
            //    i.priority = random.NextDouble() * 100;
            //}
            var query = package.sort_items.OrderBy(s => s.priority).ThenBy(s => s);

            var line_x = new List<double>(); // 配置できる可能性のある横の座標
            var line_y = new List<double>(); // 配置できる可能性のある縦の座標
            line_x.Add(0);
            line_y.Add(0);
            var fix_items = new List<SortItem>();
            foreach (var qi in query)
            {

                // 直接 foreach を2重で回すとうまく動かないので、ひとまず、場所を列挙する
                var pairs = new List<KeyValuePair<double, double>>();
                foreach (var ly in line_y)
                {
                    foreach (var lx in line_x)
                    {
                        pairs.Add(new KeyValuePair<double, double>(lx, ly));
                    }
                }

                var is_add = false;
                foreach (var t_pos in pairs)
                {
                    // (lx, ly) 座標を左上に、矩形が当てはめられるかを検証する
                    var lx = t_pos.Key;
                    var ly = t_pos.Value;

                    // 横幅が収まっているかどうか
                    if (lx + qi.rect.Width > package.width)
                    {
                        continue;
                    }

                    qi.rect.X = lx;
                    qi.rect.Y = ly;

                    // 他の矩形と重なるかどうか
                    var is_hit = false;
                    foreach (var fi in fix_items)
                    {
                        if (IsHit(fi.rect, qi.rect))
                        {
                            is_hit = true;
                            break;
                        }
                    }

                    if (!is_hit)
                    {

                        // 場所の確定
                        is_add = true;

                        AddLine(ref line_x, qi.rect.X + qi.rect.Width);
                        AddLine(ref line_y, qi.rect.Y + qi.rect.Height);
                        fix_items.Add(qi);

                        break;
                    }

                }
                if (!is_add)
                {
                    var a = 0;
                }
            }

            // 高さを算出する
            package.height = 0;
            foreach (var i in package.sort_items)
            {
                var h = i.rect.Height + i.rect.Y;
                if (package.height < h)
                {
                    package.height = h;
                }
            }

            var client_hpw = (double)client_hight / (double)client_width;
            var package_hpw = package.height / package.width;
            if (client_hpw < package_hpw)
            {
                // 高さに合わせる
                package.scale = client_hight / package.height;
            }
            else
            {
                // 横幅に合わせる
                package.scale = client_width / package.width;
            }

        }

        private bool IsHit(RectangleD a, RectangleD b)
        {
            var mx1 = a.X;
            var my1 = a.Y;
            var mx2 = a.X + a.Width;
            var my2 = a.Y + a.Height;
            var ex1 = b.X;
            var ey1 = b.Y;
            var ex2 = b.X + b.Width;
            var ey2 = b.Y + b.Height;

            //if ( mx1 <= ex2 && ex1 <= mx2 && my1 <= ey2 && ey1 <= my2)
            if ( mx1 < ex2 && ex1 < mx2 && my1 < ey2 && ey1 < my2)
            {
                return true;
            }

            //if ( a.X < b.X + b.Width &&  b.X < a.X +  )

            //if ((Math.Abs(a.X - b.X) < a.Width / 2 + b.Width / 2) &&
            //      (Math.Abs(a.Y - b.Y) < a.Height / 2 + b.Height / 2))
            //{
            //    return true;
            //}

            return false;
        }

        private void AddLine( ref List<double> list, double value )
        {
            var pos = 0;
            foreach( var i in list)
            {
                if (i == value) return;
                if (value < i )
                {
                    list.Insert(pos, value);
                    return;
                }
                pos++;
            }
            list.Insert(pos, value);

        }

        //// 並び替えの定義
        //public class SortItemPriorityComparer : System.Collections.IComparer
        //{
        //    //xがyより小さいときはマイナスの数、大きいときはプラスの数、同じときは0を返す
        //    public int Compare(object x, object y)
        //    {
        //        //nullが最も小さいとする
        //        if (x == null && y == null)
        //        {
        //            return 0;
        //        }
        //        if (x == null)
        //        {
        //            return -1;
        //        }
        //        if (y == null)
        //        {
        //            return 1;
        //        }

        //        //String型以外の比較はエラー
        //        if (!(x is SortItem))
        //        {
        //            throw new ArgumentException("SortItem型でなければなりません。", "x");
        //        }
        //        else if (!(y is SortItem))
        //        {
        //            throw new ArgumentException("SortItem型でなければなりません。", "y");
        //        }

        //        var x1 = (SortItem)x;
        //        var y1 = (SortItem)y;

        //        return (int)(x1.priority - y1.priority);

        //        //文字列の長さを比較する
        //        //return ((string)x).Length.CompareTo(((string)y).Length);
        //        //または、次のようにもできる
        //        //return ((string)x).Length - ((string)y).Length;
        //    }
        //}
    }
}
