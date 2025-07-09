namespace Kitchenbuilder.Core.Models
{
    public class Drawers
    {
        public string SketchName { get; set; } = "";

        public double Width1 { get; set; }
        public double DistanceY1 { get; set; }

        public double Width2 { get; set; }
        public double DistanceY2 { get; set; }

        public double Width3 { get; set; }
        public double DistanceY3 { get; set; }

        public double Width4 { get; set; }
        public double DistanceY4 { get; set; }

        public double Width5 { get; set; }
        public double DistanceY5 { get; set; }

        public Drawers(string sketchName)
        {
            SketchName = sketchName;
        }
    }
}
