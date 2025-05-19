
namespace Models
{
    public class Point
    {
        public Point(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
