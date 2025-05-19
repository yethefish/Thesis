using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Drone 
    {
        public int Id { get; set; }
        [MaxLength(24, ErrorMessage = "Название не может превышать 24 символа")]
        [Required]
        public string Name { get; set; }
        [MaxLength(24, ErrorMessage = "Описание не может превышать 24 символа")]
        public string Description { get; set; }
        [Required]
        public string Color { get; set; }
        public bool IsSelected { get; set; }
        public Polygon Polygon { get; set; }
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        [Required]
        public double Radius { get; set; }
        [Required]
        public double FlightRange { get; set; }
        private static int _idCounter = 0;
        public DronePath Path { get; set; } = new DronePath();

        public Drone()
        {
            Id = _idCounter++;
            Color = GetRandomColor();
            IsSelected = false;
            Polygon = new Polygon();
            Radius = 10;
            FlightRange = 100;
        }

        private string GetRandomColor()
        {
            var random = new Random();
            return String.Format("#{0:X6}", random.Next(0x1000000));
        }
    }
}