using Models;

namespace Services
{
    public interface IMapService
    {
        Task InitializeMap();
        Task ToggleEditMode(bool isEditMode);
        Task ToggleDeleteMode(bool isDeleteMode);
        Task OpenEditMenu(bool isOpen);
        Task ToggleStartFinishMode(bool isStartFinishMode);
        Task DrawStartFinishPoints(Drone drone);
        Task ClearDroneStartFinishPoints(string droneColor);
        Task RedrawPolygon(Drone drone);
        Task RedrawAllPolygons(List<Drone> drones);
        Task ClearPolygon(Polygon polygon);
        Task ToggleLock(bool isLocked);
        Task SearchAddress(string address);
        Task<string> GetAddressInputValue();
    }
}