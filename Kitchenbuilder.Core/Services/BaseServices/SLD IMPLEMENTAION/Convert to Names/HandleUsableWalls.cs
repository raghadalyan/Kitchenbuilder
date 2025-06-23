
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

                            // Base1: Before fridge
                            if (spaceStart < fStart)
                            {
                                bases["Base1"] = CreateSmartBase(
                                    $"{wallNum}_1",
                                    $"Extrude_{wallNum}_1",
                                    spaceStart,
                                    fStart,
                                    true);
                            }

                            // Base3: Fridge
                            bases["Base3"] = CreateSmartBase(
                                $"fridge_base{wallNum}",
                                $"Extrude_fridge_base{wallNum}",
                                fStart,
                                fEnd,
                                true);

                            // Base2: After fridge
                            if (fEnd < spaceEnd)
                            {
                                bases["Base2"] = CreateSmartBase(
                                    $"{wallNum}_2",
                                    $"Extrude_{wallNum}_2",
                                    fEnd,
                                    spaceEnd,
                                    true);
                            }
                        }
                        else
                        {
                            // No fridge in this space → fill regular Base1 and Base2
                            if (!bases.ContainsKey("Base1"))
                            {
                                bases["Base1"] = CreateSmartBase(
                                    $"{wallNum}_1",
                                    $"Extrude_{wallNum}_1",
                                    spaceStart,
                                    spaceEnd,
                                    true);
                            }
                            else if (!bases.ContainsKey("Base2"))
                            {
                                bases["Base2"] = CreateSmartBase(
                                    $"{wallNum}_2",
                                    $"Extrude_{wallNum}_2",
                                    spaceStart,
                                    spaceEnd,
                                    true);
                            }
                        }
                    }

                    // Ensure all 3 bases exist
                    if (!bases.ContainsKey("Base1"))
                        bases["Base1"] = CreateSmartBase($"{wallNum}_1", $"Extrude_{wallNum}_1", null, null, false);

                    if (!bases.ContainsKey("Base2"))
                        bases["Base2"] = CreateSmartBase($"{wallNum}_2", $"Extrude_{wallNum}_2", null, null, false);

                    if (!bases.ContainsKey("Base3"))
                        bases["Base3"] = CreateSmartBase($"fridge_base{wallNum}", $"Extrude_fridge_base{wallNum}", null, null, false);

                    wallObj["Bases"] = bases;
                    wallObj["Exposed"] = original["NumOfExposedWall"]?.GetValue<int>() == wallNum;

                    if (fridgeWall == wallNum && fridge != null)
                        wallObj["Fridge"] = fridge.DeepClone();

                    result[$"Wall{wallNum}"] = wallObj;
                }

                AdjustCornerBases(original, result);
                AddExposedSmartDimension(original, result); // ✅ Add this line


                File.WriteAllText(outputPath, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                Log($"✅ Output written to: {Path.GetFileName(outputPath)}");
            }
            catch (Exception ex)
            {
                Log($"❌ Error: {ex.Message}");
            }

        }



        private static JsonObject CreateSmartBase(string sketchName, string extrudeName, double? start, double? end, bool visible)
        {
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

            // Always add DistanceX dimension if start is defined
            if (start.HasValue)
            {
                smartDims.Add(new JsonObject
                {
                    ["Name"] = $"DistanceX@{sketchName}",
                    ["Size"] = start.Value
                });
            }

            // Add length dimension if both start and end are defined
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


        private static void AddExposedSmartDimension(JsonObject original, JsonObject result)
        {
            if (!original.TryGetPropertyValue("NumOfExposedWall", out JsonNode? node) || node is not JsonValue exposedVal)
            {
                Log("❌ NumOfExposedWall missing or invalid");
                return;
            }

            int exposed = exposedVal.GetValue<int>();
            string exposedWallKey = $"Wall{exposed}";
            string dimName = exposed switch
            {
                2 => "Distance_from_2@master_wall2",
                3 => "Distance_from_1@master_wall3",
                4 => "Distance_from_2@master_wall4",
                _ => null
            };

            if (dimName == null)
            {
                Log($"❌ Unknown exposed wall number: {exposed}");
                return;
            }

            if (!result.TryGetPropertyValue(exposedWallKey, out JsonNode? wallNode) || wallNode is not JsonObject wallObj)
            {
                Log($"❌ Wall '{exposedWallKey}' not found in result JSON");
                return;
            }

            if (!result.TryGetPropertyValue("Floor", out JsonNode? floorNode) || floorNode is not JsonObject floor)
            {
                Log("❌ Floor section missing or invalid in result JSON");
                return;
            }

            bool TryGetFloorSize(string key, out double value)
            {
                value = 0;
                if (!floor.TryGetPropertyValue(key, out JsonNode? node)) return false;
                if (node is JsonObject obj && obj.TryGetPropertyValue("Size", out JsonNode? sizeNode) && sizeNode is JsonValue val)
                {
                    value = val.GetValue<double>();
                    return true;
                }
                return false;
            }

            double? size = null;

            // Wall 2 or 3
            if (exposed == 2 || exposed == 3)
            {
                int prevWall = exposed - 1;
                if (original.TryGetPropertyValue($"SpacesWall{prevWall}", out JsonNode? prevSpacesNode) &&
                    prevSpacesNode is JsonArray prevSpaces)
                {
                    Log($"🔍 Checking previous wall {prevWall} for exposed wall {exposed}");

                    foreach (var space in prevSpaces.OrderByDescending(s => s["End"]?.GetValue<double>() ?? 0))
                    {
                        if (space is JsonObject obj &&
                            obj.TryGetPropertyValue("Start", out JsonNode? startNode) &&
                            obj.TryGetPropertyValue("End", out JsonNode? endNode))
                        {
                            double start = startNode.GetValue<double>();
                            double end = endNode.GetValue<double>();
                            double length = end - start;

                            Log($"   ➤ Space found: Start={start}, End={end}, Length={length}");

                            if (length >= 60)
                            {
                                if (exposed == 2 && TryGetFloorSize("Width", out double floorWidth))
                                {
                                    size = floorWidth - end;
                                    Log($"✅ Wall 2 exposed. Size = {floorWidth} - {end} = {size}");
                                }
                                else if (exposed == 3)
                                {
                                    size = end;
                                    Log($"✅ Wall 3 exposed. Using space end directly: Size = {size}");
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Log($"❌ SpacesWall{prevWall} not found");
                }
            }

            // Wall 4
            else if (exposed == 4 &&
                     result.TryGetPropertyValue("Wall1", out JsonNode? wall1Node) &&
                     wall1Node is JsonObject wall1Obj &&
                     wall1Obj.TryGetPropertyValue("SpacesWall1", out JsonNode? spacesNode) &&
                     spacesNode is JsonArray spaces &&
                     TryGetFloorSize("Width", out double floorWidth))
            {
                Log("🔍 Checking Wall1 spaces for exposed wall 4");

                foreach (var space in spaces.OrderBy(s => s["Start"]?.GetValue<double>() ?? 0))
                {
                    if (space is JsonObject obj &&
                        obj.TryGetPropertyValue("Start", out JsonNode? startNode) &&
                        obj.TryGetPropertyValue("End", out JsonNode? endNode))
                    {
                        double start = startNode.GetValue<double>();
                        double end = endNode.GetValue<double>();
                        double length = end - start;

                        Log($"   ➤ Space found: Start={start}, End={end}, Length={length}");

                        if (length >= 60)
                        {
                            size = floorWidth - start;
                            Log($"✅ Wall 4 exposed. Size = {floorWidth} - {start} = {size}");
                            break;
                        }
                    }
                }
            }

            if (size.HasValue)
            {
                wallObj["DistanceFrom"] = new JsonArray
        {
            new JsonObject
            {
                ["Name"] = dimName,
                ["Size"] = size.Value
            }
        };
                Log($"📏 Added DistanceFrom to {exposedWallKey}: '{dimName}' = {size.Value}");
            }
            else
            {
                Log($"⚠️ No suitable space found to calculate DistanceFrom for {exposedWallKey}");
            }
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


