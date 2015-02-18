using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.World.Entities
{
    public class Unit : WorldObject
    {
        public float Speed
        {
            get;
            private set;
        }

        public Unit()
        {
            Speed = 7.0f;
        }
    }
}
