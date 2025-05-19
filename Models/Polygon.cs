using System;
using System.Collections.Generic;
using System.Linq;

namespace Models
{
    public class Polygon
    {

        public List<Point> Points { get; set; } = new List<Point>();

        public void AddPoint(double latitude, double longitude)
        {
            Points.Add(new Point(latitude, longitude));
            if (Points.Count > 2)
            {
                ClosePolygon();
            }
        }

        public void InsertPoint(int index, double latitude, double longitude)
        {
            if (index >= 0 && index < Points.Count - 1) 
            {
                Points.Insert(index + 1, new Point(latitude, longitude));
            }
            else if (index == Points.Count - 1 && IsClosed()) 
            {
                Points.Insert(index, new Point(latitude, longitude));
                Points[^1] = Points[0]; 
            }
        }


        public void ClosePolygon()
        {
            if (Points.Count > 2 && !IsClosed())
            {
                Points.Add(Points[0]);
            }
        }

        public bool IsClosed()
        {
            return Points.Count > 2 && Points[0].Latitude == Points[^1].Latitude && Points[0].Longitude == Points[^1].Longitude;
        }

        public (int, double) FindClosestEdge(double lat, double lon)
        {
            if (Points.Count < 2) return (-1, double.MaxValue); 

            int closestIndex = -1;
            double minDistance = double.MaxValue;
            
            for (int i = 0; i < Points.Count - 1; i++)
            {
                double distance = GetDistanceToSegment(Points[i], Points[i + 1], new Point(lat, lon));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }

            if (IsClosed())
            {
                double distance = GetDistanceToSegment(Points[^1], Points[0], new Point(lat, lon));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = Points.Count - 1;
                }
            }

            return (closestIndex, minDistance);
        }

        public bool RemovePoint(double latitude, double longitude, double tolerance = 0.001)
        {
            var pointToRemove = Points
                .FirstOrDefault(p => Math.Abs(p.Latitude - latitude) < tolerance && 
                                    Math.Abs(p.Longitude - longitude) < tolerance);

            if (pointToRemove != null)
            {
                Points.Remove(pointToRemove);

                // Если полигон был замкнут и удалена последняя точка (дубликат первой),
                // нужно обновить первую точку
                if (IsClosed() && Points.Count > 0)
                {
                    Points[^1] = Points[0]; // Обновляем последнюю точку
                }

                return true;
            }

            return false;
        }

        public static double GetDistance(Point a, Point b)
        {
            const double R = 6371000;
            double lat1 = a.Latitude * Math.PI / 180;
            double lat2 = b.Latitude * Math.PI / 180;
            double deltaLat = (b.Latitude - a.Latitude) * Math.PI / 180;
            double deltaLon = (b.Longitude - a.Longitude) * Math.PI / 180;

            double h = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
            return R * c;
        }

        private static double GetDistanceToSegment(Point a, Point b, Point p)
        {
            double A = p.Latitude - a.Latitude;
            double B = p.Longitude - a.Longitude;
            double C = b.Latitude - a.Latitude;
            double D = b.Longitude - a.Longitude;
            double dot = A * C + B * D;
            double len_sq = C * C + D * D;
            double param = len_sq != 0 ? dot / len_sq : -1;
            if (param < 0) return GetDistance(p, a);
            if (param > 1) return GetDistance(p, b);
            return GetDistance(p, new Point(a.Latitude + param * C, a.Longitude + param * D));
        }

        public double CalculateArea()
        {
            if (Points.Count < 3) return 0;

            double area = 0;
            int n = Points.Count;
            
            // Если полигон замкнут, учитываем это
            int limit = IsClosed() ? n - 1 : n;

            for (int i = 0; i < limit; i++)
            {
                int j = (i + 1) % n;
                area += Points[i].Longitude * Points[j].Latitude;
                area -= Points[i].Latitude * Points[j].Longitude;
            }

            area = Math.Abs(area / 2);
            
            // Конвертируем площадь из градусов в квадратные метры (примерная конверсия)
            const double metersPerDegree = 111320; // примерное значение для широты
            return area * Math.Pow(metersPerDegree, 2);
        }
    }
}
