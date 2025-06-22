using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core
{
    public static class HandleUsableWalls
    {
        private static readonly string DebugPath = @"C:\\Users\\chouse\\Downloads\\Kitchenbuilder\\Output\\HandleUsableWalls.txt";

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

                    JsonObject bases = new JsonObject();
                    int baseCounter = 1;

                    for (int i = 0; i < spacesArray.Count; i++)
                    {
                        if (spacesArray[i] is not JsonObject space) continue;

                        double spaceStart = space["Start"]!.GetValue<double>();
                        double spaceEnd = space["End"]!.GetValue<double>();

                        bool isFridgeHere = fridgeWall == wallNum && fridge != null &&
                                             fridge["Start"]!.GetValue<double>() >= spaceStart &&
                                             fridge["End"]!.GetValue<double>() <= spaceEnd;

                        if (isFridgeHere)
                        {
                            double fStart = fridge["Start"]!.GetValue<double>();
                            double fEnd = fridge["End"]!.GetValue<double>();

                            if (spaceStart < fStart)
                            {
                                bases[$"Base{baseCounter}"] = CreateSmartBase(wallNum, baseCounter, spaceStart, fStart, true);
                                baseCounter++;
                            }

                            bases[$"Base{baseCounter}"] = CreateSmartBase(wallNum, -1, fStart, fEnd, true);
                            baseCounter++;

                            if (fEnd < spaceEnd)
                            {
                                bases[$"Base{baseCounter}"] = CreateSmartBase(wallNum, baseCounter, fEnd, spaceEnd, true);
                                baseCounter++;
                            }
                        }
                        else
                        {
                            bases[$"Base{baseCounter}"] = CreateSmartBase(wallNum, baseCounter, spaceStart, spaceEnd, true);
                            baseCounter++;
                        }
                    }

                    while (baseCounter <= 3)
                    {
                        bases[$"Base{baseCounter}"] = CreateSmartBase(wallNum, baseCounter, null, null, false);
                        baseCounter++;
                    }

                    wallObj["Bases"] = bases;
                    wallObj["Exposed"] = original["NumOfExposedWall"]?.GetValue<int>() == wallNum;

                    if (fridgeWall == wallNum && fridge != null)
                        wallObj["Fridge"] = fridge.DeepClone();

                    result[$"Wall{wallNum}"] = wallObj;
                }

                AdjustCornerBases(original, result);

                File.WriteAllText(outputPath, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                Log($"✅ Output written to: {Path.GetFileName(outputPath)}");
            }
            catch (Exception ex)
            {
                Log($"❌ Error: {ex.Message}");
            }
        }

        private static JsonObject CreateSmartBase(int wallNum, int index, double? start, double? end, bool visible)
        {
            string sketchName = index == -1 ? $"fridge_base{wallNum}" : $"{wallNum}_{index}";
            string extrudeName = index == -1 ? $"Extrude_fridge_base{wallNum}" : $"Extrude_{wallNum}_{index}";

            JsonObject baseObj = new JsonObject
            {
                ["SketchName"] = sketchName,
                ["ExtrudeName"] = extrudeName,
                ["Start"] = start is not null ? JsonValue.Create(start) : null,
                ["End"] = end is not null ? JsonValue.Create(end) : null,
                ["Visible"] = visible
            };

            if (!visible)
            {
                baseObj["SmartDim"] = null;
                return baseObj;
            }

            JsonArray smartDims = new JsonArray
            {
                new JsonObject
                {
                    ["Name"] = $"DistaceX@{sketchName}",
                    ["Size"] = start ?? 0
                }
            };

            if (index != -1 && start.HasValue && end.HasValue)
            {
                smartDims.Add(new JsonObject
                {
                    ["Name"] = $"length@{sketchName}",
                    ["Size"] = end.Value - start.Value
                });
            }

            baseObj["SmartDim"] = smartDims;
            return baseObj;
        }

        private static void Log(string msg)
        {
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        }

        private static JsonNode? DeepClone(this JsonNode? node)
        {
            return node == null ? null : JsonNode.Parse(node.ToJsonString());
        }
    


            private static void AdjustCornerBases(JsonObject original, JsonObject result)
        {
            // Handle corner logic: [1,4] or others
            if (original.TryGetPropertyValue("Corner", out JsonNode? cornerNode) && cornerNode is JsonArray cornerArray)
            {
                foreach (var pair in cornerArray)
                {
                    if (pair is JsonArray cp && cp.Count == 2)
                    {
                        int a = cp[0]!.GetValue<int>();
                        int b = cp[1]!.GetValue<int>();

                        int wallX, wallY;
                        bool isCorner14 = (a == 1 && b == 4) || (a == 4 && b == 1);

                        if (isCorner14)
                        {
                            wallX = 4;
                            wallY = 1;
                        }
                        else
                        {
                            wallX = Math.Min(a, b);
                            wallY = Math.Max(a, b);
                        }

                        // Adjust Base1 of wallY (second wall)
                        if (result.TryGetPropertyValue($"Wall{wallY}", out JsonNode? wallYNode) &&
                            wallYNode is JsonObject wallYObj &&
                            wallYObj.TryGetPropertyValue("Bases", out JsonNode? basesNode) &&
                            basesNode is JsonObject basesY &&
                            basesY.TryGetPropertyValue("Base1", out JsonNode? base1Node) &&
                            base1Node is JsonObject base1Y)
                        {
                            Log($"🛠 Adjusting Base1 of Wall{wallY} to start from 60 (corner {wallX},{wallY})");
                            base1Y["Start"] = 60;
                        }
                    }
                }
            }

            // Handle exposed wall logic
            if (original.TryGetPropertyValue("NumOfExposedWall", out JsonNode? exposedNumNode) &&
                exposedNumNode is JsonValue numVal)
            {
                int exposedWall = numVal.GetValue<int>();

                if (exposedWall == 4)
                {
                    // Only update Wall1.Base1
                    if (result.TryGetPropertyValue("Wall1", out JsonNode? wall1Node) &&
                        wall1Node is JsonObject wall1Obj &&
                        wall1Obj.TryGetPropertyValue("Bases", out JsonNode? wall1BasesNode) &&
                        wall1BasesNode is JsonObject bases1 &&
                        bases1.TryGetPropertyValue("Base1", out JsonNode? base1NodeWall1) &&
                        base1NodeWall1 is JsonObject base1Wall1)
                    {
                        Log("🛠 Adjusting Base1 of Wall1 because ExposedWall == 4");
                        base1Wall1["Start"] = 60;
                    }
                }
                else
                {
                    // ExposedWall is 1, 2, or 3 → update matching base to first space
                    string wallKey = $"Wall{exposedWall}";
                    string spacesKey = $"SpacesWall{exposedWall}";

                    if (result.TryGetPropertyValue(wallKey, out JsonNode? wallNode) &&
                        wallNode is JsonObject wallObj &&
                        wallObj.TryGetPropertyValue(spacesKey, out JsonNode? spacesNode) &&
                        spacesNode is JsonArray spacesArray &&
                        spacesArray.FirstOrDefault() is JsonObject firstSpace &&
                        firstSpace.TryGetPropertyValue("Start", out JsonNode? startNode) &&
                        firstSpace.TryGetPropertyValue("End", out JsonNode? endNode) &&
                        wallObj.TryGetPropertyValue("Bases", out JsonNode? basesNode) &&
                        basesNode is JsonObject bases)
                    {
                        double spaceStart = startNode.GetValue<double>();
                        double spaceEnd = endNode.GetValue<double>();

                        foreach (var basePair in bases)
                        {
                            if (basePair.Value is JsonObject baseObj &&
                                baseObj.TryGetPropertyValue("Visible", out JsonNode? visibleNode) &&
                                visibleNode?.GetValue<bool>() == true &&
                                baseObj.TryGetPropertyValue("Start", out JsonNode? baseStartNode) &&
                                baseObj.TryGetPropertyValue("End", out JsonNode? baseEndNode))
                            {
                                double baseStart = baseStartNode.GetValue<double>();
                                double baseEnd = baseEndNode.GetValue<double>();

                                // Match first visible base to the first space range
                                if (Math.Abs(baseStart - spaceStart) < 1e-2 &&
                                    Math.Abs(baseEnd - spaceEnd) < 1e-2)
                                {
                                    Log($"🛠 Adjusting matched Base in {wallKey} to start from 60 (exposed wall)");
                                    baseObj["Start"] = 60;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }




    }
}
