using System.Collections.Generic;

namespace Kitchenbuilder.Core.Models
{
    public class Kitchen
    {
        public int FloorWidth { get; set; }
        public int FloorLength { get; set; }
        public List<Wall> Walls { get; set; } = new();
        public Base? Base { get; set; } // Add this line

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
