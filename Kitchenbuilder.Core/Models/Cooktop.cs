namespace Kitchenbuilder.Models
{

    public class Cooktop
    {
        public int WallNumber { get; set; }
        public int BaseNumber { get; set; }
        public int DistanceFromLeft { get; set; }

        // Dimensions related to cooktop placement on countertop sketch
        public int DistanceX_Cooktop_On_CT { get; set; }       // DistanceX_Cooktop@On_CT
        public int DistanceY_Cooktop_On_CT { get; set; }       // DistanceY_Cooktop@On_CT
        public double Angle_Sketch_Rotate_Cooktop { get; set; }   // angle@Sketch_Rotate_Cooktop
    }
}
