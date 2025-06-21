using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core
{
    public static class Identify_Hidden_Walls
    {
        private static readonly string LogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\IdentifyHiddenWalls.txt";

        public static void Process(string originalJsonPath, string sldJsonPath)
        {
            try
            {
                File.AppendAllText(LogPath, $"\n🔍 Processing: {Path.GetFileName(originalJsonPath)}\n");

                var originalJson = JsonNode.Parse(File.ReadAllText(originalJsonPath)).AsObject();
                var sldJson = JsonNode.Parse(File.ReadAllText(sldJsonPath)).AsObject();

                var shown = new JsonObject();
                var deleted = new JsonObject();

                var usedWalls = new HashSet<int>();

                if (originalJson.ContainsKey("Wall1")) usedWalls.Add(originalJson["Wall1"].GetValue<int>());
                if (originalJson.ContainsKey("Wall2")) usedWalls.Add(originalJson["Wall2"].GetValue<int>());
                if (originalJson.TryGetPropertyValue("Exposed", out var exposedNode) &&
                    exposedNode.GetValue<bool>() &&
                    originalJson.TryGetPropertyValue("NumOfExposedWall", out var exposedWallNode))
                {
                    usedWalls.Add(exposedWallNode.GetValue<int>());
                }

                for (int wallNum = 1; wallNum <= 4; wallNum++)
                {
                    string wallKey = $"Wall{wallNum}";

                    if (usedWalls.Contains(wallNum))
                    {
                        var wallObj = new JsonObject();

                        // Check if this wall is Wall1/Wall2 in the logic
                        int? matchField = null;
                        if (originalJson.TryGetPropertyValue("Wall1", out var w1) && w1.GetValue<int>() == wallNum)
                            matchField = 1;
                        else if (originalJson.TryGetPropertyValue("Wall2", out var w2) && w2.GetValue<int>() == wallNum)
                            matchField = 2;

                        string spaceField = matchField != null ? $"SpacesWall{matchField}" : null;

                        if (spaceField != null && originalJson.TryGetPropertyValue(spaceField, out var spacesNode))
                        {
                            var clonedArray = new JsonArray();
                            foreach (var space in spacesNode.AsArray())
                            {
                                var obj = space.AsObject();
                                obj["Name"] = "";
                                clonedArray.Add(obj.DeepClone());
                            }
                            wallObj[$"SpacesWall{matchField}"] = clonedArray;
                        }

                        // If this is the exposed wall and it has ExposedWallSpace
                        if (originalJson.TryGetPropertyValue("ExposedWallSpace", out var exposedWallSpace) &&
                            originalJson.TryGetPropertyValue("NumOfExposedWall", out var numExposedNode) &&
                            numExposedNode.GetValue<int>() == wallNum)
                        {
                            var spaceObj = exposedWallSpace.AsObject();
                            spaceObj["Name"] = "";
                            var arr = new JsonArray { spaceObj.DeepClone() };
                            wallObj[$"SpacesWall{wallNum}"] = arr;
                        }

                        // Fridge
                        if (originalJson.TryGetPropertyValue("FridgeWall", out var fw) &&
                            fw.GetValue<int>() == wallNum)
                        {
                            wallObj["Fridge"] = originalJson["Fridge"].DeepClone();
                        }

                        wallObj["Exposed"] =
                            originalJson.TryGetPropertyValue("NumOfExposedWall", out var numExp) &&
                            numExp.GetValue<int>() == wallNum;

                        shown[wallKey] = wallObj;
                    }
                    else
                    {
                        deleted[wallKey] = new JsonObject();
                    }
                }

                sldJson["Shown"] = shown;
                sldJson["Deleted"] = deleted;

                File.WriteAllText(sldJsonPath, JsonSerializer.Serialize(sldJson, new JsonSerializerOptions { WriteIndented = true }));
                File.AppendAllText(LogPath, $"✅ Updated {Path.GetFileName(sldJsonPath)} with shown and deleted walls.\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(LogPath, $"❌ Error: {ex.Message}\n");
            }
        }
    }
}
