using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Models;
using Services;
using System.Linq;

namespace Pages
{
    public partial class Map : ComponentBase
    {
        private List<Drone> drones = new();
        private bool _isEditMode = false;
        private bool _isDeleteMode = false;
        private bool _isStartFinishMode = false;
        private bool _isEditMenuOpen = false;
        private bool _isLocked = false;


        private bool showAddDroneModal = false;
        private string newDroneName = "";
        private string newDroneDescription = "";
        private string newDroneColor = "#000000";
        private double newDroneRadius;
        private double newDroneFlightRange;
        private bool showDroneModal = false;
        private bool isEditingDrone = false;
        private Drone currentDrone = new Drone();
        private bool isKmFlight = false;
        private string flightRangeError = "";
        private string radiusError = "";
        private string nameError = "";

        private string altitudeError = "";


        [Inject]
        private IMapService MapService { get; set; }

        [Inject]
        private IPathService PathService { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        private DotNetObjectReference<Map> _dotNetRef;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _dotNetRef = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("setDotNetReference", _dotNetRef);
                await MapService.InitializeMap();
            }
        }
        
        [JSInvokable]
        private async Task OpenEditMenu()
        {
            _isEditMenuOpen = !_isEditMenuOpen;
            await MapService.OpenEditMenu(_isEditMenuOpen);
        }

        [JSInvokable]
        private async Task ToggleEditMode()
        {
            _isEditMode = !_isEditMode;
            _isDeleteMode = false;
            _isStartFinishMode = false;
            await MapService.ToggleEditMode(_isEditMode);
            await MapService.ToggleDeleteMode(_isDeleteMode);
            await MapService.ToggleStartFinishMode(_isStartFinishMode);
        }

        [JSInvokable]
        private async Task ToggleDeleteMode()
        {
            _isDeleteMode = !_isDeleteMode;
            _isEditMode = false;
            _isStartFinishMode = false;
            await MapService.ToggleEditMode(_isEditMode);
            await MapService.ToggleDeleteMode(_isDeleteMode);
            await MapService.ToggleStartFinishMode(_isStartFinishMode);
        }

        private async Task ToggleStartFinishMode()
        {
            _isStartFinishMode = !_isStartFinishMode;
            _isEditMode = false;
            _isDeleteMode = false;
            _isEditMenuOpen = false;
            await MapService.ToggleEditMode(_isEditMode);
            await MapService.ToggleDeleteMode(_isDeleteMode);
            await MapService.ToggleStartFinishMode(_isStartFinishMode);
            await MapService.OpenEditMenu(_isEditMenuOpen);
        }

        [JSInvokable]
        public async Task AddPoint(double[] coordinate)
        {
            if (coordinate.Length != 2) return;
            double latitude = coordinate[0];
            double longitude = coordinate[1];

            var selectedDrone = drones.FirstOrDefault(d => d.IsSelected);
            if (selectedDrone == null) return;

            if (selectedDrone.Polygon.IsClosed())
            {
                var (index, distance) = selectedDrone.Polygon.FindClosestEdge(latitude, longitude);
                if (index >= 0)
                {
                    selectedDrone.Polygon.InsertPoint(index, latitude, longitude);
                }
            }
            else
            {
                selectedDrone.Polygon.AddPoint(latitude, longitude);
            }

            await MapService.RedrawAllPolygons(drones);
            await PathService.CalculateAndDrawAllDronePaths(drones);
        }

        [JSInvokable]
        public async Task UpdatePoint(double[] coordinate)
        {
            if (coordinate.Length != 2) return;

            double latitude = coordinate[0];
            double longitude = coordinate[1];

            var selectedDrone = drones.FirstOrDefault(d => d.IsSelected);
            if (selectedDrone == null) return;

            var closestPoint = selectedDrone.Polygon.Points
                .OrderBy(p => Polygon.GetDistance(p, new Models.Point(latitude, longitude)))
                .FirstOrDefault();

            if (closestPoint != null)
            {
                closestPoint.Latitude = latitude;
                closestPoint.Longitude = longitude;
            }
            await PathService.CalculateAndDrawDronePath(selectedDrone);
        }

        [JSInvokable]
        public async Task RemovePoint(double[] coordinate)
        {
            if (!_isDeleteMode || coordinate.Length != 2) return;

            double latitude = coordinate[0];
            double longitude = coordinate[1];

            var selectedDrone = drones.FirstOrDefault(d => d.IsSelected);
            if (selectedDrone == null || selectedDrone.Polygon.Points.Count == 0) return;

            // Увеличиваем точность поиска точки
            bool isRemoved = selectedDrone.Polygon.RemovePoint(latitude, longitude, 0.0001); // Более точный допуск

            if (isRemoved)
            {
                // Если осталось меньше 3 точек, очищаем полигон
                if (selectedDrone.Polygon.Points.Count < 3)
                {
                    selectedDrone.Polygon.Points.Clear();
                }
                else if (selectedDrone.Polygon.IsClosed())
                {
                    // Обновляем последнюю точку (копию первой) если полигон был замкнут
                    selectedDrone.Polygon.Points[^1] = selectedDrone.Polygon.Points[0];
                }

                await MapService.RedrawAllPolygons(drones);
                await PathService.CalculateAndDrawAllDronePaths(drones);
            }
        }

        [JSInvokable]
        public async Task<bool> IsPointBelongsToSelectedDrone(double[] coordinate)
        {
            if (coordinate.Length != 2) return false;

            double latitude = coordinate[0];
            double longitude = coordinate[1];

            var selectedDrone = drones.FirstOrDefault(d => d.IsSelected);
            if (selectedDrone == null) return false;

            // Проверяем, принадлежит ли точка выбранному дрону
            return selectedDrone.Polygon.Points.Any(p => 
                Math.Abs(p.Latitude - latitude) < 0.0001 && 
                Math.Abs(p.Longitude - longitude) < 0.0001);
        }

        [JSInvokable]
        public async Task RedrawAllPolygons()
        {
            await MapService.RedrawAllPolygons(drones);
        }

        private async Task ToggleLock()
        {
            _isLocked = !_isLocked;
            await MapService.ToggleLock(_isLocked);
        }

        private async Task SearchAddress()
        {
            var address = await MapService.GetAddressInputValue();
            await MapService.SearchAddress(address);
        }

        private async Task SavePath()
        {
            var selectedDrone = drones.FirstOrDefault(d => d.IsSelected);
            if (selectedDrone == null || selectedDrone.Path?.Points == null) return;

            var csvContent = await PathService.GenerateMavlinkWaypointsCsv(selectedDrone);
            
            var fileName = $"{selectedDrone.Name}_mission.csv";
            var fileContent = System.Text.Encoding.UTF8.GetBytes(csvContent);
            
            await JSRuntime.InvokeVoidAsync("downloadFile", 
                fileName, 
                Convert.ToBase64String(fileContent));
        }

        private async Task AddDrone()
        {
            var drone = new Drone();
            drones.Add(drone);
            await MapService.RedrawAllPolygons(drones);
        }

        private async Task SelectDroneItem(Drone selectedDrone)
        {
            foreach (var drone in drones)
            {
                drone.IsSelected = false;
            }

            selectedDrone.IsSelected = true;
            await MapService.RedrawAllPolygons(drones);
        }

        private async Task OnColorChanged(Drone drone, ChangeEventArgs e)
        {
            await ChangeDroneColor(drone, e.Value.ToString());
        }

        private async Task ChangeDroneColor(Drone drone, string newColor)
        {
            drone.Color = newColor;
            await MapService.RedrawAllPolygons(drones);
            await PathService.CalculateAndDrawAllDronePaths(drones);
            StateHasChanged(); // Добавлено
        }

        private async Task DroneRemove(Drone drone)
        {
            drones.Remove(drone);
            await MapService.RedrawAllPolygons(drones);
        }

        public void Dispose()
        {
            _dotNetRef?.Dispose();
        }

        private async Task OnRadiusChange(Drone drone, ChangeEventArgs e)
        {
            if (double.TryParse(e.Value?.ToString(), out var value))
            {
                drone.Radius = value;
                await PathService.CalculateAndDrawAllDronePaths(drones);
                StateHasChanged(); // Добавлено обновление состояния
            }
        }

        private void OnFlightRangeChange(ChangeEventArgs e)
        {
            if (double.TryParse(e.Value?.ToString(), out var value))
            {
                newDroneFlightRange = value;
            }
        }

        private async Task ShowAddDroneModal()
        {
            flightRangeError = "";
            radiusError = "";
            nameError = "";
            altitudeError = "";
            currentDrone = new Drone();
            
            isEditingDrone = false;
            showDroneModal = true;
            StateHasChanged();
        }

        private string GetRandomColor()
        {
            var random = new Random();
            return String.Format("#{0:X6}", random.Next(0x1000000));
        }

        private async Task AddNewDrone()
        {
            var drone = new Drone
            {
                Name = string.IsNullOrWhiteSpace(newDroneName) ? $"Дрон" : newDroneName,
                Description = newDroneDescription,
                Color = newDroneColor,
                Radius = newDroneRadius,
                FlightRange = newDroneFlightRange
            };
            
            drones.Add(drone);
            showAddDroneModal = false;
            await MapService.RedrawAllPolygons(drones);
        }

        private async Task EditDrone(Drone drone)
        {
            flightRangeError = "";
            radiusError = "";
            nameError = "";
            altitudeError = "";

            if (!drone.IsSelected)
            {
                await SelectDroneItem(drone);
            }
            
            currentDrone = new Drone
            {
                Id = drone.Id,
                Name = drone.Name,
                Description = drone.Description,
                Color = drone.Color,
                Radius = drone.Radius,
                FlightRange = drone.FlightRange,
                IsSelected = drone.IsSelected,
                Polygon = drone.Polygon,
                StartPoint = drone.StartPoint,
                EndPoint = drone.EndPoint
            };
            
            isEditingDrone = true;
            showDroneModal = true;
            StateHasChanged();
        }

        private async Task CloseDroneModal()
        {
            showDroneModal = false;
            StateHasChanged();
        }

        private async Task SaveDrone()
        {

            if (!string.IsNullOrEmpty(nameError) || 
                !string.IsNullOrEmpty(flightRangeError) || 
                !string.IsNullOrEmpty(radiusError) ||
                !string.IsNullOrEmpty(altitudeError))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(currentDrone.Name))
            {
                nameError = "У БПЛА должно быть название.";
                StateHasChanged();
                return;
            }

            if (isEditingDrone)
            {
                var existingDrone = drones.FirstOrDefault(d => d.Id == currentDrone.Id);
                if (existingDrone != null)
                {
                    existingDrone.Name = currentDrone.Name;
                    existingDrone.Description = currentDrone.Description;
                    existingDrone.Color = currentDrone.Color;
                    existingDrone.Radius = currentDrone.Radius;
                    existingDrone.FlightRange = currentDrone.FlightRange;
                    existingDrone.Altitude = currentDrone.Altitude;
                }
            }
            else
            {
                var newDrone = new Drone
                {
                    Name = currentDrone.Name,
                    Description = currentDrone.Description,
                    Color = currentDrone.Color,
                    Radius = currentDrone.Radius,
                    FlightRange = currentDrone.FlightRange
                };
                
                if (currentDrone.Id > 0)
                {
                    newDrone.Id = currentDrone.Id;
                }
                
                drones.Add(newDrone);
            }
            
            showDroneModal = false;
            await MapService.RedrawAllPolygons(drones);
            await PathService.CalculateAndDrawAllDronePaths(drones);
            StateHasChanged();
        }

        private async Task CalculatePathForSelectedDrone()
        {
            var selectedDrone = drones.FirstOrDefault(d => d.IsSelected);
            if (selectedDrone != null)
            {
                await PathService.CalculateAndDrawDronePath(selectedDrone);
            }
        }

        [JSInvokable]
        public async Task SetStartPoint(double[] coordinate)
        {
            if (coordinate.Length != 2) return;
            
            var selectedDrone = drones.FirstOrDefault(d => d.IsSelected);
            if (selectedDrone == null) return;
            
            await MapService.ClearDroneStartFinishPoints(selectedDrone.Color);
            await PathService.ClearDronePath(selectedDrone);
            
            selectedDrone.StartPoint = new Models.Point(coordinate[0], coordinate[1]);
            await MapService.DrawStartFinishPoints(selectedDrone);
            
            await PathService.CalculateAndDrawDronePath(selectedDrone);
            StateHasChanged();
        }

        [JSInvokable]
        public async Task SetEndPoint(double[] coordinate)
        {
            if (coordinate.Length != 2) return;
            
            var selectedDrone = drones.FirstOrDefault(d => d.IsSelected);
            if (selectedDrone == null) return;
            
            await MapService.ClearDroneStartFinishPoints(selectedDrone.Color);
            await PathService.ClearDronePath(selectedDrone);
            
            selectedDrone.EndPoint = new Models.Point(coordinate[0], coordinate[1]);
            await MapService.DrawStartFinishPoints(selectedDrone);
            
            await PathService.CalculateAndDrawDronePath(selectedDrone);
            StateHasChanged();
        }

        private string GetPathLengthClass(Drone drone)
        {
            if (drone.Path == null || drone.Path.Points.Count < 2)
                return "";
                
            double pathLength = drone.Path.CalculateLength();
            
            if (pathLength > drone.FlightRange)
                return "danger";
                
            if (pathLength > (drone.FlightRange - 200))
                return "warning";
                
            return "";
        }

        private void HandleFlightRangeInput(ChangeEventArgs e, bool isKm)
        {
            flightRangeError = "";
            if (double.TryParse(e.Value?.ToString(), out var inputValue))
            {
                var min = isKm ? 0.1 : 1;
                
                if (inputValue < min)
                {
                    flightRangeError = $"Значение должно быть больше {min}";
                    return;
                }
                
                currentDrone.FlightRange = isKm 
                    ? (long)(inputValue * 1000)
                    : (long)inputValue;
            }
            else
            {
                flightRangeError = "Некорректное значение";
            }
            StateHasChanged();
        }

        private void HandleRadiusInput(ChangeEventArgs e)
        {
            radiusError = "";
            if (double.TryParse(e.Value?.ToString(), out var inputValue))
            {
                if (inputValue < 1)
                {
                    radiusError = "Значение должно быть больше 1";
                    StateHasChanged();
                    return;
                }
                
                currentDrone.Radius = (long)inputValue;
            }
            else
            {
                radiusError = "Некорректное значение";
            }
            StateHasChanged();
        }
        
        private void HandleАltitudeInput(ChangeEventArgs e)
        {
            altitudeError = "";
            if (double.TryParse(e.Value?.ToString(), out var inputValue))
            {
                if (inputValue < 1)
                {
                    altitudeError = "Значение должно быть больше 1";
                    StateHasChanged();
                    return;
                }
                
                currentDrone.Altitude = (double)inputValue;
            }
            else
            {
                altitudeError = "Некорректное значение";
            }
            StateHasChanged();
        }

        private void HandleNameInput(ChangeEventArgs e)
        {
            nameError = "";
            string input = e.Value?.ToString()?.Trim() ?? "";

            if (string.IsNullOrEmpty(input))
            {
                nameError = "Название не может быть пустым";
            }
            else if (input.Length > 24)
            {
                nameError = "Максимум 24 символа";
            }

            currentDrone.Name = input;
            StateHasChanged();
        }

        private void ToggleUnit(bool isKm, string type)
        {
            if (type == "flight") isKmFlight = isKm;
            StateHasChanged();
        }
    }
}