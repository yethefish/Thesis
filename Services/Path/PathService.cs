// PathService.cs
using Microsoft.JSInterop;
using Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Text;

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

            if (drone.StartPoint == null || drone.EndPoint == null)
            {
                drone.Path.Points.Clear();
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

        public async Task<double> GetPathLength(Drone drone)
        {
            if (drone.Path == null || drone.Path.Points.Count < 2)
            {
                return 0;
            }
            return drone.Path.CalculateLength();
        }

        public async Task ClearDronePath(Drone drone)
        {
            await _jsRuntime.InvokeVoidAsync("clearDronePath", drone.Color);
        }

        public List<Models.Point> CalculateOptimalPath(Drone drone)
        {
            // Тот же бустрофедон, но с плавными переходами
            return GenerateBoustrophedonPath(drone);
        }


        private List<Models.Point> GenerateBoustrophedonPath(Drone drone)
        {
            var poly = drone.Polygon.Points;
            // отбрасываем последний дублирующий
            var boundary = poly.Take(poly.Count - 1).ToList();

            var segments = new List<(Models.Point Start, Models.Point End)>();
            double step = 4 * drone.Radius;
            double minLat = boundary.Min(p => p.Latitude);
            double maxLat = boundary.Max(p => p.Latitude);
            double stepLat = step / 111320.0; // градусы

            bool leftToRight = true;
            for (double lat = minLat + stepLat / 2; lat <= maxLat; lat += stepLat)
            {
                var ints = GetScanLineIntersections(boundary, lat)
                              .OrderBy(p => p.Longitude).ToList();
                if (ints.Count < 2) continue;
                for (int i = 0; i < ints.Count - 1; i += 2)
                {
                    var a = ints[i];
                    var b = ints[i + 1];
                    segments.Add(leftToRight
                        ? (a, b)
                        : (b, a));
                    leftToRight = !leftToRight;
                }
            }

            var path = new List<Models.Point>();
            // стартуем из StartPoint
            Models.Point current = drone.StartPoint;
            double currentBearing = double.NaN; // пока не задан
            path.Add(current);

            for (int i = 0; i < segments.Count; i++)
            {
                var (segStart, segEnd) = segments[i];
                // 1) Подлетаем к началу сегмента

                double desiredBearing = CalculateBearing(current, segStart);

                // если первый сегмент — currentBearing NaN, считаем его = desiredBearing
                if (i == 0) currentBearing = desiredBearing;

                // если угол > max, рисуем дугу
                if (Math.Abs(NormalizeAngle(desiredBearing - currentBearing)) > drone.MaxTurnAngle)
                {
                    var turnArc = GenerateArcFromTo(
                        current, segStart,
                        currentBearing, drone.Radius * 2.6);
                    path.AddRange(turnArc);
                }
                else
                {
                    // иначе можно сразу прыгнуть в segStart
                    path.Add(segStart);
                }

                // 2) Прямой проход
                path.Add(segEnd);

                // обновляем current и bearing на конец сегмента
                current = segEnd;
                currentBearing = CalculateBearing(segStart, segEnd);
            }

            // 3) Переход к конечной точке

            path.Add(drone.EndPoint);


            // всегда завершаем EndPoint
            if (!path.Last().Equals(drone.EndPoint))
                path.Add(drone.EndPoint);

            return path;
        }

        // Генерация дуги из from->to, учитывая начальный курс и радиус поворота.
        // Возвращает серию точек от прямой до to. Последняя точка == to.
        private List<Models.Point> GenerateArcFromTo(
            Models.Point from,
            Models.Point to,
            double initialBearing,
            double turnRadiusMeters,
            int segments = 20)
        {
            var arc = new List<Models.Point>();
            double targetBearing = CalculateBearing(from, to);
            // signed угол от начального до целевого
            double delta = NormalizeSigned(targetBearing - initialBearing);
            double direction = Math.Sign(delta);

            double absDelta = Math.Abs(delta);
            double angleStep = absDelta / segments;

            for (int i = 1; i <= segments; i++)
            {
                double bearing = initialBearing + direction * angleStep * i;
                // длина дуги на земле: L = R * θ (θ в рад)
                double dist = turnRadiusMeters * (angleStep * Math.PI / 180) * i;
                var pt = ComputeOffset(from, dist, bearing);
                arc.Add(pt);
            }
            // гарантируем точное попадание в to
            arc[arc.Count - 1] = to;
            return arc;
        }

        private Models.Point ComputeOffset(Models.Point start, double distMeters, double bearingDeg)
        {
            const double R = 6371000.0;
            double φ1 = ToRad(start.Latitude);
            double λ1 = ToRad(start.Longitude);
            double θ = ToRad(bearingDeg);

            double φ2 = Math.Asin(Math.Sin(φ1) * Math.Cos(distMeters / R) +
                                  Math.Cos(φ1) * Math.Sin(distMeters / R) * Math.Cos(θ));
            double λ2 = λ1 + Math.Atan2(Math.Sin(θ) * Math.Sin(distMeters / R) * Math.Cos(φ1),
                                       Math.Cos(distMeters / R) - Math.Sin(φ1) * Math.Sin(φ2));

            return new Models.Point(ToDeg(φ2), ToDeg(λ2));
        }

        private List<Models.Point> GetScanLineIntersections(
            List<Models.Point> poly, double scanLat)
        {
            var ints = new List<Models.Point>();
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % poly.Count];
                if ((a.Latitude <= scanLat && b.Latitude > scanLat) ||
                    (a.Latitude > scanLat && b.Latitude <= scanLat))
                {
                    double lon = a.Longitude +
                                 (scanLat - a.Latitude) *
                                 (b.Longitude - a.Longitude) /
                                 (b.Latitude - a.Latitude);
                    ints.Add(new Models.Point(scanLat, lon));
                }
            }
            return ints;
        }

        private double CalculateBearing(Models.Point a, Models.Point b)
        {
            double φ1 = ToRad(a.Latitude), φ2 = ToRad(b.Latitude);
            double Δλ = ToRad(b.Longitude - a.Longitude);

            double y = Math.Sin(Δλ) * Math.Cos(φ2);
            double x = Math.Cos(φ1) * Math.Sin(φ2) -
                       Math.Sin(φ1) * Math.Cos(φ2) * Math.Cos(Δλ);
            return NormalizeAngle(ToDeg(Math.Atan2(y, x)));
        }

        private double NormalizeAngle(double angle)
        {
            angle %= 360;
            return angle < 0 ? angle + 360 : angle;
        }

        private double NormalizeSigned(double angle)
        {
            angle %= 360;
            if (angle > 180) angle -= 360;
            if (angle < -180) angle += 360;
            return angle;
        }

        private double ToRad(double deg) => deg * Math.PI / 180.0;
        private double ToDeg(double rad) => rad * 180.0 / Math.PI;
        
        public async Task<string> GenerateMavlinkWaypointsCsv(Drone drone)
        {
            double altitude = 50;
            double acceptanceRadius = 10;
            if (drone.Path?.Points == null || drone.Path.Points.Count < 2)
                return string.Empty;

            var csv = new StringBuilder();
            csv.AppendLine("Seq,Frame,Command,Param1,Param2,Param3,Param4,X,Y,Z");

            csv.AppendLine($"0,3,22,0,0,0,0,{drone.StartPoint.Latitude},{drone.StartPoint.Longitude},{altitude}");

            int seq = 1;
            foreach (var point in drone.Path.Points)
            {
                csv.AppendLine($"{seq},3,16,{acceptanceRadius},0,0,0,{point.Latitude},{point.Longitude},{altitude}");
                seq++;
            }

            // Добавляем команду посадки (MAV_CMD_NAV_LAND)
            csv.AppendLine($"{seq},3,21,0,0,0,0,{drone.EndPoint.Latitude},{drone.EndPoint.Longitude},0");

            return csv.ToString();
        }
    }
}