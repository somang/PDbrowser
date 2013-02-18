using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebBrowser
{
    public class PointCollection : List<Point>
    {
        public Point Centroid { get; set; }
        
        public PointCollection()
            : base()
        {
            Centroid = new Point();
        }

        public void AddPoint(Point p)
        {
            this.Add(p);
            UpdateCentroid();
        }

        public Point RemovePoint(int index)
        {
            Point removedPoint = new Point(this[index].Id, this[index].X, this[index].Y);
            this.RemoveAt(index);
            UpdateCentroid();

            return (removedPoint);
        }

        public Point RemovePoint(Point p)
        {
            Point removedPoint = new Point(p.Id, p.X, p.Y);
            this.Remove(p);
            UpdateCentroid();

            return (removedPoint);
        }

        public Point GetPointNearestToCentroid()
        {
            double minimumDistance = 0.0;
            int nearestPointIndex = -1;

            foreach (Point p in this)
            {
                double distance = Point.FindDistance(p, Centroid);

                if (this.IndexOf(p) == 0)
                {
                    minimumDistance = distance;
                    nearestPointIndex = this.IndexOf(p);
                }
                else
                {
                    if (minimumDistance > distance)
                    {
                        minimumDistance = distance;
                        nearestPointIndex = this.IndexOf(p);
                    }
                }
            }

            return (this[nearestPointIndex]);
        }
        // Calculate Centroid dynamically.
        public void UpdateCentroid()
        {
            double xSum = (from p in this select p.X).Sum();
            double ySum = (from p in this select p.Y).Sum();
            Centroid.X = (xSum / (int)this.Count);
            Centroid.Y = (ySum / (int)this.Count);
        }
    }
}
