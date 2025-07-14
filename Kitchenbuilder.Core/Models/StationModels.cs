using Kitchenbuilder.Core.Models;
using System.Text.Json.Serialization;

namespace Kitchenbuilder.Core
{
    public class StationInfo
    {
        public string BaseName { get; set; }
        public string ExtrudeName { get; set; } = ""; // <-- Add this line

        public int StationStart { get; set; }
        public int StationEnd { get; set; }
        public int WallNumber { get; set; }
        public List<CabinetInfo> Cabinets { get; set; } = new();
        public bool HasCountertop { get; set; }
        public string SketchName { get; set; }


    }

    public class CabinetInfo
    {
        public string SketchName { get; set; }
        public int Height { get; set; } = 70;
        public int Depth { get; set; } = 60;
        public int Width { get; set; }
       
        public bool HasDrawers { get; set; }
        public int DistanceX { get; set; }  // Start of cabinet along the station
        public int DistanceY { get; set; } = 15;  // Always 
        public string ExtrudeName { get; set; } = "";  // Add this line

        public Drawers? Drawers { get; set; }

    }
}
