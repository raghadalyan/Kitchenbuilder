using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core
{
    public static class HandleUsableWalls
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\HandleUsableWalls.txt";

        public static void Process(string filePath, string outputPath)
        {
            try
            {
                Log($"🔍 Handling usable walls for: {Path.GetFileName(filePath)}");

                string jsonContent = File.ReadAllText(filePath);
                JsonObject original = JsonNode.Parse(jsonContent).AsObject();
                JsonObject result = new JsonObject();

                for (int i = 1; i <= 4; i++)
                {
                    string wallKey = $"Wall{i}";
                    string spacesKey = $"SpacesWall{i}";

                    if (!original.ContainsKey(wallKey) && !original.ContainsKey(spacesKey))
                        continue;

                    JsonObject wallObj = new JsonObject();

                    // Clone and add SpacesWall
                    JsonArray? fullSpaces = null;
                    if (original[spacesKey] is JsonArray spacesArray)
                    {
                        fullSpaces = (JsonArray)DeepClone(spacesArray)!;
                        wallObj[spacesKey] = fullSpaces;
                    }

                    // Handle bases
                    JsonObject bases = new JsonObject();
                    for (int j = 1; j <= 3; j++)
                    {
                        string sketchName = j == 2 ? $"fridge_base{i}" : $"{i}_{(j == 1 ? 1 : 2)}";
                        string extrudeName = j == 2 ? $"Extrude_fridge_base{i}" : $"Extrude_{i}_{(j == 1 ? 1 : 2)}";

                        var baseObj = new JsonObject
                        {
                            ["SketchName"] = sketchName,
                            ["ExtrudeName"] = extrudeName,
                            ["Start"] = null,
                            ["End"] = null,
                            ["Visible"] = false
                        };

                        if (fullSpaces != null)
                        {
                            int baseIndex = j == 2 ? 1 : j == 1 ? 0 : 2;
                            if (baseIndex < fullSpaces.Count && fullSpaces[baseIndex] is JsonObject space)
                            {
                                baseObj["Start"] = space["Start"]?.GetValue<double>();
                                baseObj["End"] = space["End"]?.GetValue<double>();

                                baseObj["Visible"] = true;
                            }
                        }

                        bases[$"Base{j}"] = baseObj;
                    }

                    wallObj["Bases"] = bases;

                    // Clone and insert fridge if needed
                    if (original.ContainsKey("Fridge") && original["FridgeWall"]?.ToString() == i.ToString())
                    {
                        wallObj["Fridge"] = DeepClone(original["Fridge"]);
                    }

                    wallObj["Exposed"] = (original.ContainsKey("NumOfExposedWall") &&
                                           original["NumOfExposedWall"]?.ToString() == i.ToString());

                    result[wallKey] = DeepClone(wallObj);
                }

                File.WriteAllText(outputPath, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                Log($"✅ Output written to: {Path.GetFileName(outputPath)}");
            }
            catch (Exception ex)
            {
                Log($"❌ Error: {ex.Message}");
            }
        }

        private static JsonNode? DeepClone(JsonNode? node)
        {
            return node == null ? null : JsonNode.Parse(node.ToJsonString());
        }

        private static void Log(string msg)
        {
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        }
    }
}
