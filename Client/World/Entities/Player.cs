using Client.World.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.World.Entities
{
    public class Player : Unit
    {
        public bool IsGhost
        {
            get
            {
                return HasFlag(PlayerFlags.PLAYER_FLAGS_GHOST);
            }
        }

        public bool IsAlive
        {
            get
            {
                return this[UnitField.UNIT_FIELD_HEALTH] > 0 && !IsGhost;
            }
        }

        public bool HasFlag(PlayerFlags flag)
        {
            return (this[PlayerField.PLAYER_FLAGS] & (uint)flag) != 0;
        }

        public Position CorpsePosition
        {
            get;
            set;
        }
    }
}
