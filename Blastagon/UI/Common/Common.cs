using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;

namespace Blastagon.UI.Common
{
    public class Common
    {
        public class ScreenData
        {
            public System.Windows.Forms.Screen target_screen;
        }

        static public ScreenData ScreenCheck( Form form )
        {
            var screens = Screen.AllScreens;

            foreach (var screen in screens) {
                
                if (IsHitReactangle( form.Bounds, screen.Bounds ))
                {
                    var sd = new ScreenData();
                    sd.target_screen = screen;
                    return sd;
                }
            }
            return null;
        }

        static public bool IsHitReactangle( Rectangle a, Rectangle b )
        {
            if (
                  Math.Abs(a.X - b.X) < a.Width / 2 + b.Width / 2 //横の判定
                  &&
                  Math.Abs(a.Y - b.Y) < a.Height / 2 + b.Height / 2 //縦の判定
            )
            {
                return true;
            }
            return false;
        }

    }
}
