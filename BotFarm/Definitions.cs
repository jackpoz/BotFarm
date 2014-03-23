using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotFarm
{
    [Flags]
    public enum GroupType
    {
        GROUPTYPE_NORMAL = 0x00,
        GROUPTYPE_BG = 0x01,
        GROUPTYPE_RAID = 0x02,
        GROUPTYPE_BGRAID = GROUPTYPE_BG | GROUPTYPE_RAID,       // mask
        GROUPTYPE_UNK1 = 0x04,
        GROUPTYPE_LFG = 0x08
        // 0x10, leave/change group?, I saw this flag when leaving group and after leaving BG while in group
    };
}
