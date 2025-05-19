namespace Models
{
    public class DronePath
    {
        public List<Point> Points { get; set; } = new List<Point>();
        public string Color { get; set; }

        public double CalculateLength()
        {
            if (Points.Count < 2) return 0;
            
            double totalLength = 0;
            for (int i = 0; i < Points.Count - 1; i++)
            {
                totalLength += Polygon.GetDistance(Points[i], Points[i + 1]);
            }
            return totalLength;
        }
    }
}