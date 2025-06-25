//using SolidWorks.Interop.sldworks;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text.Json;

//namespace Kitchenbuilder.Core
//{
//    public static class HandleCounterTop
//    {
//        public static List<string> GetVisibleSketches(string jsonPath)
//        {
//            var sketches = new List<string>();

//            if (!File.Exists(jsonPath))
//                return sketches;

//            string json = File.ReadAllText(jsonPath);
//            var doc = JsonDocument.Parse(json);

//            foreach (var wall in new[] { "Wall1", "Wall2", "Wall3", "Wall4" })
//            {
//                if (doc.RootElement.TryGetProperty(wall, out var wallElement) &&
//                    wallElement.TryGetProperty("Bases", out var bases))
//                {
//                    foreach (var baseProp in bases.EnumerateObject())
//                    {
//                        var baseData = baseProp.Value;
//                        bool isVisible = baseData.GetProperty("Visible").GetBoolean();
//                        string? sketch = baseData.GetProperty("SketchName").GetString();

//                        if (isVisible && !string.IsNullOrWhiteSpace(sketch) && !sketch.Contains("fridge"))
//                        {
//                            sketches.Add(sketch);
//                        }
//                    }
//                }
//            }

//            return sketches;
//        }

//        public static void ActivateSketchForStation(int index, SolidWorksSessionService session)
//        {
//            try
//            {
//                IModelDoc2? model = session.GetActiveModel();
//                if (model == null)
//                {
//                    Log("❌ No active model found.");
//                    return;
//                }

//                if (LayoutLauncher.StationSketches == null || index >= LayoutLauncher.StationSketches.Count)
//                {
//                    Log($"❌ Invalid index or StationSketches not initialized. Index: {index}");
//                    return;
//                }

//                string sketchName = LayoutLauncher.StationSketches[index];
//                Log($"🎯 Trying to activate sketch: {sketchName}");

//                Feature feature = (Feature)model.FirstFeature();
//                Feature? sketchFeature = null;
//                while (feature != null)
//                {
//                    if (feature.Name == sketchName)
//                    {
//                        sketchFeature = feature;
//                        break;
//                    }
//                    feature = (Feature)feature.GetNextFeature();
//                }

//                if (sketchFeature == null)
//                {
//                    Log("❌ Sketch not found in model.");
//                    return;
//                }

//                bool selected = sketchFeature.Select2(false, -1);
//                if (!selected)
//                {
//                    Log("❌ Failed to select sketch.");
//                    return;
//                }

//                model.EditSketch();  // No return value, just call
//                Log("✅ Sketch activated.");
//            }
//            catch (Exception ex)
//            {
//                Log($"❌ Exception in ActivateSketchForStation: {ex.Message}");
//            }
//        }

//        private static void Log(string message)
//        {
//            string debugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\countertop_debug.txt";
//            string logLine = $"[{DateTime.Now:HH:mm:ss}] {message}";
//            File.AppendAllText(debugPath, logLine + System.Environment.NewLine);
//        }
//    }
//}
