using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Kitchenbuilder.Models;
using SolidWorks.Interop.sldworks;
using Kitchenbuilder.Core.Models; // for Island

namespace Kitchenbuilder.Core
{
    public static class MultiCountertopSelector
    {
        private static readonly string DebugPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Output", "Sink-Cooktop", "MultiCountertopSelector.txt"
        );

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}");
        }

        private static int GetBaseNumber(string baseKey)
        {
            return baseKey.StartsWith("Base") && int.TryParse(baseKey.Substring(4), out int num) ? num : 0;
        }

        public static void SuggestLayouts(List<Countertop> countertops, int optionNum, IModelDoc2 model)
        {
            Log($"🛠️ Processing {countertops.Count} countertops for option {optionNum}...");

            var grouped = countertops.GroupBy(c => c.WallNumber).ToDictionary(g => g.Key, g => g.ToList());
            int numWalls = grouped.Count;
            Log($"📊 Countertops are on {numWalls} wall(s).");

            string basePath = Path.Combine(
                KitchenConfig.Get().BasePath,
                "Kitchenbuilder", "Kitchenbuilder", "JSON"
            );

            string jsonPath = Path.Combine(basePath, $"Option{optionNum}SLD.json");

            JsonObject? root = File.Exists(jsonPath)
                ? JsonNode.Parse(File.ReadAllText(jsonPath))!.AsObject()
                : null;

            if (root == null)
            {
                Log("❌ Option JSON file not found.");
                return;
            }

            bool hasIsland = root["HasIsland"]?.ToString()?.ToLower() == "true";
            double floorWidth = root["Floor"]?["Width"]?["Size"]?.GetValue<double>() ?? 0;
            double floorLength = root["Floor"]?["Length"]?["Size"]?.GetValue<double>() ?? 0;
            Island? island = null;
            if (hasIsland)
            {
                island = new Island
                {
                    Direction = root["Island"]?["Direction"]?.GetValue<double>() ?? 90,
                    DistanceX = root["Island"]?["DistanceX"]?.GetValue<double>() ?? 0,
                    DistanceY = root["Island"]?["DistanceY"]?.GetValue<double>() ?? 0,
                    Depth = (int)(root["Island"]?["Depth"]?.GetValue<double>() ?? 90),
                    Width = (int)(root["Island"]?["Width"]?.GetValue<double>() ?? 180),
                    Material = root["Island"]?["Material"]?.ToString() ?? ""
                };
            }

            int fridgeWallNumber = -1;
            foreach (var wallEntry in root)
            {
                if (wallEntry.Value is JsonObject wallObj && wallObj.TryGetPropertyValue("Fridge", out JsonNode? fridgeNode))
                {
                    if (fridgeNode?["Start"] != null && fridgeNode["End"] != null &&
                        wallEntry.Key.StartsWith("Wall") && int.TryParse(wallEntry.Key.Substring(4), out int num))
                    {
                        fridgeWallNumber = num;
                        break;
                    }
                }
            }

            if (fridgeWallNumber == -1)
            {
                Log("❌ Could not find fridge wall in JSON.");
                return;
            }

            Log($"🧊 Fridge wall identified: Wall{fridgeWallNumber}");

            int suggestionCount = 0;
            int layoutFolderIndex = 1;
            
            if (numWalls == 2)
            {
                var wallList = grouped.Keys.ToList();
                var ct1 = grouped[wallList[0]].OrderByDescending(c => c.Width).First();
                var ct2 = grouped[wallList[1]].OrderByDescending(c => c.Width).First();

                int base1 = GetBaseNumber(ct1.BaseKey);
                int base2 = GetBaseNumber(ct2.BaseKey);

                bool win1 = WindowRangeChecker.IsWindowInRange(ct1.Start, ct1.End, ct1.WallNumber);
                bool win2 = WindowRangeChecker.IsWindowInRange(ct2.Start, ct2.End, ct2.WallNumber);

                if (suggestionCount < 3 && win1)
                {
                    SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow("sink", ct1.WallNumber, base1, ct1.Start, 0, 60, 60, floorWidth, floorLength, ct1.Start, ct1.End, model);
                    SinkCooktopMiddle.CreateCooktopInMiddle(ct2.WallNumber, base2, optionNum, model);
                    SaveSinkCooktopImage.Save(model, layoutFolderIndex++, $"sink_win_wall{ct1.WallNumber}_cooktop_middle_wall{ct2.WallNumber}", optionNum);
                    suggestionCount++;
                }

                if (suggestionCount < 3 && win2)
                {
                    SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow("sink", ct2.WallNumber, base2, ct2.Start, 0, 60, 60, floorWidth, floorLength, ct2.Start, ct2.End, model);
                    SinkCooktopMiddle.CreateCooktopInMiddle(ct1.WallNumber, base1, optionNum, model);
                    SaveSinkCooktopImage.Save(model, layoutFolderIndex++, $"sink_win_wall{ct2.WallNumber}_cooktop_middle_wall{ct1.WallNumber}", optionNum);
                    suggestionCount++;
                }

                if (suggestionCount < 3 && !win1 && !win2)
                {
                    SinkCooktopMiddle.CreateSinkInMiddle(ct1.WallNumber, base1, optionNum, model);
                    SinkCooktopMiddle.CreateCooktopInMiddle(ct2.WallNumber, base2, optionNum, model);
                    SaveSinkCooktopImage.Save(model, layoutFolderIndex++, $"sink_middle_wall{ct1.WallNumber}_cooktop_middle_wall{ct2.WallNumber}", optionNum);
                    suggestionCount++;
                }

                if (hasIsland && suggestionCount < 3 && island != null)
                {
                    Log("🌴 Suggesting island layout for 2-wall scenario...");
                    bool sinkOnIsland = new Random().Next(2) == 0;

                    if (sinkOnIsland)
                    {
                        SinkCooktopOnIsland.CreateSinkOnIsland(ct1.WallNumber, island, model);
                        SinkCooktopMiddle.CreateCooktopInMiddle(ct2.WallNumber, base2, optionNum, model);
                        SaveSinkCooktopImage.Save(model, layoutFolderIndex++, $"sink_island_cooktop_wall{ct2.WallNumber}", optionNum);
                    }
                    else
                    {
                        SinkCooktopMiddle.CreateSinkInMiddle(ct1.WallNumber, base1, optionNum, model);
                        SinkCooktopOnIsland.CreateCooktopOnIsland(ct2.WallNumber, island, model);
                        SaveSinkCooktopImage.Save(model, layoutFolderIndex++, $"sink_wall{ct1.WallNumber}_cooktop_island", optionNum);
                    }

                    suggestionCount++;
                }

            }
            else if (numWalls > 2)
            {
                var largestPerWall = grouped.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OrderByDescending(c => c.Width).First());
                var wallIds = largestPerWall.Keys.ToList();

                foreach (var sinkWall in wallIds.Where(w => w != fridgeWallNumber))
                {
                    foreach (var cooktopWall in wallIds.Where(w => w != fridgeWallNumber && w != sinkWall))
                    {
                        if (suggestionCount >= 3) break;

                        var sinkCt = largestPerWall[sinkWall];
                        var cookCt = largestPerWall[cooktopWall];
                        int sinkBase = GetBaseNumber(sinkCt.BaseKey);
                        int cookBase = GetBaseNumber(cookCt.BaseKey);

                        bool winSink = WindowRangeChecker.IsWindowInRange(sinkCt.Start, sinkCt.End, sinkWall);

                        if (winSink)
                        {
                            SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow("sink", sinkWall, sinkBase, sinkCt.Start, 0, 60, 60, floorWidth, floorLength, sinkCt.Start, sinkCt.End, model);
                            SinkCooktopMiddle.CreateCooktopInMiddle(cooktopWall, cookBase, optionNum, model);
                            SaveSinkCooktopImage.Save(model, layoutFolderIndex++, $"sink_win_wall{sinkWall}_cooktop_middle_wall{cooktopWall}", optionNum);
                        }
                        else
                        {
                            SinkCooktopMiddle.CreateSinkInMiddle(sinkWall, sinkBase, optionNum, model);
                            SinkCooktopMiddle.CreateCooktopInMiddle(cooktopWall, cookBase, optionNum, model);
                            SaveSinkCooktopImage.Save(model, layoutFolderIndex++, $"sink_middle_wall{sinkWall}_cooktop_middle_wall{cooktopWall}", optionNum);
                        }

                        suggestionCount++;
                        if (hasIsland && suggestionCount < 3 && island != null)
                        {
                            Log("🌴 Suggesting island layout for multi-wall scenario...");
                            string useIsland = new Random().Next(2) == 0 ? "Sink" : "Cooktop";
                            int otherWall = useIsland == "Sink" ? cooktopWall : sinkWall;
                            var otherCt = largestPerWall[otherWall];
                            int otherBase = GetBaseNumber(otherCt.BaseKey);

                            if (useIsland == "Sink")
                            {
                                SinkCooktopOnIsland.CreateSinkOnIsland(sinkWall, island, model);
                                SinkCooktopMiddle.CreateCooktopInMiddle(otherWall, otherBase, optionNum, model);
                                SaveSinkCooktopImage.Save(model, layoutFolderIndex++, $"sink_island_cooktop_wall{otherWall}", optionNum);
                            }
                            else
                            {
                                SinkCooktopMiddle.CreateSinkInMiddle(otherWall, otherBase, optionNum, model);
                                SinkCooktopOnIsland.CreateCooktopOnIsland(cooktopWall, island, model);
                                SaveSinkCooktopImage.Save(model, layoutFolderIndex++, $"sink_wall{otherWall}_cooktop_island", optionNum);
                            }

                            suggestionCount++;
                        }


                        if (suggestionCount >= 3) break;
                    }

                    if (suggestionCount >= 3) break;
                }

                if (suggestionCount == 0)
                    Log("⚠️ No valid 3-wall layout found.");
            }
            else
            {
                Log("❌ Unexpected case: countertops are not spread on enough walls.");
            }
        }
    }
}
