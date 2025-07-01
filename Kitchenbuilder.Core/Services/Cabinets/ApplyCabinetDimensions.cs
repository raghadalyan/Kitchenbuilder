using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class ApplyCabinetDimensions
    {
        private static string debugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\ApplyCabinetDimensions.txt";

        private static void Log(string message)
        {
            File.AppendAllText(debugPath, $"{DateTime.Now:HH:mm:ss} - {message}{System.Environment.NewLine}");
        }

        public static void Apply(ModelDoc2 model, List<CabinetInfo> cabinets)
        {
            try
            {
                if (model == null)
                {
                    Log("❌ ModelDoc2 is null.");
                    return;
                }

                foreach (var cabinet in cabinets)
                {
                    string widthDim = $"Length@{cabinet.SketchName}";
                    string distXDim = $"DistanceX@{cabinet.SketchName}";

                    Log($"🛠 Applying dimensions to {cabinet.SketchName}: Width={cabinet.Width}, DistanceX={cabinet.DistanceX}");

                    Edit_Sketch_Dim.SetDimension(model, widthDim, cabinet.Width);
                    Edit_Sketch_Dim.SetDimension(model, distXDim, cabinet.DistanceX);
                }

                Log("✅ All cabinet dimensions applied successfully.");
            }
            catch (Exception ex)
            {
                Log($"❌ Exception in ApplyCabinetDimensions: {ex.Message}");
            }
        }
    }
}
