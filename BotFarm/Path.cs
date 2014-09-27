using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DetourCLI;

namespace BotFarm
{
    class Path
    {
        Point[] points;
        int NextPointIndex;

        public Point CurrentPosition
        {
            get;
            private set;
        }

        public float Speed
        {
            get;
            set;
        }

        public Path(List<Point> points, float speed)
        {
            if (points == null || points.Count < 2)
                throw new ArgumentException("Argument cannot be null or a list with just 1 point", "points");
            this.points = points.ToArray();

            if (speed <= 0.0f)
                throw new ArgumentException("Argument must be a positive number", "speed");
            this.Speed = speed;

            CurrentPosition = this.points[0];
            NextPointIndex = 1;
        }

        public Point MoveAlongPath(float deltaTime)
        {
            float totalDistance = deltaTime * Speed;
            float distanceToNextPoint = (points[NextPointIndex] - CurrentPosition).Length;
            if(totalDistance < distanceToNextPoint)
            {
                Point result = CurrentPosition + (points[NextPointIndex] - CurrentPosition).Direction * totalDistance;
                CurrentPosition = result;
            }
            else
            {
                NextPointIndex++;
                if (NextPointIndex == points.Length)
                    CurrentPosition = points.Last();
                else
                {
                    float remainingTime = (totalDistance - distanceToNextPoint) / Speed;
                    CurrentPosition = MoveAlongPath(remainingTime);
                }
            }

            return CurrentPosition;
        }

        public float TimeBeforeNextPoint()
        {
            throw new NotImplementedException();
        }
    }
}
