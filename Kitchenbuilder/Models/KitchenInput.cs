using System.Collections.Generic;

namespace Kitchenbuilder.Models
{
    public class KitchenInput
    {
        public int FloorWidth { get; set; }
        public int FloorLength { get; set; }
        public List<Wall> Walls { get; set; } = new();
    }

    public class Wall
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool HasWindows { get; set; }
        public List<Window>? Windows { get; set; }
    }

    public class Window
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int DistanceX { get; set; }
        public int DistanceY { get; set; }
    }
}
