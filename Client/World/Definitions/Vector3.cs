using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.World.Definitions
{
    public class Vector3
    {
        public float X
        {
            get;
            set;
        }
        public float Y
        {
            get;
            set;
        }
        public float Z
        {
            get;
            set;
        }

        public Vector3(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public void Set(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Set(Vector3 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        public float Length
        {
            get
            {
                return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
            }
        }
    }
}
