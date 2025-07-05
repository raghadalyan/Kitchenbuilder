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

                Log("🛠 Resetting base position: X@On_CT = 0, Y@On_CT = 0");
                EditSketchDim_IModel.SetDimension(modelDoc, "X@On_CT", 0);
                EditSketchDim_IModel.SetDimension(modelDoc, "Y@On_CT", 0);

                if (sink != null)
                {
                    Log($"🔧 Applying Sink placement on Wall{sink.WallNumber}, Base{sink.BaseNumber}");
                    EditSketchDim_IModel.SetDimension(modelDoc, "DistanceX_faucet@On_CT", sink.DistanceX_Faucet_On_CT);
                    EditSketchDim_IModel.SetDimension(modelDoc, "DistanceY_faucet@On_CT", sink.DistanceY_Faucet_On_CT);
                    EditSketchDim_IModel.SetDimension(modelDoc, "angle@Sketch_Rotate_Faucet", sink.Angle_Sketch_Rotate_Faucet);
                }

                if (cooktop != null)
                {
                    Log($"🔥 Applying Cooktop placement on Wall{cooktop.WallNumber}, Base{cooktop.BaseNumber}");
                    EditSketchDim_IModel.SetDimension(modelDoc, "DistanceX_Cooktop@On_CT", cooktop.DistanceX_Cooktop_On_CT);
                    EditSketchDim_IModel.SetDimension(modelDoc, "DistanceY_Cooktop@On_CT", cooktop.DistanceY_Cooktop_On_CT);
                    EditSketchDim_IModel.SetDimension(modelDoc, "angle@Sketch_Rotate_Cooktop", cooktop.Angle_Sketch_Rotate_Cooktop);
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
