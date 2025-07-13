namespace Kitchenbuilder.Core.Models
{
    public class Space
    {
        public string Type { get; set; } = "Other";
        public double Width { get; set; } = 60;
        public double Height { get; set; } = 60;

        public double DistanceX { get; set; }
        public double DistanceY { get; set; }
        public int WallNum { get; set; }


        public Space() { }

        public Space(string type, double width, double height, double depth, double dx, double dy,int wall_num)
        {
            Type = type;
            Width = width;
            Height = height;
            DistanceX = dx;
            DistanceY = dy;
            WallNum = wall_num;
        }
    }
}
