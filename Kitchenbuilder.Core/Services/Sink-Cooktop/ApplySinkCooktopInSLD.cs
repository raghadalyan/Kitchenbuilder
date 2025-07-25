﻿using SolidWorks.Interop.sldworks;
using System;
using System.IO;
using Kitchenbuilder.Models;

namespace Kitchenbuilder.Core
{
    public static class ApplySinkCooktopInSLD
    {
        private static string LogPath =>
            Path.Combine(KitchenConfig.Get().BasePath, "Kitchenbuilder", "Output", "ApplySinkCooktopInSLD_Log.txt");

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

                    // ✏️ Apply sink cut dimensions
                    Log($"✏️ Editing Sink Cut Sketch: Width={sink.Width_Sink_Cut}, Length={sink.Length_Sink_Cut}, DX={sink.DX_Sink_Cut}, DY={sink.DY_Sink_Cut}");
                    EditSketchDim_IModel.SetDimension(modelDoc, "Width@Sketch_Sink_Cut", sink.Width_Sink_Cut);
                    EditSketchDim_IModel.SetDimension(modelDoc, "Length@Sketch_Sink_Cut", sink.Length_Sink_Cut);
                    EditSketchDim_IModel.SetDimension(modelDoc, "DX@Sketch_Sink_Cut", sink.DX_Sink_Cut);
                    EditSketchDim_IModel.SetDimension(modelDoc, "DY@Sketch_Sink_Cut", sink.DY_Sink_Cut);
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
