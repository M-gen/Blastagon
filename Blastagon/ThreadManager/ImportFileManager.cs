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
    public class ImportFileManager : IDisposable
    {
        class ImportFileData
        {
            public string file_path;
            //public string relative_directory; // 相対ディレクトリパス
            public ImageLibrary.Tag tag;
        }

        class ImportFileDataSet
        {
            public List<ImportFileData> file_datas = new List<ImportFileData>();
            //public Dictionary<string, ImageLibrary.Tag> tags = new Dictionary<string, ImageLibrary.Tag>();
            public string root_directory = "";

            public ImportFileDataSet( string[] files, ImageLibrary.Tag parent_tag)
            {
                {
                    var path = files[0];
                    root_directory = path.Substring(0, path.LastIndexOf('\\'));
                }

                foreach (var path in files)
                {
                    if (File.Exists(path))
                    {
                        var fd = new ImportFileData();
                        fd.file_path = path;
                        //fd.relative_directory = "";//
                        fd.tag = parent_tag;
                        file_datas.Add(fd);
                    }
                    else if (Directory.Exists(path))
                    {
                        //var files2 = System.IO.Directory.GetFiles(path, "*", System.IO.SearchOption.AllDirectories);
                        var files2 = System.IO.Directory.GetFiles(path, "*", System.IO.SearchOption.TopDirectoryOnly);
                        files2 = files2.Concat(System.IO.Directory.GetDirectories(path, "*", System.IO.SearchOption.TopDirectoryOnly)).ToArray();
                        InitDirectoryFiles(files2, path, parent_tag);
                    }

                }

            }

            private ImageLibrary.Tag GetNextTag(string file_directory, ImageLibrary.Tag parent_tag)
            {
                var tag_name = file_directory.Substring(file_directory.LastIndexOf('\\') + 1);
                var target_tag = default(ImageLibrary.Tag);
                foreach (var tag in parent_tag.chiled)
                {
                    if (tag.name == tag_name)
                    {
                        target_tag = tag;
                    }
                }
                if (target_tag == null)
                {
                    target_tag = new ImageLibrary.Tag();
                    target_tag.name = tag_name;
                    target_tag.parent = parent_tag;

                    // リスト側にも上手く追加しておく
                    var parent_li = AppCore.core.tree_list.GetListItemByTmpData(parent_tag);
                    //var up_list_item = AppCore.core.tree_list.GetListItemByTmpData(up_item);
                    var li = AppCore.core.tree_list.AddItemToParent(tag_name, parent_li);
                    li.tmp_data = target_tag;

                }
                return target_tag;
            }

            public void InitDirectoryFiles(string[] files, string file_directory , ImageLibrary.Tag parent_tag)
            {
                var target_tag = GetNextTag(file_directory, parent_tag);
                foreach (var path in files)
                {
                    if (File.Exists(path))
                    {
                        var fd = new ImportFileData();
                        fd.file_path = path;
                        fd.tag = target_tag;
                        file_datas.Add(fd);
                    }
                    else if (Directory.Exists(path))
                    {
                        //var files2 = System.IO.Directory.GetFiles(path, "*", System.IO.SearchOption.AllDirectories);
                        var files2 = System.IO.Directory.GetFiles(path, "*", System.IO.SearchOption.TopDirectoryOnly);
                        files2 = files2.Concat(System.IO.Directory.GetDirectories(path, "*", System.IO.SearchOption.TopDirectoryOnly)).ToArray();
                        InitDirectoryFiles(files2, path, target_tag);
                    }

                }

            }

        }

        // スレッド制御、予約や解放
        Thread import_thread = null;
        volatile bool import_thread_end = false;

        private class ImportFilesInTagTreeParam
        {
            public string[] files;
            public ImageLibrary.Tag parent_tag;
            public int file_count = 0;
            public ImportFilesInTagTreeParam(string[] files, ImageLibrary.Tag parent_tag)
            {
                this.files = files;
                this.parent_tag = parent_tag;
            }
        }

        public ImportFileManager()
        {
            
        }

        public void ImportFilesInTagTree(string[] files, ImageLibrary.Tag parent_tag)
        {
            if(import_thread!=null)
            {
                throw (new System.Exception("スレッドが未終了の段階での呼び出しがありました : ImportFilesInTagTree"));
            }
            import_thread = null;
            import_thread_end = false;

            var param = new ImportFilesInTagTreeParam(files, parent_tag);
            //ImportFilesInTagTree_Thread(files, parent_tag);

            import_thread = new Thread(new ParameterizedThreadStart(ImportFilesInTagTree_Thread));
            import_thread.Start(param);
        }

        // タグツリー側へファイルを組み込む場合に使う
        //private void ImportFilesInTagTree_Thread( string[] files, ImageLibrary.Tag parent_tag)
        private void ImportFilesInTagTree_Thread( object tmp)
        {
            var param = (ImportFilesInTagTreeParam)tmp;
            var data_set = new ImportFileDataSet(param.files, param.parent_tag);

            var count = 0;
            foreach (var data in data_set.file_datas)
            {
                if (import_thread_end) break;

                try
                {
                    ImportFilesInTagTree_Thread_InitFile(data);
                }
                catch
                {
                }

                count++;
                App.AppCore.core.progress_panel.side_message = $"{count} / {data_set.file_datas.Count()}";
                App.AppCore.core.progress_panel.value     = count;
                App.AppCore.core.progress_panel.value_max = data_set.file_datas.Count();
                App.AppCore.core.progress_panel.RefleshByOtherThread();
            }

            // tree_listのタグがうまく更新できていない場合があるので
            // 全て削除して、一通り入れ直す
            // とすると、リストの子供の開閉状態が引き継げない...
            //App.AppCore.core.UpdateTreeListByImageLibraryTagForce();
            App.AppCore.core.tree_list.is_refresh = true;

            //AppCore.core.image_library.UpdateTagStatus();

            // スレッドの終了なので、ProgressPanelを消去しておく
            App.AppCore.core.ProgressPanelDispose();
        }

        //private void ImportFilesInTagTree_Thread_FileCount(ref ImportFilesInTagTreeParam param)
        //{

        //}

        private void ImportFilesInTagTree_Thread_InitFile(ImportFileData data)
        {

            if (AppCore.core.image_library.image_datas.ContainsKey(data.file_path))
            {
                // ファイル重複
                var image_data = AppCore.core.image_library.image_datas[data.file_path];
                AppCore.core.popup_log.AddMessage("既にリンクされたデータです:" + data.file_path);
            }
            else
            {
                var plugin = Blastagon.PluginFileConector.FileConectorManager.GetFileConector(data.file_path, false);

                if (plugin == null)
                {
                    AppCore.core.popup_log.AddMessage("次のファイルが読み込めません:" + data.file_path);
                    AppCore.core.thumbnail_view.is_reflesh_body = true;
                }


                var thumbnail_image = (UI.ThumbnailImage)null;
                try
                {
                    var bitmap = plugin.image_conector.FromFile(null, data.file_path, "");
                    thumbnail_image = AppCore.core.thumbnail_view.AddImages(data.file_path, bitmap.Width, bitmap.Height, "");
                    bitmap.Dispose();

                    if (thumbnail_image == null)
                    {
                        AppCore.core.popup_log.AddMessage("次のファイルが読み込めません 2:" + data.file_path);
                        AppCore.core.thumbnail_view.is_reflesh_body = true;
                        return;
                    }
                }
                catch
                {
                    AppCore.core.popup_log.AddMessage("次のファイルが読み込めません 3:" + data.file_path);
                    AppCore.core.thumbnail_view.is_reflesh_body = true;
                    return;
                }

                var tags = plugin.tag_conector.Read(data.file_path);

                // 埋め込まれているタグに組み込む
                foreach (var tag in tags)
                {
                    if (import_thread_end) break;
                    if (tag.name == "") continue;

                    var library_tag = AppCore.core.image_library.GetTag(tag.name);
                    if (library_tag == null)
                    {
                        library_tag = AppCore.core.image_library.AddTag(tag.name);
                    }
                    thumbnail_image.data.image_data.tags.Add(new ImageLibrary.ImageTag(library_tag, tag.ex_word));
                }

                // タグリスト側へのドロップなので、そちら用の読込先にタグを入れる
                thumbnail_image.data.image_data.tags.Add(new ImageLibrary.ImageTag(data.tag, ""));

                //AppCore.core.image_library.image_datas.Add();
                AppCore.core.image_library.image_datas.Add(data.file_path, thumbnail_image.data.image_data);


                // tree_listのタグがうまく更新できていない場合があるので
                // 全て削除して、一通り入れ直す
                // とすると、リストの子供の開閉状態が引き継げない...

            }

        }


        private bool CheckUseAbleFile(string path)
        {
            // 型チェック
            var f = path.ToUpper();
            var is_ok = false;
            if (f.IndexOf(".PNG") > 0) is_ok = true;
            if (f.IndexOf(".JPEG") > 0) is_ok = true;
            if (f.IndexOf(".JPG") > 0) is_ok = true;
            if (!is_ok) return false;

            return true;
        }

        public void Dispose()
        {
            this.import_thread_end = true;
        }
    }
}
