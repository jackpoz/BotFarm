using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.World.Entities
{
    public class Position
    {
        public const int INVALID_MAP_ID = -1;

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
        public float O
        {
            get;
            set;
        }
        public int MapID
        {
            get;
            set;
        }

        public bool IsValid
        {
            get
            {
                return X != 0.0f && Y != 0.0f && Z != 0.0f && MapID != INVALID_MAP_ID;
            }
        }

        public Position(float x, float y, float z, float o, int mapID)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.O = o;
            this.MapID = mapID;
        }

        public Position() : this(0.0f, 0.0f, 0.0f, 0.0f, INVALID_MAP_ID)
        { }

        public Position GetPosition()
        {
            return new Position(X, Y, Z, O, MapID);
        }

        public void SetPosition(Position pos)
        {
            X = pos.X;
            Y = pos.Y;
            Z = pos.Z;
            O = pos.O;
        }

        public void SetPosition(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void ResetPosition()
        {
            X = 0.0f;
            Y = 0.0f;
            Z = 0.0f;
            O = 0.0f;
            MapID = INVALID_MAP_ID;
        }

        public static Position operator +(Position a, Position b)
        {
            if (a.MapID != INVALID_MAP_ID && b.MapID != INVALID_MAP_ID && a.MapID != b.MapID)
                return new Position();

            return new Position(a.X + b.X, a.Y + b.Y, a.Z + b.Z, b.O, a.MapID);
        }

        public static Position operator -(Position a, Position b)
        {
            if (a.MapID != INVALID_MAP_ID && b.MapID != INVALID_MAP_ID && a.MapID != b.MapID)
                return new Position();

            var result = new Position(a.X - b.X, a.Y - b.Y, a.Z - b.Z, 0.0f, a.MapID);
            result.O = result.CalculateOrientation();

            return result;
        }

        public static Position operator *(Position a, double scale)
        {
            return a * (float)scale;
        }

        public static Position operator *(Position a, float scale)
        {
            return new Position(a.X * scale, a.Y * scale, a.Z * scale, a.O, a.MapID);
        }

        public float Length
        {
            get
            {
                return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
            }
        }

        public Position Direction
        {
            get
            {
                var length = Length;
                return new Position(X / length, Y / length, Z / length, O, MapID);
            }
        }

        private float CalculateOrientation()
        {
            double orientation;
            if (X == 0)
            {
                if (Y > 0)
                    orientation = Math.PI / 2;
                else
                    orientation = 3 * Math.PI / 2;
            }
            else if (Y == 0)
            {
                if (X > 0)
                    orientation = 0;
                else
                    orientation = Math.PI;
            }
            else
            {
                orientation = Math.Atan2(Y, X);
                if (orientation < 0)
                    orientation += 2 * Math.PI;
            }

            return (float)orientation;
        }
    }
}
