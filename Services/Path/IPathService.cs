using Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface IPathService
    {
        Task CalculateAndDrawDronePath(Drone drone);
        Task CalculateAndDrawAllDronePaths(List<Drone> drones);
        List<Models.Point> CalculateOptimalPath(Drone drone);
        Task ClearDronePath(Drone drone);
        Task<double> GetPathLength(Drone drone);
        Task<string> GenerateMavlinkWaypointsCsv(Drone drone);
    }
}