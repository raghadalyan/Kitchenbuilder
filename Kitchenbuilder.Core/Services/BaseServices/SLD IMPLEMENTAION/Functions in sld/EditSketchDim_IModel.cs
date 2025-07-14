using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class EditSketchDim_IModel
    {
        private static readonly string LogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\EditSketchDim_IModel.txt";

        public static void SetDimension(IModelDoc2 modelDoc, string dimName, double size)
        {
            try
            {
                Log($"📏 Editing dimension '{dimName}' to {size}");

                if (modelDoc is not ModelDoc2 model)
                {
                    Log("❌ Cannot cast IModelDoc2 to ModelDoc2.");
                    return;
                }

                var dimObj = model.Parameter(dimName);
                if (dimObj is Dimension dimension)
                {
                    double value;

                    if (dimName.ToLower().Contains("angle"))
                    {
                        // Angle dimension — convert degrees to radians
                        value = size * (Math.PI / 180.0);
                        Log($"ℹ️ Angle detected. Converted {size}° to {value} radians.");
                    }
                    else
                    {
                        // Regular linear dimension — convert cm to meters
                        value = size / 100.0;
                        Log($"ℹ️ Linear dimension. Converted {size} cm to {value} meters.");
                    }

                    dimension.SystemValue = value;

                    model.EditRebuild3();
                    Log($"✅ Dimension '{dimName}' set to {value}. Rebuilt model.");

                    int warn = 0, err = 0;
                    model.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref warn, ref err);
                    Log($"💾 Saved model. Warnings: {warn}, Errors: {err}");
                }
                else
                {
                    Log($"❌ Dimension '{dimName}' not found.");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Exception: {ex.Message}");
            }
        }

        private static void Log(string msg)
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        }
    }
}
