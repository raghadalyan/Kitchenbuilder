using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.IO;
using SolidWorks.Interop.swconst;
using Kitchenbuilder.Core.Models;

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
                        // Show drawer body before editing
                        string sketchSuffix = cabinet.SketchName.Replace("Sketch_Cabinet", ""); // e.g., "1_4"
                        string bodyName = $"Extrude_Drawers{sketchSuffix}";
                        Show_Bodies_In_Sld_IModel.ShowBody(model, bodyName);

                        // Cabinet dimensions
                        string widthDim = $"Length@{cabinet.SketchName}";
                        string heightDim = $"Width@{cabinet.SketchName}";
                        string distXDim = $"DistanceX@{cabinet.SketchName}";
                        string distYDim = $"DistanceY@{cabinet.SketchName}";

                        Log($"🛠 Applying cabinet dims: {cabinet.SketchName} => Width={cabinet.Width}, Height={cabinet.Height}, DistanceX={cabinet.DistanceX}");

                        EditSketchDim_IModel.SetDimension(model, widthDim, cabinet.Width);
                        EditSketchDim_IModel.SetDimension(model, heightDim, cabinet.Height);
                        EditSketchDim_IModel.SetDimension(model, distXDim, cabinet.DistanceX);
                        EditSketchDim_IModel.SetDimension(model, distYDim, cabinet.DistanceY);
                        Log($"📐 DistanceY={cabinet.DistanceY} applied to {cabinet.SketchName}");
                        // Apply drawer dimensions (only non-zero)
                        if (cabinet.Drawers != null)
                        {
                            string drawerSketch = cabinet.Drawers.SketchName;

                            for (int i = 1; i <= 5; i++)
                            {
                                var widthProp = typeof(Drawers).GetProperty($"Width{i}");
                                var distYProp = typeof(Drawers).GetProperty($"DistanceY{i}");

                                if (widthProp != null && distYProp != null)
                                {
                                    double widthVal = (double)widthProp.GetValue(cabinet.Drawers)!;
                                    double distYVal = (double)distYProp.GetValue(cabinet.Drawers)!;

                                    if (widthVal > 0 && distYVal > 0)
                                    {
                                        string widthDimName = $"Width{i}@{drawerSketch}";
                                        string distYDimName = $"DistanceY{i}@{drawerSketch}";

                                        EditSketchDim_IModel.SetDimension(model, widthDimName, widthVal);
                                        EditSketchDim_IModel.SetDimension(model, distYDimName, distYVal);

                                        Log($"✏️ Drawer {i}: Width={widthVal}, DistanceY={distYVal} @ {drawerSketch}");
                                    }
                                    else
                                    {
                                        Log($"⏭️ Skipped Drawer {i}: Width={widthVal}, DistanceY={distYVal}");
                                    }
                                }
                            }
                        }
                    }
                }

                // Save and rebuild
                model.EditRebuild3();
                int errs = 0, warns = 0;
                model.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref errs, ref warns);

                Log("✅ All cabinet and drawer dimensions applied and model saved.");
            }
            catch (Exception ex)
            {
                Log($"❌ Exception in ApplyCabinetDimensions: {ex.Message}");
            }
        }

    }
}