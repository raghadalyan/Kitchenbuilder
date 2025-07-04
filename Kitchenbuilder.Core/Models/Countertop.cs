namespace Kitchenbuilder.Models
{
    public class Countertop
    {
        public string BaseKey { get; set; } = "";
        public double Start { get; set; }
        public double End { get; set; }
        public double Width => End - Start;
        public int WallNumber { get; set; }
    }
}