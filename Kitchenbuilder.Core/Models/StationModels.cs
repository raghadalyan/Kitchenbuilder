namespace Kitchenbuilder.Core
{
    public class StationInfo
    {
        public string BaseName { get; set; }
        public int StationStart { get; set; }
        public int StationEnd { get; set; }
        public int WallNumber { get; set; }
        public List<CabinetInfo> Cabinets { get; set; } = new();
    }

    public class CabinetInfo
    {
        public string SketchName { get; set; }
        public int Width { get; set; }
        public bool HasDrawers { get; set; }
    }
}
