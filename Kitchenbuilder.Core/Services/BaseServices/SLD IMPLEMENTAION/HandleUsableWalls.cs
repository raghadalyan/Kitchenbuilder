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

                    // Handle fridge overlap
                    JsonObject? fridgeSpace = null;
                    int fridgeSpaceIndex = -1;
                    if (fridgeWall == wallNum && fridge != null)
                    {
                        double fStart = fridge["Start"]!.GetValue<double>();
                        double fEnd = fridge["End"]!.GetValue<double>();
                        for (int i = 0; i < spacesArray.Count; i++)
                        {
                            if (spacesArray[i] is JsonObject sp)
                            {
                                double s = sp["Start"]!.GetValue<double>();
                                double e = sp["End"]!.GetValue<double>();
                                if (fStart >= s && fEnd <= e)
                                {
                                    fridgeSpace = sp;
                                    fridgeSpaceIndex = i;
                                    break;
                                }
                            }
                        }
                    }

                    // Create bases
                    JsonObject bases = new JsonObject();
                    for (int b = 0; b < 3; b++)
                    {
                        string sketch = (b == 1)
                            ? $"fridge_base{wallNum}"
                            : $"{wallNum}_{(b == 0 ? 1 : 2)}";
                        string extrude = (b == 1)
                            ? $"Extrude_fridge_base{wallNum}"
                            : $"Extrude_{wallNum}_{(b == 0 ? 1 : 2)}";

                        JsonObject baseObj = new JsonObject
                        {
                            ["SketchName"] = sketch,
                            ["ExtrudeName"] = extrude,
                            ["Start"] = null,
                            ["End"] = null,
                            ["Visible"] = false
                        };

                        if (b == 1 && fridgeSpace != null && fridge != null)
                        {
                            baseObj["Start"] = fridge["Start"]!.GetValue<double>();
                            baseObj["End"] = fridge["End"]!.GetValue<double>();
                            baseObj["Visible"] = true;
                        }
                        else if ((b == 0 || b == 2) && spacesArray.Count > b && spacesArray[b] is JsonObject space)
                        {
                            double s = space["Start"]!.GetValue<double>();
                            double e = space["End"]!.GetValue<double>();

                            if (fridgeSpace != null && fridge != null && b == 0)
                            {
                                double fEnd = fridge["End"]!.GetValue<double>();
                                if (fEnd < e)
                                {
                                    baseObj["Start"] = fEnd;
                                    baseObj["End"] = e;
                                    baseObj["Visible"] = true;
                                }
                            }
                            else
                            {
                                baseObj["Start"] = s;
                                baseObj["End"] = e;
                                baseObj["Visible"] = true;
                            }
                        }

                        bases[$"Base{b + 1}"] = baseObj;
                    }

                    // Only add fridge_base if not already handled
                    if (!bases.ContainsKey("Base2") && fridgeWall == wallNum && fridge != null)
                    {
                        JsonObject fridgeBase = new JsonObject
                        {
                            ["SketchName"] = $"fridge_base{wallNum}",
                            ["ExtrudeName"] = $"Extrude_fridge_base{wallNum}",
                            ["Start"] = fridge["Start"]!.GetValue<double>(),
                            ["End"] = fridge["End"]!.GetValue<double>(),
                            ["Visible"] = true
                        };

                        bases["Base2"] = fridgeBase;
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
