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
                                                             // New Sink Cut Dimensions
        public int Width_Sink_Cut { get; set; }
        public int Length_Sink_Cut { get; set; }
        public int DX_Sink_Cut { get; set; }
        public int DY_Sink_Cut { get; set; }
        public void ComputeSinkCutDimensions(int floorWidth, int floorLength, int wall)
        {
            WallNumber = wall;

            if (wall == 1)
            {
                Width_Sink_Cut = 40;
                Length_Sink_Cut = 60;
                DX_Sink_Cut = 10;
                DY_Sink_Cut = DistanceY_Faucet_On_CT - (Length_Sink_Cut / 2);
            }
            else if (wall == 2)
            {
                Width_Sink_Cut = 60;
                Length_Sink_Cut = 40;
                DX_Sink_Cut = DistanceX_Faucet_On_CT - (Width_Sink_Cut / 2);
                DY_Sink_Cut = 10;
            }
            else if (wall == 3)
            {
                Width_Sink_Cut = 40;
                Length_Sink_Cut = 60;
                DX_Sink_Cut = floorLength - 10;
                DY_Sink_Cut = DistanceY_Faucet_On_CT - (Length_Sink_Cut / 2);
            }
            else if (wall == 4)
            {
                Width_Sink_Cut = 60;
                Length_Sink_Cut = 40;
                DX_Sink_Cut = DistanceX_Faucet_On_CT - (Width_Sink_Cut / 2);
                DY_Sink_Cut = floorWidth - 10;
            }
        }
    }
}