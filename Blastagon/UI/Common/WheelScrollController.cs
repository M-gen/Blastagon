using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blastagon.UI.Common
{
    public class WheelScrollController
    {
        Int64 power = 0;
        Int64 power_max = 0;
        double scale = 1.0;

        Action<Int64> update;

        public WheelScrollController(Action<Int64> update, int power_max, double scale)
        {
            this.update = update;
            this.power_max = power_max;
            this.scale = scale;
        }

        public void Stop()
        {
            power = 0;
        }

        public bool IsScroll()
        {
            return power != 0;
        }

        // 数値を更新するためのUpdate
        public void UpdateValue()
        {
            update(power);
        }

        // マウスホイールの更新への対応
        public void UpdateWheel( int wheel_value )
        {
            power += (Int64)(wheel_value * scale);

            if (power < -power_max) power = -power_max;
            if (power > power_max)  power = power_max;
        }

    }
}
