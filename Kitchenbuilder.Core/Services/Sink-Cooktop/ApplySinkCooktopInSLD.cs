using SolidWorks.Interop.sldworks;
using System;
using System.IO;
using Kitchenbuilder.Models;

namespace Kitchenbuilder.Core
{
    public static class ApplySinkCooktopInSLD
    {
        private static readonly string LogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\ApplySinkCooktopInSLD_Log.txt";

        private static void Log(string message)
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }

        public static void ApplySinkAndCooktop(IModelDoc2 modelDoc, Sink? sink, Cooktop? cooktop)
        {
            try
            {
                if (modelDoc == null)
                {
                    Log("❌ No active SolidWorks model.");
                    return;
                }

                if (modelDoc is not ModelDoc2 model)
                {
                    Log("❌ Could not cast IModelDoc2 to ModelDoc2.");
                    return;
                }

                if (sink != null)
                {
                    Log($"🔧 Applying Sink placement on Wall{sink.WallNumber}, Base{sink.BaseNumber}");
                    Edit_Sketch_Dim.SetDimension(model, "DistanceX_faucet@On_CT", sink.DistanceX_Faucet_On_CT);
                    Edit_Sketch_Dim.SetDimension(model, "DistanceY_faucet@On_CT", sink.DistanceY_Faucet_On_CT);
                    Edit_Sketch_Dim.SetDimension(model, "angle@Sketch_Rotate_Faucet", sink.Angle_Sketch_Rotate_Faucet);
                }

                if (cooktop != null)
                {
                    Log($"🔥 Applying Cooktop placement on Wall{cooktop.WallNumber}, Base{cooktop.BaseNumber}");
                    Edit_Sketch_Dim.SetDimension(model, "DistanceX_Cooktop@On_CT", cooktop.DistanceX_Cooktop_On_CT);
                    Edit_Sketch_Dim.SetDimension(model, "DistanceY_Cooktop@On_CT", cooktop.DistanceY_Cooktop_On_CT);
                    Edit_Sketch_Dim.SetDimension(model, "angle@Sketch_Rotate_Cooktop", cooktop.Angle_Sketch_Rotate_Cooktop);
                }

                Log("✅ Finished applying sink/cooktop.");
            }
            catch (Exception ex)
            {
                Log($"❌ Exception: {ex.Message}");
            }
        }
    }
}
