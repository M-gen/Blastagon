using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;

using Blastagon.Common;

namespace Blastagon.App
{

    public class ImageLibrary
    {
        volatile int tag_update_count = 0;  // タグの更新カウンター（0にして更新するので、0でなければ更新が残っていることになる）
        volatile object tag_update_lock = new object();

        // タグの基本情報
        public class Tag
        {
            public enum ShowMode
            {
                Hide,       // 非表示
                Show,       // 表示
                ShowChiled, // 子タグのみ表示、自身にしかなければ非表示
                Star,       // 星、星のタグどうしをAND合成して、表示するべきものを決める
                Exclusion,  // 除外　このタグを持つものは、他で表示されるとしても、表示しない
            }

            public string name;
            public Tag parent;
            public List<Tag> chiled = new List<Tag>();
            public bool is_show = true;                     // 表示されるタグかどうか
            public bool is_show_chiled = true;              
            public ShowMode show_mode = ShowMode.Show;      //
            public int belong_image_count = 0;              // タグに所属している画像数

            public string GetFullName()
            {
                if (parent != null)
                {
                    return parent.GetFullName() + @"\:" + name;
                }

                return name;
            }

            // target_tagが、src_tagのparentか、そのまた続くparentに、存在するか
            static public ImageLibrary.Tag CheckGroupTag(ImageLibrary.Tag target_tag, ImageLibrary.Tag src_tag)
            {
                if (src_tag.parent == null) return null;
                if (src_tag.parent == target_tag) return src_tag.parent;
                return CheckGroupTag(target_tag, src_tag.parent);
            }
        }

        // ファイルごとにもつタグの情報
        public class ImageTag
        {
            public Tag tag;
            public string ex_word; // Clip,30,30,30,30 --- --- // 半角スペースで区間をわけ、区間ごとに,でわけてパラメータとする
            public Rectangle clip; // 

            public ImageTag(Tag tag, string ex_word)
            {
                this.tag = tag;
                this.ex_word = ex_word;

                var ex_word_split = ex_word.Split(' ');
                foreach( var word in ex_word_split)
                {
                    var p = ex_word.Split(',');
                    switch (p[0])
                    {
                        case "Clip":
                            clip = new Rectangle(int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]));
                            break;
                    }
                }
            }

            public static string GetExWordByClip( Rectangle rect )
            {
                return string.Format("Clip,{0},{1},{2},{3}", rect.X, rect.Y, rect.Width, rect.Height); ;
            }
        }

        public class ImageData
        {
            public string file_path;
            //public List<Tag> tag = new List<Tag>();
            public List<ImageTag> tags = new List<ImageTag>();
            public Size size = new Size(0, 0);

            // todo: この削除以外にもいろいろな削除方法が必要
            // 該当タグを削除
            public void DeleteTag(Tag tag)
            {
                //tags.ForeachBreak((t) =>
                //{
                //    if (t == tag)
                //    {
                //        tags.Remove(t);
                //        return true;
                //    }
                //    return false;
                //});
                foreach ( var t in tags)
                {
                    if(t.tag==tag)
                    {
                        tags.Remove(t);
                        return;
                    }
                }
            }
        }

        // サムネイル作成のために一時的に必要になる
        // ImageDataとex_wordをまとめたクラス
        public class ImageDataExWordSet
        {
            public ImageData image_data;
            public string ex_word;
            public ImageDataExWordSet(ImageData image_data, string ex_word)
            {
                this.image_data = image_data;
                this.ex_word = ex_word;
            }

            //public void AnalyzeExWord(string ex_word, ref Rectangle clip, ref bool is_clip)
            public void AnalyzeExWord( ref Rectangle clip, ref bool is_clip)
            {
                is_clip = false;

                var ex_word_split = ex_word.Split(' ');
                foreach (var word in ex_word_split)
                {
                    var p = ex_word.Split(',');
                    switch (p[0])
                    {
                        case "Clip":
                            is_clip = true;
                            clip = new Rectangle(int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]));
                            break;
                    }
                }
            }
        }

        //private Dictionary<string, ImageData> _image_datas = new Dictionary<string, ImageData>();
        public LockDictionary<string, ImageData> image_datas = new LockDictionary<string, ImageData>(new Dictionary<string, ImageData>());
        //public List<Tag> _tags = new List<Tag>();
        public LockList<Tag> tags = new LockList<Tag>(new List<Tag>());
        public object tags_lock = new object();

        public LockList<ImageDataExWordSet> image_datas_pin = new LockList<ImageDataExWordSet>(new List<ImageDataExWordSet>()); // ピンどめされた画像
        // 

        public ImageLibrary()
        {
        }

        public Tag AddTag(string full_name)
        {

            //var tmp_full_name = full_name.Replace(@"\:", "\t"); // Splitのために1文字に置換…タブ(\t)はエスケープしやすいので
            var split_tag_name = SplitTagDir(full_name);

            var t = new Tag();
            if (split_tag_name.Count() > 0)
            {
                t.name = split_tag_name[split_tag_name.Count() - 1];
                if (split_tag_name.Count() > 1)
                {
                    t.parent = AddTag_GetAndAddParentTag(full_name);
                    t.parent.chiled.Add(t);
                }
            }
            else
            {
                t.name = full_name;
            }
            tags.Add(t);
            return t;
        }

        // 親となるべきタグの取得と、存在しない場合は作ってそれを返す
        private Tag AddTag_GetAndAddParentTag(string full_name)
        {
            var parent_full_name = GetParentTagDirFullName(full_name);
            var t = GetTag(parent_full_name);
            if (t == null) t = AddTag(parent_full_name);

            return t;
        }

        private string[] SplitTagDir(string full_name)
        {
            var tmp_full_name = full_name.Replace(@"\:", "\t"); // Splitのために1文字に置換…タブ(\t)はエスケープしやすいので
            var split_tag_name = tmp_full_name.Split('\t');

            return split_tag_name;
        }

        private string GetParentTagDirFullName(string full_name)
        {
            var split_tag_name = SplitTagDir(full_name);
            var conect_string = new string[split_tag_name.Count() - 1];
            var conected = "";
            for (var i = 0; i < conect_string.Count(); i++)
            {
                conected += split_tag_name[i];
                if (i != conect_string.Count() - 1)
                {
                    conected += @"\:";
                }
            }

            return conected;
        }

        public Tag GetTag(string full_name)
        {
            Tag tmp_tag = null;
            tags.ForeachBreak((t) =>
            {
                if (t.GetFullName() == full_name)
                {
                    tmp_tag =  t;
                    return true;
                }
                return false;
            });
            return tmp_tag;
        }


        public void ChangeTagParent(Tag target, Tag new_parent, Tag old_parent)
        {
            if (old_parent != null)
            {
                old_parent.chiled.Remove(target);
            }

            if (new_parent != null)
            {
                new_parent.chiled.Add(target);
            }
            target.parent = new_parent;

        }

        public void UpdateTagStatus()
        {
            lock (tag_update_lock)
            {
                if (tag_update_count == 0)
                {
                    tag_update_count++;

                    var thread = new System.Threading.Thread(new System.Threading.ThreadStart(UpdateTagStatus_Core));
                    thread.Start();
                }
                else
                {
                    tag_update_count++;
                }
            }
        }

        private void UpdateTagStatus_Core()
        {
            while (true)
            {
                lock (tag_update_lock)
                {
                    if (tag_update_count > 0)
                    {
                        tag_update_count = 0;
                    }
                    else
                    {
                        return;
                    }
                }
                foreach (var t in tags)
                {
                    t.belong_image_count = 0;
                    foreach( var id in image_datas)
                    {
                        foreach (var t2 in id.tags)
                        {
                            if (t == t2.tag)
                            {
                                t.belong_image_count++;
                                break;
                            }
                        }
                    }
                    //image_datas.Foreach((id) =>
                    //{
                    //    foreach (var t2 in id.Value.tags)
                    //    {
                    //        if (t == t2.tag)
                    //        {
                    //            t.belong_image_count++;
                    //            break;
                    //        }
                    //    }
                    //});
                }
            }
        }

        // ピン留めされているかどうか
        public bool IsPinOn( ImageDataExWordSet image_data_ex_word )
        {
            var is_ok = false;
            image_datas_pin.ForeachBreak((id) =>
            {
                if (id== image_data_ex_word)
                {
                    is_ok = true;
                    return true;
                }
                return false;

            });
            return is_ok;
        }

        public void RemoveImage(string file_path)
        {
            var i1 = AppCore.core.image_library.image_datas.Count();
            AppCore.core.image_library.image_datas.Remove(file_path);
            var i2 = AppCore.core.image_library.image_datas.Count();
            Console.WriteLine("{0} {1} {2}",file_path, i1, i2);
        }
    }
}
