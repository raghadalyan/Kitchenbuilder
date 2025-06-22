using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core
{
    public static class HandleUsableWalls
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\HandleUsableWalls.txt";

        public static void Process(string inputPath, string outputPath)
        {
            try
            {
                Log($"🔍 Handling usable walls for: {Path.GetFileName(inputPath)}");

                string jsonContent = File.ReadAllText(inputPath);
                JsonObject original = JsonNode.Parse(jsonContent).AsObject();
                JsonObject result = File.Exists(outputPath)
                    ? JsonNode.Parse(File.ReadAllText(outputPath)).AsObject()
                    : new JsonObject();

                int? fridgeWall = original["FridgeWall"]?.GetValue<int>();
                var fridge = original["Fridge"]?.AsObject();

                HashSet<int> usableWalls = new();
                for (int i = 1; i <= 4; i++)
                {
                    if (original.ContainsKey($"Wall{i}") || original.ContainsKey($"SpacesWall{i}") ||
                        (original["NumOfExposedWall"]?.GetValue<int>() == i))
                    {
                        usableWalls.Add(i);
                    }
                }

                foreach (int wallNum in usableWalls)
                {
                    JsonObject wallObj = new JsonObject();

                    // Handle spaces
                    string spacesKey = $"SpacesWall{wallNum}";
                    JsonArray spacesArray;

                    if (original.ContainsKey(spacesKey))
                    {
                        spacesArray = original[spacesKey]!.AsArray();
                        wallObj[spacesKey] = spacesArray.DeepClone();
                    }
                    else if (original["NumOfExposedWall"]?.GetValue<int>() == wallNum && original.ContainsKey("ExposedWallSpace"))
                    {
                        JsonArray tempArr = new JsonArray();
                        var exposedSpace = original["ExposedWallSpace"]!.DeepClone().AsObject();
                        exposedSpace["Name"] = "";
                        tempArr.Add(exposedSpace);
                        wallObj[spacesKey] = tempArr;
                        spacesArray = tempArr;
                    }
                    else continue;

                    // Create dynamic bases
                    JsonObject bases = new JsonObject();
                    int baseCounter = 1;

                    for (int i = 0; i < spacesArray.Count; i++)
                    {
                        if (spacesArray[i] is not JsonObject space)
                            continue;

                        double spaceStart = space["Start"]!.GetValue<double>();
                        double spaceEnd = space["End"]!.GetValue<double>();

                        bool isFridgeHere = fridgeWall == wallNum &&
                                            fridge != null &&
                                            fridge["Start"]!.GetValue<double>() >= spaceStart &&
                                            fridge["End"]!.GetValue<double>() <= spaceEnd;

                        if (isFridgeHere)
                        {
                            double fStart = fridge["Start"]!.GetValue<double>();
                            double fEnd = fridge["End"]!.GetValue<double>();

                            // Before fridge
                            if (spaceStart < fStart)
                            {
                                bases[$"Base{baseCounter}"] = new JsonObject
                                {
                                    ["SketchName"] = $"{wallNum}_{baseCounter}",
                                    ["ExtrudeName"] = $"Extrude_{wallNum}_{baseCounter}",
                                    ["Start"] = spaceStart,
                                    ["End"] = fStart,
                                    ["Visible"] = true
                                };
                                baseCounter++;
                            }

                            // Fridge base
                            bases[$"Base{baseCounter}"] = new JsonObject
                            {
                                ["SketchName"] = $"fridge_base{wallNum}",
                                ["ExtrudeName"] = $"Extrude_fridge_base{wallNum}",
                                ["Start"] = fStart,
                                ["End"] = fEnd,
                                ["Visible"] = true
                            };
                            baseCounter++;

                            // After fridge
                            if (fEnd < spaceEnd)
                            {
                                bases[$"Base{baseCounter}"] = new JsonObject
                                {
                                    ["SketchName"] = $"{wallNum}_{baseCounter}",
                                    ["ExtrudeName"] = $"Extrude_{wallNum}_{baseCounter}",
                                    ["Start"] = fEnd,
                                    ["End"] = spaceEnd,
                                    ["Visible"] = true
                                };
                                baseCounter++;
                            }
                        }
                        else
                        {
                            // Normal full space base
                            bases[$"Base{baseCounter}"] = new JsonObject
                            {
                                ["SketchName"] = $"{wallNum}_{baseCounter}",
                                ["ExtrudeName"] = $"Extrude_{wallNum}_{baseCounter}",
                                ["Start"] = spaceStart,
                                ["End"] = spaceEnd,
                                ["Visible"] = true
                            };
                            baseCounter++;
                        }
                    }

                    // Ensure we always have exactly 3 bases
                    while (baseCounter <= 3)
                    {
                        string sketch = (baseCounter == 2)
                            ? $"fridge_base{wallNum}"
                            : $"{wallNum}_{baseCounter}";
                        string extrude = (baseCounter == 2)
                            ? $"Extrude_fridge_base{wallNum}"
                            : $"Extrude_{wallNum}_{baseCounter}";

                        bases[$"Base{baseCounter}"] = new JsonObject
                        {
                            ["SketchName"] = sketch,
                            ["ExtrudeName"] = extrude,
                            ["Start"] = null,
                            ["End"] = null,
                            ["Visible"] = false
                        };

                        baseCounter++;
                    }

                    wallObj["Bases"] = bases;
                    wallObj["Exposed"] = original["NumOfExposedWall"]?.GetValue<int>() == wallNum;

                    if (fridgeWall == wallNum && fridge != null)
                        wallObj["Fridge"] = fridge.DeepClone();

                    result[$"Wall{wallNum}"] = wallObj;
                }

                File.WriteAllText(outputPath, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                Log($"✅ Output written to: {Path.GetFileName(outputPath)}");
            }
            catch (Exception ex)
            {
                Log($"❌ Error: {ex.Message}");
            }
        }

        private static void Log(string msg)
        {
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        }

        private static JsonNode? DeepClone(this JsonNode? node)
        {
            return node == null ? null : JsonNode.Parse(node.ToJsonString());
        }





    }
}
