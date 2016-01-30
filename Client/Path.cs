using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DetourCLI;
using MapCLI;

namespace Client
{
    public class Path
    {
        Point[] points;
        int NextPointIndex;

        public Point CurrentPosition
        {
            get
            {
                return _currentPosition;
            }
            private set
            {
                _currentPosition = value;
            }
        }
        Point _currentPosition;

        public float CurrentOrientation
        {
            get
            {
                if (NextPointIndex < points.Length)
                    return (points[NextPointIndex] - CurrentPosition).DirectionOrientation;
                else
                    return (points[NextPointIndex - 1] - points[NextPointIndex - 2]).DirectionOrientation;
            }
        }

        public float Speed
        {
            get;
            set;
        }

        public int MapID
        {
            get;
            private set;
        }

        public Point Destination
        {
            get
            {
                return points.Last();
            }
        }

        Point previousPosition;
        int closePositionCounter;
        static readonly int MaxClosePositionCounter = 4;

        public Path(List<Point> points, float speed, int mapID)
        {
            if (points == null || points.Count < 2)
                throw new ArgumentException("Argument cannot be null or a list with just 1 point", "points");
            this.points = points.ToArray();

            if (speed <= 0.0f)
                throw new ArgumentException("Argument must be a positive number", "speed");
            this.Speed = speed;

            this.CurrentPosition = this.points[0];
            this.NextPointIndex = 1;
            this.MapID = mapID;
            this.previousPosition = CurrentPosition;
            this.closePositionCounter = 0;
        }

        public Point MoveAlongPath(float deltaTime)
        {
            float totalDistance = deltaTime * Speed;
            float distanceToNextPoint = (points[NextPointIndex] - _currentPosition).Length;
            if(totalDistance < distanceToNextPoint)
            {
                Point result = _currentPosition + (points[NextPointIndex] - _currentPosition).Direction * totalDistance;
                _currentPosition = result;
                _currentPosition.Z = MapCLI.Map.GetHeight(_currentPosition.X, _currentPosition.Y, _currentPosition.Z, MapID);
            }
            else
            {
                NextPoint(totalDistance, distanceToNextPoint);
            }

            if ((_currentPosition - previousPosition).Length < 1f)
            {
                closePositionCounter++;
                if (closePositionCounter >= MaxClosePositionCounter)
                {
                    closePositionCounter = 0;
                    NextPoint(totalDistance, distanceToNextPoint);
                }
            }
            else
            {
                previousPosition = _currentPosition;
                closePositionCounter = 0;
            }

            return _currentPosition;
        }

        private void NextPoint(float totalDistance, float distanceToNextPoint)
        {
            NextPointIndex++;
            if (NextPointIndex >= points.Length)
                _currentPosition = points.Last();
            else
            {
                float remainingTime = (totalDistance - distanceToNextPoint) / Speed;
                _currentPosition = MoveAlongPath(remainingTime);
            }
            _currentPosition.Z = MapCLI.Map.GetHeight(_currentPosition.X, _currentPosition.Y, _currentPosition.Z, MapID);
        }

        public float TimeBeforeNextPoint()
        {
            throw new NotImplementedException();
        }
    }
}
