using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core
{
    public static class Identify_Hidden_Walls
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\IdentifyHiddenWalls.txt";

        public static void Process(string inputPath, string outputPath, Dictionary<int, List<(double start, double end)>> _unused)
        {
            try
            {
                Log($"🔍 Processing hidden walls for: {Path.GetFileName(inputPath)}");

                string originalJson = File.ReadAllText(inputPath);
                JsonObject original = JsonNode.Parse(originalJson)?.AsObject() ?? new JsonObject();

                string outputJson = File.ReadAllText(outputPath);
                JsonObject result = JsonNode.Parse(outputJson)?.AsObject() ?? new JsonObject();

                var existingWalls = new HashSet<int>();

                foreach (var kvp in original)
                {
                    string key = kvp.Key;
                    if (key.StartsWith("Wall") && int.TryParse(key.Substring(4), out int wallNum))
                        existingWalls.Add(wallNum);
                }

                int exposedWallNum = -1;
                if (original.TryGetPropertyValue("NumOfExposedWall", out JsonNode? exposedNode))
                {
                    exposedWallNum = exposedNode?.GetValue<int>() ?? -1;
                    if (exposedWallNum >= 1 && exposedWallNum <= 4)
                    {
                        existingWalls.Add(exposedWallNum);
                        Log($"🔎 Exposed wall {exposedWallNum} marked as usable");
                    }
                }

                Log($"✅ Usable walls: {string.Join(", ", existingWalls.OrderBy(x => x))}");

                for (int i = 1; i <= 4; i++)
                {
                    if (existingWalls.Contains(i)) continue;

                    var wallKey = $"Wall{i}";
                    var spacesKey = $"SpacesWall{i}";

                    Log($"➕ Adding hidden wall {i}");

                    JsonObject newWall = new JsonObject
                    {
                        [spacesKey] = new JsonArray(),
                        ["Bases"] = new JsonObject
                        {
                            ["Base1"] = CreateHiddenBase(i, 1),
                            ["Base2"] = CreateHiddenBase(i, 2),
                            ["Base3"] = CreateHiddenBase(i, 3),
                        },
                        ["Exposed"] = false
                    };

                    result[wallKey] = newWall;
                }

                File.WriteAllText(outputPath, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                Log($"✅ Output updated: {Path.GetFileName(outputPath)}");
            }
            catch (Exception ex)
            {
                Log($"❌ [Error] {ex.Message}");
            }
        }

        private static JsonObject CreateHiddenBase(int wallNum, int index)
        {
            string sketchName = "";
            string extrudeName = "";

            if (index == 1) // מקרר
            {
                sketchName = $"fridge_base{wallNum}";
                extrudeName = $"Extrude_fridge_base{wallNum}";
            }
            else if (index == 2)
            {
                sketchName = $"{wallNum}_2";
                extrudeName = $"Extrude_{wallNum}_2";
            }
            else if (index == 3)
            {
                sketchName = $"{wallNum}_1";
                extrudeName = $"Extrude_{wallNum}_1";
            }

            return new JsonObject
            {
                ["SketchName"] = sketchName,
                ["ExtrudeName"] = extrudeName,
                ["Start"] = null,
                ["End"] = null,
                ["Visible"] = false,
                ["SmartDim"] = null
            };
        }


        private static void Log(string message)
        {
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
    }
}
