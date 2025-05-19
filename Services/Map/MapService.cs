using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Models;

namespace Services
{
    public class MapService : IMapService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly IPathService _pathService;

        public MapService(IJSRuntime jsRuntime, IPathService pathService)
        {
            _jsRuntime = jsRuntime;
            _pathService = pathService;
        }

        public async Task InitializeMap()
        {
            await _jsRuntime.InvokeVoidAsync("initializeMap");
        }

        public async Task ToggleEditMode(bool isEditMode)
        {
            await _jsRuntime.InvokeVoidAsync("toggleEditMode", isEditMode);
        }

        public async Task ToggleDeleteMode(bool isDeleteMode)
        {
            await _jsRuntime.InvokeVoidAsync("toggleDeleteMode", isDeleteMode);
        }

        public async Task OpenEditMenu(bool isOpen)
        {
            await _jsRuntime.InvokeVoidAsync("toggleEditMenu", isOpen);
        }

        public async Task ToggleStartFinishMode(bool isStartFinishMode)
        {
            await _jsRuntime.InvokeVoidAsync("toggleStartFinishMode", isStartFinishMode);
        }

        public async Task RedrawPolygon(Drone drone)
        {
            // Очищаем все элементы полигона (точки, линии и сам полигон)
            await _jsRuntime.InvokeVoidAsync("clearPolygon", drone.Polygon);

            var coordinates = drone.Polygon.Points.Select(p => new double[] { p.Latitude, p.Longitude }).ToList();

            // Рисуем точки и линии
            for (int i = 0; i < drone.Polygon.Points.Count; i++)
            {
                var point = drone.Polygon.Points[i];
                await _jsRuntime.InvokeVoidAsync("drawPoint", new[] { point.Latitude, point.Longitude });

                int nextIndex = (i + 1) % drone.Polygon.Points.Count;
                if (nextIndex < drone.Polygon.Points.Count)
                {
                    var nextPoint = drone.Polygon.Points[nextIndex];
                    await _jsRuntime.InvokeVoidAsync("drawLine",
                        new[] { point.Latitude, point.Longitude },
                        new[] { nextPoint.Latitude, nextPoint.Longitude });
                }
            }

            // Рисуем полигон, если он замкнут
            if (drone.Polygon.IsClosed())
            {
                await _jsRuntime.InvokeVoidAsync("drawPolygon", coordinates, drone.Color);
            }
        }

        public async Task RedrawAllPolygons(List<Drone> drones)
        {
            await _jsRuntime.InvokeVoidAsync("clearMap");

            foreach (var drone in drones)
            {
                var coordinates = drone.Polygon.Points.Select(p => new double[] { p.Latitude, p.Longitude }).ToList();

                // Рисуем полигон и точки
                for (int i = 0; i < drone.Polygon.Points.Count; i++)
                {
                    var point = drone.Polygon.Points[i];
                    await _jsRuntime.InvokeVoidAsync("drawPoint", new[] { point.Latitude, point.Longitude });

                    int nextIndex = (i + 1) % drone.Polygon.Points.Count;
                    if (nextIndex < drone.Polygon.Points.Count)
                    {
                        var nextPoint = drone.Polygon.Points[nextIndex];
                        await _jsRuntime.InvokeVoidAsync("drawLine",
                            new[] { point.Latitude, point.Longitude },
                            new[] { nextPoint.Latitude, nextPoint.Longitude });
                    }
                }

                if (drone.Polygon.IsClosed())
                {
                    await _jsRuntime.InvokeVoidAsync("drawPolygon", coordinates, drone.Color);
                }

                // Рисуем точки старта и финиша для каждого дрона
                if (drone.StartPoint != null || drone.EndPoint != null)
                {
                    await _jsRuntime.InvokeVoidAsync("drawStartFinishPoints", 
                        drone.StartPoint != null ? new[] { drone.StartPoint.Latitude, drone.StartPoint.Longitude } : null,
                        drone.EndPoint != null ? new[] { drone.EndPoint.Latitude, drone.EndPoint.Longitude } : null,
                        drone.Color);
                }

                // Всегда рисуем путь для дрона, если есть старт и финиш
                if (drone.StartPoint != null && drone.EndPoint != null && drone.Polygon.IsClosed())
                {
                    await _pathService.CalculateAndDrawDronePath(drone);
                }
            }
        }

        public async Task DrawStartFinishPoints(Drone drone)
        {
            await _jsRuntime.InvokeVoidAsync("drawStartFinishPoints", 
                drone.StartPoint != null ? new[] { drone.StartPoint.Latitude, drone.StartPoint.Longitude } : null,
                drone.EndPoint != null ? new[] { drone.EndPoint.Latitude, drone.EndPoint.Longitude } : null,
                drone.Color);
        }

        public async Task ClearDroneStartFinishPoints(string droneColor)
        {
            await _jsRuntime.InvokeVoidAsync("clearDroneStartFinishPoints", droneColor);
        }

        public async Task ClearPolygon(Polygon polygon)
        {
            await _jsRuntime.InvokeVoidAsync("clearPolygon", polygon);
        }

        public async Task ToggleLock(bool isLocked)
        {
            await _jsRuntime.InvokeVoidAsync("toggleLockAnimation", isLocked);
        }

        public async Task SearchAddress(string address)
        {
            await _jsRuntime.InvokeVoidAsync("searchAddress", address);
        }

        public async Task<string> GetAddressInputValue()
        {
            return await _jsRuntime.InvokeAsync<string>("getAddressInputValue");
        }

    }
}