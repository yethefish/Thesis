using Microsoft.JSInterop;
using Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class PathService : IPathService
    {
        private readonly IJSRuntime _jsRuntime;

        public PathService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task CalculateAndDrawDronePath(Drone drone)
        {
            await _jsRuntime.InvokeVoidAsync("clearDronePath", drone.Color);
            
            if (drone.Polygon.Points.Count < 3 || !drone.Polygon.IsClosed() || 
                drone.StartPoint == null || drone.EndPoint == null)
            {
                return;
            }

            var pathPoints = CalculateOptimalPath(drone);
            drone.Path.Points = pathPoints;
            drone.Path.Color = drone.Color;

            if (pathPoints.Count > 1)
            {
                await _jsRuntime.InvokeVoidAsync("drawDronePath", 
                    pathPoints.Select(p => new[] { p.Latitude, p.Longitude }).ToList(), 
                    drone.Color);
            }
        }

        public async Task CalculateAndDrawAllDronePaths(List<Drone> drones)
        {
            foreach (var drone in drones)
            {
                await CalculateAndDrawDronePath(drone);
            }
        }

        public List<Models.Point> CalculateOptimalPath(Drone drone)
        {
            var path = new List<Models.Point>();
            
            path.Add(drone.StartPoint);

            if (drone.Polygon.IsClosed())
            {
                path.AddRange(CalculateBoustrophedonPath(drone));
            }

            path.Add(drone.EndPoint);

            return path;
        }

        private List<Models.Point> CalculateBoustrophedonPath(Drone drone)
        {
            var polygonPoints = drone.Polygon.Points.Take(drone.Polygon.Points.Count - 1).ToList();
            var path = new List<Models.Point>();

            if (polygonPoints.Count < 3) return path;

            // 1. Определяем ограничивающий прямоугольник полигона
            double minLat = polygonPoints.Min(p => p.Latitude);
            double maxLat = polygonPoints.Max(p => p.Latitude);
            double minLon = polygonPoints.Min(p => p.Longitude);
            double maxLon = polygonPoints.Max(p => p.Longitude);

            // 2. Рассчитываем шаг сканирования на основе радиуса дрона
            double coverageWidth = 4 * drone.Radius;
            double overlap = 0.1;
            double stepMeters = coverageWidth * (1 - overlap);
            double stepLat = stepMeters / 111320.0; // Конвертируем метры в градусы широты

            // 3. Генерируем линии сканирования с отступом от границ
            bool leftToRight = true;
            for (double lat = minLat + stepLat; lat <= maxLat - stepLat; lat += stepLat)
            {
                // Находим точки пересечения горизонтальной линии с полигоном
                var intersections = FindIntersections(polygonPoints, lat)
                    .OrderBy(p => p.Longitude)
                    .ToList();

                if (intersections.Count < 2) continue;

                // Пропускаем нечетное количество пересечений
                if (intersections.Count % 2 != 0) continue;

                // Создаем пары точек для линий сканирования
                for (int i = 0; i < intersections.Count; i += 2)
                {
                    var start = intersections[i];
                    var end = intersections[i + 1];

                    // Смещение точек внутрь полигона по долготе
                    double offsetMeters = stepMeters * 0.1; // 10% от шага
                    double offsetLon = offsetMeters / (111320.0 * Math.Cos(lat * Math.PI / 180));
                    var startShifted = new Models.Point(start.Latitude, start.Longitude + offsetLon);
                    var endShifted = new Models.Point(end.Latitude, end.Longitude - offsetLon);

                    if (leftToRight)
                    {
                        path.Add(startShifted);
                        path.Add(endShifted);
                    }
                    else
                    {
                        path.Add(endShifted);
                        path.Add(startShifted);
                    }
                }

                leftToRight = !leftToRight; // Меняем направление
            }

            return path;
        }

        private List<Models.Point> FindIntersections(List<Models.Point> polygonPoints, double lat)
        {
            var intersections = new List<Models.Point>();
            int n = polygonPoints.Count;
            for (int i = 0; i < n; i++)
            {
                Models.Point P1 = polygonPoints[i];
                Models.Point P2 = polygonPoints[(i + 1) % n];
                if ((P1.Latitude < lat && P2.Latitude >= lat) || (P1.Latitude >= lat && P2.Latitude < lat))
                {
                    double t = (lat - P1.Latitude) / (P2.Latitude - P1.Latitude);
                    double lon = P1.Longitude + t * (P2.Longitude - P1.Longitude);
                    intersections.Add(new Models.Point(lat, lon));
                }
            }
            return intersections;
        }

        public async Task ClearDronePath(Drone drone)
        {
            await _jsRuntime.InvokeVoidAsync("clearDronePath", drone.Color);
        }

        public async Task<double> GetPathLength(Drone drone)
        {
            if (drone.Path == null || drone.Path.Points.Count < 2)
            {
                return 0;
            }
            
            return drone.Path.CalculateLength();
        }
    }
}