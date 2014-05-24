using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.World.Entities
{
    public class WorldObject : Position
    {
        public UInt64 GUID
        {
            get;
            set;
        }
    }
}
