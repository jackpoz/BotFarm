using MapCLI;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace DetourCLI
{
    public enum PathType
    {
        None,
        Complete,
        Partial
    };

    public class Detour : IDisposable
    {
        public Detour()
        {
        }

        public void Dispose()
        {
        }

        public PathType FindPath(float startX, float startY, float startZ, float endX, float endY, float endZ, int mapID, out List<Point> path)
        {
            path = new List<Point>();
            return PathType.None;
        }

        public static void Initialize(string mmapsPath)
        {
        }
    }
}

namespace VMapCLI
{
    public class VMap
    {
        //static const int DEFAULT_HEIGHT_SEARCH = 50.0f;
        //static const float SAFE_Z_HIGHER_BIAS = 2.0f;

        public static void Initialize(String vmapsPath)
        {
        }

        public static void LoadTile(int tileX, int tileY, int mapID)
        {
        }

        public static float GetHeight(float X, float Y, float Z, int mapID)
        {
            return float.NaN;
        }
    }
}

namespace MapCLI
{
    public class Point
    {
        public float X;
        public float Y;
        public float Z;

        public Point(float x, float y, float z)
        {
            throw new NotImplementedException();
        }

        public static Point operator+(Point a, Point b)
        {
            throw new NotImplementedException();
        }

        public static Point operator -(Point a, Point b)
        {
            throw new NotImplementedException();
        }

        public static Point operator *(Point a, float b)
        {
            throw new NotImplementedException();
        }

        public Point Direction
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public float DirectionOrientation
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public float Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    public class Map
    {
        public static float GetHeight(float X, float Y, float Z, int mapID)
        {
            return float.NaN;
        }

        public static void Initialize(string mmapsPath)
        {
        }
    }
}

namespace DBCStoresCLI
{
    public class AchievementExploreLocation
    {
        public AchievementExploreLocation(Point location, uint criteriaID)
        {
        }

        public uint CriteriaID;
        public Point Location;
    };

    public class DBCStores
    {
        public static void Initialize(String dbcsPath)
        {
        }

        public static void LoadDBCs()
        {
        }

        public static List<AchievementExploreLocation> GetAchievementExploreLocations(float x, float y, float z, int mapID)
        {
            return new List<AchievementExploreLocation>();
        }
    }
}