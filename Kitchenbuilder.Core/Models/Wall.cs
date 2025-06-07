using System.Collections.Generic;

namespace Kitchenbuilder.Core.Models
{
    public class Wall
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool HasWindows { get; set; }
        public List<Window>? Windows { get; set; }
        public bool HasDoors { get; set; }
        public List<Door>? Doors { get; set; }
    }
}
