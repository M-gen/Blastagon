using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blastagon.JPEG
{
    public class JPEGText
    {
        string file_path;

        public JPEGText( string file_path )
        {
            this.file_path = file_path;
        }
        
        public string GetText( int exif_id )
        {
            using (var bmp = new System.Drawing.Bitmap(file_path)) {
                foreach (var item in bmp.PropertyItems)
                {
                    if (item.Id != exif_id) continue;
                    if ( item.Type==1 )
                    {
                        string val = System.Text.Encoding.Unicode.GetString(item.Value);
                        val = val.Trim( '\0' );
                        return val;
                    }
                    else if (item.Type==2 )
                    {
                        string val = System.Text.Encoding.ASCII.GetString(item.Value);
                        val = val.Trim( '\0' );
                        return val;

                    }
                }
            }
            return "";
        }

        public void SaveText( string dst_file_path, int exif_id, string value)
        {
            using (var bmp = new System.Drawing.Bitmap(file_path))
            {
                var is_ok = false;
                foreach (var item in bmp.PropertyItems)
                {
                    if (item.Id != exif_id) continue;

                    item.Id = exif_id;
                    item.Type = 1;
                    item.Value = System.Text.Encoding.Unicode.GetBytes(value + '\0');
                    bmp.SetPropertyItem(item); // これがないと適応されない？
                    is_ok = true;
                    break;
                }
                if (!is_ok)
                {
                    var item = bmp.PropertyItems[0];
                    item.Id = exif_id;
                    item.Type = 1;
                    item.Value = System.Text.Encoding.Unicode.GetBytes(value + '\0');
                    bmp.SetPropertyItem(item);
                }

                //保存する
                try
                {
                    bmp.Save(dst_file_path, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
                catch
                {
                    System.IO.File.Delete(dst_file_path);
                    bmp.Dispose();
                    throw (new System.Exception(file_path + " 一時ファイルの保存に失敗しました : PluginJPEGFileConector"));
                }

            }
        }

    }
}
