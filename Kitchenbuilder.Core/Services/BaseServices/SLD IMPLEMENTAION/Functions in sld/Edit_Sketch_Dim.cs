using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class Edit_Sketch_Dim
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\EditSketchDim.txt";

        public static void SetDimension(ModelDoc2 model, string dimName, double size)
        {
            try
            {
                Log($"🔍 Trying to edit dimension: {dimName} → {size} mm");

                var dimObj = model.Parameter(dimName);
                if (dimObj is Dimension dimension)
                {
                    Log($"✅ Found dimension '{dimName}', setting to {size / 1000.0} meters");

                    dimension.SystemValue = size / 100.0; // cm to meters
                    model.EditRebuild3();

                    Log("🔁 Rebuilt model after dimension change.");

                    // Save the document
                    int warnings = 0;
                    int errors = 0;
                    model.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref warnings, ref errors);

                    Log($"💾 Saved model. Warnings: {warnings}, Errors: {errors}");
                }
                else
                {
                    Log($"❌ Dimension '{dimName}' not found or not a Dimension object.");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Exception when setting dimension '{dimName}': {ex.Message}");
            }
        }

        private static void Log(string message)
        {
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
    }
}
