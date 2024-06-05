using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using System.Drawing;

using Blastagon.App;
using Blastagon.Common;

namespace Blastagon.PluginFileConector
{

    public class Plugin
    {

        //public InfoBase info;
        public dynamic info;
        //public TagConectorBase tag_conector;
        public dynamic tag_conector;
        //public ImageConectorBase image_conector;
        public dynamic image_conector;

    }


    public class FileConectorManager
    {
        static FileConectorManager manager;

        const string LOAD_PLUGIN_DIR = @"data/plugin_file_conector/";

        LockList<Plugin> plugins = new LockList<Plugin>(new List<Plugin>());
        LockDictionary<string, Plugin> plugins_by_file_type = new LockDictionary<string, Plugin>(new Dictionary<string, Plugin>()); // ファイル拡張子ごとに対応プラグインを辞書でわりふったもの

        public FileConectorManager()
        {
            manager = this;
            var plugin_files = System.IO.Directory.GetFiles(@"data/plugin_file_conector", "*.dll.plugin", System.IO.SearchOption.AllDirectories);

            foreach( var file in plugin_files)
            {
                Assembly asm = Assembly.LoadFrom(file);
                Module[] mods   = asm.GetModules();

                var plugin = new Plugin();
                {
                    var mod = mods[0];
                    {
                        var type = mod.GetType("Plugin.Info");
                        if (type != null)
                        {
                            var p = Activator.CreateInstance(type);
                            plugin.info = p;

                        }
                    }

                    {
                        var type = mod.GetType("Plugin.TagConector");
                        if (type != null)
                        {
                            var p = Activator.CreateInstance(type);
                            plugin.tag_conector = p;
                        }
                    }

                    {
                        var type = mod.GetType("Plugin.ImageConector");
                        if (type != null)
                        {
                            var p = Activator.CreateInstance(type);
                            plugin.image_conector = p;
                        }
                    }


                }
                plugins.Add(plugin);
                var file_types = plugin.info.file_type.Split(',');
                foreach(var file_type in file_types)
                {
                    var file_type_upper = file_type.ToUpper();
                    if (plugins_by_file_type.ContainsKey(file_type_upper))
                    {
                        // 上書き、後で読み込まれるプラグインを優先する
                        plugins_by_file_type[file_type_upper] = plugin;
                    }
                    else
                    {
                        plugins_by_file_type.Add(file_type_upper, plugin);
                    }
                }
            }

        }

        // ファイルから、それにあった、プラグインを取得する
        static public Plugin GetFileConector( string file_path, bool is_deap_check)
        {
            var up_file_path = file_path.ToUpper();
            var type = GetFileType(file_path);
            // todo : 速度面での足かせになるところ、ファイルの判定を精確にやってないのが微妙、拡張子でしか判断していない
            //      管理データ構築時に、ファイル形式を断定しておけば早くなるんだけど、やってない...
            if (manager.plugins_by_file_type.ContainsKey(type))
            {
                return manager.plugins_by_file_type[type];
            }
            else
            {
                return null;
            }
        }

        static private string GetFileType(string file_path)
        {
            var up = file_path.ToUpper();
            return up.Substring(up.LastIndexOf('.')+1);
        }
    }
}
