namespace Kitchenbuilder.Models
{
    public class Sink
    {
        public int WallNumber { get; set; }
        public int BaseNumber { get; set; }
        public int DistanceFromLeft { get; set; }

        // Dimensions related to faucet placement on countertop sketch
        public int DistanceX_Faucet_On_CT { get; set; }      // DistanceX_faucet@On_CT
        public int DistanceY_Faucet_On_CT { get; set; }      // DistanceY_faucet@On_CT
        public int Angle_Sketch_Rotate_Faucet { get; set; }  // angle@Sketch_Rotate_Faucet
    }
}