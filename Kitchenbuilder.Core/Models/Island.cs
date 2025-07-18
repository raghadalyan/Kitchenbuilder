namespace Kitchenbuilder.Core.Models
{
    public class Island
    {
        public int Depth { get; set; } = 90;
        public int Width { get; set; } = 180;
        public string Material { get; set; } = "";
        public double Direction { get; set; } = 90;
        public double DistanceX { get; set; } = 0;
        public double DistanceY { get; set; } = 0;
    }
}