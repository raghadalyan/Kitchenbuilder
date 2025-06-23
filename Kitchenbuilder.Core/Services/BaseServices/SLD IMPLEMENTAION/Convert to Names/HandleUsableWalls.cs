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
                    bool fridgeAssigned = false;
                    int regularBaseCounter = 1;

                    foreach (JsonNode node in spacesArray)
                    {
                        if (node is not JsonObject space) continue;
                        double spaceStart = space["Start"]!.GetValue<double>();
                        double spaceEnd = space["End"]!.GetValue<double>();

                        bool isFridgeInThisSpace = fridgeWall == wallNum && fridge != null &&
                            fridge["Start"]!.GetValue<double>() >= spaceStart &&
                            fridge["End"]!.GetValue<double>() <= spaceEnd;

                        if (isFridgeInThisSpace)
                        {
                            double fStart = fridge["Start"]!.GetValue<double>();
                            double fEnd = fridge["End"]!.GetValue<double>();

                            // Base1: before fridge
                            if (spaceStart < fStart)
                            {
                                bases["Base1"] = CreateSmartBase(wallNum, regularBaseCounter, spaceStart, fStart, true);
                                regularBaseCounter++;
                            }

                            // Base2: fridge
                            bases["Base2"] = CreateSmartBase(wallNum, -1, fStart, fEnd, true);
                            fridgeAssigned = true;

                            // Base3: after fridge
                            if (fEnd < spaceEnd)
                            {
                                bases["Base3"] = CreateSmartBase(wallNum, regularBaseCounter, fEnd, spaceEnd, true);
                                regularBaseCounter++;
                            }
                        }
                        else
                        {
                            if (!bases.ContainsKey("Base1"))
                            {
                                bases["Base1"] = CreateSmartBase(wallNum, regularBaseCounter, spaceStart, spaceEnd, true);
                                regularBaseCounter++;
                            }
                            else if (!bases.ContainsKey("Base2") && !fridgeAssigned)
                            {
                                bases["Base2"] = CreateSmartBase(wallNum, regularBaseCounter, spaceStart, spaceEnd, true);
                                regularBaseCounter++;
                            }
                            else if (!bases.ContainsKey("Base3"))
                            {
                                bases["Base3"] = CreateSmartBase(wallNum, regularBaseCounter, spaceStart, spaceEnd, true);
                                regularBaseCounter++;
                            }
                        }
                    }

                    // Ensure all Base1-3 exist
                    for (int i = 1; i <= 3; i++)
                    {
                        string baseKey = $"Base{i}";
                        if (!bases.ContainsKey(baseKey))
                            bases[baseKey] = CreateSmartBase(wallNum, i, null, null, false);
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

            JsonArray smartDims = new JsonArray();

            // Always set DistanceX
            smartDims.Add(new JsonObject
            {
                ["Name"] = $"DistanceX@{sketchName}",
                ["Size"] = start.HasValue ? JsonValue.Create(start.Value) : null
            });

            // Always set length if both Start and End are defined
            if (start.HasValue && end.HasValue)
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
            // Handle corner logic
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

                        if (result.TryGetPropertyValue($"Wall{wallY}", out JsonNode? wallYNode) &&
                            wallYNode is JsonObject wallYObj &&
                            wallYObj.TryGetPropertyValue("Bases", out JsonNode? basesNode) &&
                            basesNode is JsonObject basesY &&
                            basesY.TryGetPropertyValue("Base1", out JsonNode? base1Node) &&
                            base1Node is JsonObject base1Y)
                        {
                            Log($"🛠 Adjusting Base1 of Wall{wallY} to start from 60 (corner {wallX},{wallY})");
                            base1Y["Start"] = 60;

                            if (base1Y.TryGetPropertyValue("SmartDim", out JsonNode? smartDimsNode) &&
                                smartDimsNode is JsonArray smartDims)
                            {
                                foreach (var dimObj in smartDims)
                                {
                                    if (dimObj is JsonObject dim &&
                                        dim.TryGetPropertyValue("Name", out JsonNode? nameNode))
                                    {
                                        string dimName = nameNode.ToString();

                                        if (dimName.StartsWith("DistanceX@"))
                                        {
                                            dim["Size"] = 60;
                                        }
                                        else if (dimName.StartsWith("length@") &&
                                                 base1Y.TryGetPropertyValue("End", out JsonNode? endNode))
                                        {
                                            double end = endNode!.GetValue<double>();
                                            dim["Size"] = end - 60;
                                        }
                                    }
                                }
                            }
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
                    if (result.TryGetPropertyValue("Wall1", out JsonNode? wall1Node) &&
                        wall1Node is JsonObject wall1Obj &&
                        wall1Obj.TryGetPropertyValue("Bases", out JsonNode? wall1BasesNode) &&
                        wall1BasesNode is JsonObject bases1 &&
                        bases1.TryGetPropertyValue("Base1", out JsonNode? base1NodeWall1) &&
                        base1NodeWall1 is JsonObject base1Wall1)
                    {
                        Log("🛠 Adjusting Base1 of Wall1 because ExposedWall == 4");
                        base1Wall1["Start"] = 60;

                        if (base1Wall1.TryGetPropertyValue("SmartDim", out JsonNode? smartDimsNode) &&
                            smartDimsNode is JsonArray smartDims)
                        {
                            foreach (var dimObj in smartDims)
                            {
                                if (dimObj is JsonObject dim &&
                                    dim.TryGetPropertyValue("Name", out JsonNode? nameNode))
                                {
                                    string dimName = nameNode.ToString();

                                    if (dimName.StartsWith("DistanceX@"))
                                    {
                                        dim["Size"] = 60;
                                    }
                                    else if (dimName.StartsWith("length@") &&
                                             base1Wall1.TryGetPropertyValue("End", out JsonNode? endNode))
                                    {
                                        double end = endNode!.GetValue<double>();
                                        dim["Size"] = end - 60;
                                    }
                                }
                            }
                        }
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
                                visibleNode?.GetValue<bool>() == true)
                            {
                                baseObj["Start"] = 60;

                                if (baseObj.TryGetPropertyValue("SmartDim", out JsonNode? smartDimsNode) &&
                                    smartDimsNode is JsonArray smartDims)
                                {
                                    foreach (var dimObj in smartDims)
                                    {
                                        if (dimObj is JsonObject dim &&
                                            dim.TryGetPropertyValue("Name", out JsonNode? nameNode))
                                        {
                                            string dimName = nameNode.ToString();

                                            if (dimName.StartsWith("DistanceX@"))
                                            {
                                                dim["Size"] = 60;
                                            }
                                            else if (dimName.StartsWith("length@") &&
                                                     baseObj.TryGetPropertyValue("End", out JsonNode? localEndNode))
                                            {
                                                double end = localEndNode!.GetValue<double>();
                                                dim["Size"] = end - 60;
                                            }
                                        }
                                    }
                                }

                                break; // only update the first visible base
                            }
                        }
                    }
                }
            }
        }

    }




}
