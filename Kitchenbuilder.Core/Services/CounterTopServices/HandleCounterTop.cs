using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Kitchenbuilder.Core.Services;

namespace Kitchenbuilder.Core
{
    public static class HandleCounterTop
    {
        public static List<string> GetVisibleSketches(string jsonPath)
        {
            Log("📌 Starting GetVisibleSketches...");
            var sketches = new List<string>();

            if (!File.Exists(jsonPath))
            {
                Log($"❌ JSON not found: {jsonPath}");
                return sketches;
            }

            Log("📖 Reading JSON content...");
            string json = File.ReadAllText(jsonPath);
            var doc = JsonDocument.Parse(json);

            foreach (var wall in new[] { "Wall1", "Wall2", "Wall3", "Wall4" })
            {
                Log($"🔍 Checking wall: {wall}");
                if (doc.RootElement.TryGetProperty(wall, out var wallElement) &&
                    wallElement.TryGetProperty("Bases", out var bases))
                {
                    foreach (var baseProp in bases.EnumerateObject())
                    {
                        var baseData = baseProp.Value;
                        bool isVisible = baseData.GetProperty("Visible").GetBoolean();
                        string? sketch = baseData.GetProperty("SketchName").GetString();

                        Log($"➡️ Base: {baseProp.Name}, Visible={isVisible}, Sketch={sketch}");

                        if (isVisible && !string.IsNullOrWhiteSpace(sketch) && !sketch.ToLower().Contains("fridge"))
                        {
                            sketches.Add(sketch);
                            Log($"✅ Collected sketch: {sketch}");
                        }
                    }
                }
            }

            Log("📦 Finished collecting sketches.");
            return sketches;
        }

        public static void ApplyCountertopData(string jsonPath, List<BaseDistance> distances, SolidWorksSessionService session)
        {
            Log("📌 Starting ApplyCountertopData...");

            if (!File.Exists(jsonPath))
            {
                Log($"❌ JSON not found: {jsonPath}");
                return;
            }

            string json = File.ReadAllText(jsonPath);
            var kitchenData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
            var updatedData = new Dictionary<string, object>();
            int index = 0;
            int baseCountProcessed = 0;

            foreach (var wall in new[] { "Wall1", "Wall2", "Wall3", "Wall4" })
            {
                Log($"🏠 Processing wall: {wall}");

                if (!kitchenData.TryGetValue(wall, out JsonElement wallElement))
                {
                    Log($"⚠️ Wall '{wall}' not found in JSON.");
                    continue;
                }

                var wallDict = JsonSerializer.Deserialize<Dictionary<string, object>>(wallElement.GetRawText())!;
                if (!wallDict.TryGetValue("Bases", out var basesObj))
                {
                    Log($"⚠️ No 'Bases' in {wall}");
                    continue;
                }

                var bases = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(
                    JsonSerializer.Serialize(basesObj)
                )!;

                foreach (var baseKey in bases.Keys.ToList())
                {
                    var baseData = bases[baseKey];
                    Log($"➡️ Base: {baseKey}");

                    if (!baseData.TryGetValue("Visible", out var visibleObj) || !baseData.TryGetValue("SketchName", out var sketchObj))
                    {
                        Log("⚠️ Missing 'Visible' or 'SketchName' in base.");
                        continue;
                    }

                    bool isVisible = (visibleObj is JsonElement ve) ? ve.GetBoolean() : Convert.ToBoolean(visibleObj);
                    string sketch = (sketchObj is JsonElement se) ? se.GetString() ?? "" : sketchObj.ToString() ?? "";

                    Log($"🔍 Visible={isVisible}, Sketch={sketch}");

                    if (!isVisible || sketch.ToLower().Contains("fridge"))
                    {
                        Log("⏩ Skipping this base.");
                        continue;
                    }

                    string centerTopName = $"Extrude_CT_{sketch}";
                    double left = index < distances.Count ? distances[index].Left : 0;
                    double right = index < distances.Count ? distances[index].Right : 0;

                    baseData["Countertop"] = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            { "Name", centerTopName },
                            { "L", left },
                            { "R", right }
                        }
                    };
                    baseData["IsCountertopVisible"] = true;

                    bases[baseKey] = baseData;
                    Log($"✅ Added countertop: {centerTopName}, L={left}, R={right}");

                    index++;
                    baseCountProcessed++;
                }

                wallDict["Bases"] = bases;
                updatedData[wall] = wallDict;
            }

            foreach (var kvp in kitchenData)
            {
                if (!updatedData.ContainsKey(kvp.Key))
                    updatedData[kvp.Key] = kvp.Value;
            }

            string updatedJson = JsonSerializer.Serialize(updatedData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonPath, updatedJson);
            Log($"💾 JSON saved. Total processed bases: {baseCountProcessed}");

            Log("👁️ Calling CheckVisibleCountertops...");
            CheckVisibleCountertops.ProcessVisibleCountertops(jsonPath, session);
            Log("✅ Done ApplyCountertopData.");
        }

        public static void PreFillCountertops(string jsonPath)
        {
            Log("📌 Starting PreFillCountertops...");

            if (!File.Exists(jsonPath))
            {
                Log($"❌ JSON not found: {jsonPath}");
                return;
            }

            string json = File.ReadAllText(jsonPath);
            var kitchenData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
            var updatedData = new Dictionary<string, object>();
            int totalCountertopsAdded = 0;

            foreach (var wall in new[] { "Wall1", "Wall2", "Wall3", "Wall4" })
            {
                Log($"🏠 Pre-filling wall: {wall}");

                if (!kitchenData.TryGetValue(wall, out JsonElement wallElement))
                {
                    Log($"⚠️ Wall '{wall}' not found in JSON.");
                    continue;
                }

                var wallDict = JsonSerializer.Deserialize<Dictionary<string, object>>(wallElement.GetRawText())!;
                if (!wallDict.TryGetValue("Bases", out var basesObj))
                {
                    Log($"⚠️ No 'Bases' found in wall: {wall}");
                    continue;
                }

                var bases = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(
                    JsonSerializer.Serialize(basesObj)
                )!;

                foreach (var baseKey in bases.Keys.ToList())
                {
                    var baseData = bases[baseKey];

                    if (!baseData.TryGetValue("Visible", out var visibleObj) || !baseData.TryGetValue("SketchName", out var sketchObj))
                    {
                        Log($"⚠️ Base '{baseKey}' missing 'Visible' or 'SketchName'.");
                        continue;
                    }

                    bool isVisible = (visibleObj is JsonElement ve) ? ve.GetBoolean() : Convert.ToBoolean(visibleObj);
                    string sketch = (sketchObj is JsonElement se) ? se.GetString() ?? "" : sketchObj.ToString() ?? "";

                    if (!isVisible)
                    {
                        Log($"⏩ Base '{baseKey}' is not visible.");
                        continue;
                    }

                    if (sketch.ToLower().Contains("fridge"))
                    {
                        Log($"⏩ Skipping fridge sketch: {sketch}");
                        continue;
                    }

                    string centerTopName = $"Extrude_CT_{sketch}";

                    baseData["Countertop"] = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            { "Name", centerTopName },
                            { "L", 0 },
                            { "R", 0 }
                        }
                    };
                    baseData["IsCountertopVisible"] = true;

                    bases[baseKey] = baseData;

                    Log($"✅ Pre-filled Countertop for Base '{baseKey}': {centerTopName}, L=0, R=0");
                    totalCountertopsAdded++;
                }

                wallDict["Bases"] = bases;
                updatedData[wall] = wallDict;
            }

            foreach (var kvp in kitchenData)
            {
                if (!updatedData.ContainsKey(kvp.Key))
                    updatedData[kvp.Key] = kvp.Value;
            }

            string updatedJson = JsonSerializer.Serialize(updatedData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonPath, updatedJson);
            Log($"💾 JSON updated and saved: {jsonPath}");
            Log($"📊 Total Countertops Added: {totalCountertopsAdded}");
            Log("✅ Done PreFillCountertops.");
        }

        public static void ActivateSketchForStation(int index, SolidWorksSessionService session)
        {
            Log($"📌 Activating **countertop sketch** for station index: {index}");

            try
            {
                IModelDoc2? model = session.GetActiveModel();
                if (model == null)
                {
                    Log("❌ No active model found.");
                    return;
                }

                if (LayoutLauncher.StationSketches == null || index >= LayoutLauncher.StationSketches.Count)
                {
                    Log($"❌ Invalid index or StationSketches not initialized. Index: {index}");
                    return;
                }

                // 👇 نحاول تحديد سكيتش الشايش بناءً على الاسم المعدّل
                string originalSketch = LayoutLauncher.StationSketches[index];
                string ctSketchName = $"CT_{originalSketch}";

                Log($"🎯 Trying to activate sketch: {ctSketchName}");

                Feature? feature = model.FirstFeature() as Feature;
                Feature? sketchFeature = null;

                while (feature != null)
                {
                    if (feature.Name.Equals(ctSketchName, StringComparison.OrdinalIgnoreCase))
                    {
                        sketchFeature = feature;
                        break;
                    }
                    feature = feature.GetNextFeature() as Feature;
                }

                if (sketchFeature == null)
                {
                    Log("❌ Sketch not found in model.");
                    return;
                }

                bool selected = sketchFeature.Select2(false, -1);
                Log(selected ? "✅ Sketch selected." : "❌ Failed to select sketch.");

                model.EditSketch();
                Log("✅ Called EditSketch.");
            }
            catch (Exception ex)
            {
                Log($"❌ Exception in ActivateSketchForStation: {ex.Message}");
            }
        }


        private static void Log(string message)
        {
            string debugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\countertop_debug.txt";
            string logLine = $"[{DateTime.Now:HH:mm:ss}] {message}";
            File.AppendAllText(debugPath, logLine + System.Environment.NewLine);
        }

        public class BaseDistance
        {
            public double Left { get; set; }
            public double Right { get; set; }
        }
    }
}
