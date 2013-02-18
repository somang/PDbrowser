using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebBrowser
{
    public class Clustering
    {
        public static List<PointCollection> doClustering(PointCollection points, int clusterCount)
        {
            //divide points into equal clusters
            List<PointCollection> clusterCollection = new List<PointCollection>();
            List<List<Point>> allGroups = ListUtility.SplitList<Point>(points, clusterCount);
            foreach (List<Point> group in allGroups)
            {
                PointCollection cluster = new PointCollection();
                cluster.AddRange(group);
                clusterCollection.Add(cluster);
            }

            Console.WriteLine("Start clustering");
            //start clustering
            int increament = 1;
            while (increament > 0)
            {
                increament = 0;

                foreach (PointCollection cluster in clusterCollection) //for all clusters
                {
                    for (int pointIndex = 0; pointIndex < cluster.Count; pointIndex++) //for all points in each cluster
                    {
                        Point point = cluster[pointIndex];

                        int nearestCluster = FindNearestCluster(clusterCollection, point);
                        if (nearestCluster != clusterCollection.IndexOf(cluster)) //if point has moved
                        {
                            if (cluster.Count > 1) //cluster shall have minimum one point
                            {
                                Point removedPoint = cluster.RemovePoint(point);
                                clusterCollection[nearestCluster].AddPoint(removedPoint);
                                increament += 1;
                            }
                        }
                    }
                }
            }

            return (clusterCollection);
        }

        public static int FindNearestCluster(List<PointCollection> clusterCollection, Point point)
        {
            double min_d = 0.0;
            int nearestCluster = -1;

            for (int k = 0; k < clusterCollection.Count; k++) //find nearest cluster
            {
                double d = Point.FindDistance(point, clusterCollection[k].Centroid);
                if (k == 0)
                {
                    min_d = d;
                    nearestCluster = 0;
                }
                else if (min_d > d)
                {
                    min_d = d;
                    nearestCluster = k;
                }
            }

            return (nearestCluster);
        }
    }
}
