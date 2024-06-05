using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Drawing;
using System.Threading;

using Blastagon.Common;
using Blastagon.App;

namespace Blastagon.ThreadManager
{
    public class SaveLoadManager : IDisposable
    {
        static SaveLoadManager save_load_manager;

        public const string NONE_TAG_IMAGE_IN_TAG_NAME = "未登録";

        volatile LockList<string> load_image_by_drop_file_list = new LockList<string>(new List<string>());
        volatile LockList<string> load_image_by_file_path_list = new LockList<string>(new List<string>());
        //volatile object load_image_lock = new object();

        volatile int load_image_file_counter = 0;
        volatile int load_image_file_counter_max = 0;

        // スレッド制御、予約や解放
        Thread loop_thread;
        volatile bool loop_thread_end = false;

        public SaveLoadManager()
        {
            save_load_manager = this;

            loop_thread = new Thread(new ThreadStart(LoopThread));
            loop_thread.Start();

        }

        private void LoopThread()
        {
            while (!loop_thread_end)
            {
                var sleep = 0;
                sleep = 200;
                if (load_image_by_drop_file_list.Count() > 0)
                {
                    sleep = 0;
                    var file_path = load_image_by_drop_file_list.PopFront();//[0];
                    //load_image_by_drop_file_list.RemoveAt(0);

                    LoopThread_Drop_LoadImageByDrop(file_path);

                    //var files = System.IO.Directory.GetFiles(path, "*", System.IO.SearchOption.AllDirectories);
                    AppCore.core.SetFormTitle(load_image_file_counter + " / " + load_image_file_counter_max);
                    if ( App.AppCore.core.progress_panel!=null)
                    {
                        App.AppCore.core.progress_panel.side_message = load_image_file_counter + " / " + load_image_file_counter_max;
                    }
                    WaitSleep.Do(sleep);
                    continue;
                }
                //WaitSleep.Do(sleep);

                //sleep = 200;
                if ( load_image_by_file_path_list.Count() > 0)
                {
                    sleep = 0;
                    var file_path = load_image_by_file_path_list.PopFront();

                    LoopThread_LoadImageByFilePath(file_path);
                }
                WaitSleep.Do(sleep);

            }
        }

        private void LoopThread_LoadImageByFilePath( string file_path )
        {
            if (AppCore.core.image_library.image_datas.ContainsKey(file_path))
            {
                // ファイル重複
                var image_data = AppCore.core.image_library.image_datas[file_path];
                if (AppCore.core.thumbnail_view.ExistImage(image_data))
                {
                    AppCore.core.popup_log.AddMessage("既にリンクされたデータです:" + file_path);
                    AppCore.core.popup_log.AddMessage("また、サムネイルにも表示されています");
                }
                else
                {
                    AppCore.core.popup_log.AddMessage("既にリンクされたデータです:" + file_path);
                    AppCore.core.popup_log.AddMessage("サムネイルに追加しました");
                    AppCore.core.thumbnail_view.AddImages( new ImageLibrary.ImageDataExWordSet( image_data, ""));
                    AppCore.core.thumbnail_view.is_reflesh_body = true;
                }
            }
            else
            {
                var plugin = Blastagon.PluginFileConector.FileConectorManager.GetFileConector(file_path, false);

                if (plugin == null)
                {
                    AppCore.core.popup_log.AddMessage("次のファイルが読み込めません:" + file_path);
                    AppCore.core.thumbnail_view.is_reflesh_body = true;
                }

                //var bitmap = plugin.image_conector.FromFile(null, file_path, "");
                //var thumbnail_image = AppCore.core.thumbnail_view.AddImages(file_path, bitmap.Width, bitmap.Height, "");
                //bitmap.Dispose();

                //if (thumbnail_image == null) return;
                var thumbnail_image = (UI.ThumbnailImage)null;
                try
                {
                    var bitmap = plugin.image_conector.FromFile(null, file_path, "");
                    thumbnail_image = AppCore.core.thumbnail_view.AddImages(file_path, bitmap.Width, bitmap.Height, "");
                    bitmap.Dispose();

                    if (thumbnail_image == null)
                    {
                        AppCore.core.popup_log.AddMessage("次のファイルが読み込めません 2:" + file_path);
                        AppCore.core.thumbnail_view.is_reflesh_body = true;
                        load_image_file_counter++;
                        return;
                    }
                }
                catch
                {
                    AppCore.core.popup_log.AddMessage("次のファイルが読み込めません 3:" + file_path);
                    AppCore.core.thumbnail_view.is_reflesh_body = true;
                    load_image_file_counter++;
                    return;
                }

                var is_tag_info_load = true;
                if (is_tag_info_load) {

                    var tags = plugin.tag_conector.Read(file_path);

                    foreach (var tag in tags)
                    {
                        if (tag.name == "") continue;

                        var library_tag = AppCore.core.image_library.GetTag(tag.name);
                        if (library_tag == null)
                        {
                            library_tag = AppCore.core.image_library.AddTag(tag.name);
                        }
                        thumbnail_image.data.image_data.tags.Add(new ImageLibrary.ImageTag(library_tag, tag.ex_word));
                    }
                }

                // タグが無い場合、「未登録」タグに、タグを入れる
                if (thumbnail_image.data.image_data.tags.Count == 0)
                {
                    var tag_name = NONE_TAG_IMAGE_IN_TAG_NAME;
                    var library_tag = AppCore.core.image_library.GetTag(tag_name);
                    if (library_tag == null)
                    {
                        library_tag = AppCore.core.image_library.AddTag(tag_name);
                    }
                    thumbnail_image.data.image_data.tags.Add(new ImageLibrary.ImageTag(library_tag, ""));
                }

                AppCore.core.image_library.image_datas.Add(file_path, thumbnail_image.data.image_data);


            }

            load_image_file_counter++;
            if (App.AppCore.core.progress_panel != null)
            {
                App.AppCore.core.progress_panel.side_message = load_image_file_counter + " / " + load_image_file_counter_max;
                App.AppCore.core.progress_panel.value = load_image_file_counter;
                App.AppCore.core.progress_panel.value_max = load_image_file_counter_max;
                App.AppCore.core.progress_panel.RefleshByOtherThread();
                if (load_image_file_counter == load_image_file_counter_max)
                {
                    load_image_file_counter = 0;
                    load_image_file_counter_max = 0;
                    App.AppCore.core.ProgressPanelDispose();

                    // tree_listのタグがうまく更新できていない場合があるので
                    // 全て削除して、一通り入れ直す
                    // とすると、リストの子供の開閉状態が引き継げない...
                    App.AppCore.core.UpdateTreeListByImageLibraryTagForce();
                    App.AppCore.core.tree_list.is_refresh = true;
                    // todo : 本来ここで tree_list.Refleshを入れたいが、…スレッドが違うのでちょっとややこしいInvoekがいる
                }

                AppCore.core.image_library.UpdateTagStatus();
            }
        }

        private void LoopThread_Drop_LoadImageByDrop(string path)
        {
            if (File.Exists(path))
            {
                LoopThread_Drop_LoadImageByDrop_Sub(path);
            }
            else if (Directory.Exists(path))
            {
                var files = System.IO.Directory.GetFiles(path, "*", System.IO.SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    LoopThread_Drop_LoadImageByDrop_Sub(file);
                }
            }

        }

        private void LoopThread_Drop_LoadImageByDrop_Sub(string path)
        {
            // 型チェック
            var f = path.ToUpper();
            var is_ok = false;
            if (f.IndexOf(".PNG") > 0) is_ok = true;
            if (f.IndexOf(".JPEG") > 0) is_ok = true;
            if (f.IndexOf(".JPG") > 0) is_ok = true;
            if (!is_ok) return;
            SaveLoadManager.LoadImageByFilePath(path);
        }


        public static void LoadImageByFilePath( string file_path )
        {
            save_load_manager.load_image_file_counter_max++;
            save_load_manager.load_image_by_file_path_list.Add(file_path);
        }


        public static void LoadImageByDrop(string[] file_path_array)
        {
            foreach (var file_path in file_path_array)
            {
                save_load_manager.load_image_by_drop_file_list.Add(file_path);
            }
        }

        public void Dispose()
        {
            loop_thread_end = true;
        }
    }
}
