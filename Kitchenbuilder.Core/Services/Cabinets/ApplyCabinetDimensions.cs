using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.IO;
using SolidWorks.Interop.swconst;

namespace Kitchenbuilder.Core
{
    public static class ApplyCabinetDimensions
    {
        private static string debugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\ApplyCabinetDimensions.txt";

        private static void Log(string message)
        {
            File.AppendAllText(debugPath, $"{DateTime.Now:HH:mm:ss} - {message}{System.Environment.NewLine}");
        }

        public static void Apply(IModelDoc2 model, List<StationInfo> stations)
        {
            try
            {
                if (model == null)
                {
                    Log("❌ IModelDoc2 is null.");
                    return;
                }

                foreach (var station in stations)
                {
                    foreach (var cabinet in station.Cabinets)
                    {
                        // Step 1: Show body before editing
                        string sketchSuffix = cabinet.SketchName.Replace("Sketch_Cabinet", ""); // e.g., "1_4"
                        string bodyName = $"Extrude_Drawers{sketchSuffix}";
                        Show_Bodies_In_Sld_IModel.ShowBody(model, bodyName);

                        // Step 2: Apply dimensions
                        string widthDim = $"Length@{cabinet.SketchName}";
                        string distXDim = $"DistanceX@{cabinet.SketchName}";

                        Log($"🛠 Applying dimensions to {cabinet.SketchName}: Width={cabinet.Width}, DistanceX={cabinet.DistanceX}");

                        EditSketchDim_IModel.SetDimension(model, widthDim, cabinet.Width);
                        EditSketchDim_IModel.SetDimension(model, distXDim, cabinet.DistanceX);
                    }
                }

                // Save and rebuild
                model.EditRebuild3();
                int errs = 0, warns = 0;
                model.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref errs, ref warns);

                Log("✅ All cabinet dimensions applied and model saved.");
            }
            catch (Exception ex)
            {
                Log($"❌ Exception in ApplyCabinetDimensions: {ex.Message}");
            }
        }
    }
}
