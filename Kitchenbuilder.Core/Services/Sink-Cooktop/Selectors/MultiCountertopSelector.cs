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

            string basePath = Path.Combine(KitchenConfig.Get().BasePath, "Kitchenbuilder", "Kitchenbuilder", "JSON");
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
                    var sink = SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow("sink", ct1.WallNumber, base1, ct1.Start, 0, 60, 60, floorWidth, floorLength, ct1.Start, ct1.End, model) as Sink;
                    var cooktop = SinkCooktopMiddle.CreateCooktopInMiddle(ct2.WallNumber, base2, optionNum, model);
                    if (sink != null && cooktop != null)
                        SaveSinkCooktopImage.Save(model, layoutFolderIndex++, $"sink_win_wall{ct1.WallNumber}_cooktop_middle_wall{ct2.WallNumber}", optionNum, sink, cooktop);
                    suggestionCount++;
                }

                if (suggestionCount < 3 && win2)
                {
                    var sink = SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow("sink", ct2.WallNumber, base2, ct2.Start, 0, 60, 60, floorWidth, floorLength, ct2.Start, ct2.End, model) as Sink;
                    var cooktop = SinkCooktopMiddle.CreateCooktopInMiddle(ct1.WallNumber, base1, optionNum, model);
                    if (sink != null && cooktop != null)
                        SaveSinkCooktopImage.Save(model, layoutFolderIndex++, $"sink_win_wall{ct2.WallNumber}_cooktop_middle_wall{ct1.WallNumber}", optionNum, sink, cooktop);
                    suggestionCount++;
                }

                if (suggestionCount < 3 && !win1 && !win2)
                {
                    var sink = SinkCooktopMiddle.CreateSinkInMiddle(ct1.WallNumber, base1, optionNum, model);
                    var cooktop = SinkCooktopMiddle.CreateCooktopInMiddle(ct2.WallNumber, base2, optionNum, model);
                    if (sink != null && cooktop != null)
                        SaveSinkCooktopImage.Save(model, layoutFolderIndex++, $"sink_middle_wall{ct1.WallNumber}_cooktop_middle_wall{ct2.WallNumber}", optionNum, sink, cooktop);
                    suggestionCount++;
                }

                if (hasIsland && suggestionCount < 3 && island != null)
                {
                    Log("🌴 Suggesting island layout for 2-wall scenario...");
                    bool sinkOnIsland = new Random().Next(2) == 0;

                    if (sinkOnIsland)
                    {
                        var sink = SinkCooktopOnIsland.CreateSinkOnIsland(ct1.WallNumber, island, model);
                        var cooktop = SinkCooktopMiddle.CreateCooktopInMiddle(ct2.WallNumber, base2, optionNum, model);
                        if (sink != null && cooktop != null)
                            SaveSinkCooktopImage.Save(model, layoutFolderIndex++, $"sink_island_cooktop_wall{ct2.WallNumber}", optionNum, sink, cooktop);
                    }
                    else
                    {
                        var sink = SinkCooktopMiddle.CreateSinkInMiddle(ct1.WallNumber, base1, optionNum, model);
                        var cooktop = SinkCooktopOnIsland.CreateCooktopOnIsland(ct2.WallNumber, island, model);
                        if (sink != null && cooktop != null)
                            SaveSinkCooktopImage.Save(model, layoutFolderIndex++, $"sink_wall{ct1.WallNumber}_cooktop_island", optionNum, sink, cooktop);
                    }

                    suggestionCount++;
                }
            }
            else
            {
                Log("❌ Unexpected case: countertops are not spread on exactly 2 walls.");
            }
        }
    }
}
