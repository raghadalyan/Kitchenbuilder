using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Kitchenbuilder.Models;

namespace Kitchenbuilder.Core
{
    public static class MultiCountertopSelector
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Sink-Cooktop\MultiCountertopSelector.txt";

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        private static int GetBaseNumber(string baseKey)
        {
            if (baseKey.StartsWith("Base") && int.TryParse(baseKey.Substring(4), out int num))
                return num;
            return 0;
        }

        public static void SuggestLayouts(List<Countertop> countertops, int optionNum)
        {
            Log($"🛠️ Processing {countertops.Count} countertops for option {optionNum}...");

            var grouped = countertops.GroupBy(c => c.WallNumber).ToDictionary(g => g.Key, g => g.ToList());
            int numWalls = grouped.Count;
            Log($"📊 Countertops are on {numWalls} wall(s).");

            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "Kitchenbuilder", "Kitchenbuilder", "JSON");
            string jsonPath = Path.Combine(basePath, $"Option{optionNum}SLD.json");

            JsonObject? root = null;
            bool hasIsland = false;
            if (File.Exists(jsonPath))
            {
                root = JsonNode.Parse(File.ReadAllText(jsonPath))!.AsObject();
                hasIsland = root["HasIsland"]?.ToString()?.ToLower() == "true";
                Log($"🔍 HasIsland = {hasIsland}");
            }

            double floorWidth = root?["Floor"]?["Width"]?["Size"]?.GetValue<double>() ?? 0;
            double floorLength = root?["Floor"]?["Length"]?["Size"]?.GetValue<double>() ?? 0;

            // Identify fridge wall from JSON
            int fridgeWallNumber = -1;
            foreach (var wallEntry in root!)
            {
                if (wallEntry.Value is JsonObject wallObj && wallObj.TryGetPropertyValue("Fridge", out JsonNode? fridgeNode))
                {
                    if (fridgeNode?["Start"] != null && fridgeNode["End"] != null)
                    {
                        string wallKey = wallEntry.Key;
                        if (wallKey.StartsWith("Wall") && int.TryParse(wallKey.Substring(4), out int num))
                        {
                            fridgeWallNumber = num;
                            break;
                        }
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

            if (numWalls == 2)
            {
                var wallList = grouped.Keys.ToList();
                var ct1 = grouped[wallList[0]].OrderByDescending(c => c.Width).First();
                var ct2 = grouped[wallList[1]].OrderByDescending(c => c.Width).First();

                int base1 = GetBaseNumber(ct1.BaseKey);
                int base2 = GetBaseNumber(ct2.BaseKey);

                bool win1 = WindowRangeChecker.IsWindowInRange(ct1.Start, ct1.End, ct1.WallNumber);
                bool win2 = WindowRangeChecker.IsWindowInRange(ct2.Start, ct2.End, ct2.WallNumber);

                Log($"🔎 Win check - Wall{ct1.WallNumber}: {win1}, Wall{ct2.WallNumber}: {win2}");

                if (suggestionCount < 3 && win1)
                {
                    Log($"🪟 Calling sink under window on Wall{ct1.WallNumber}, base {base1}");
                    Log($"🔧 SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow('sink', Wall{ct1.WallNumber}, Base{base1})");
                    SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow("sink", ct1.WallNumber, base1, ct1.Start, 0, 60, 60, floorWidth, floorLength, ct1.Start, ct1.End);

                    Log($"🔥 Cooktop in middle on Wall{ct2.WallNumber}, base {base2}");
                    Log($"🔧 SinkCooktopMiddle.CreateCooktopInMiddle(Wall{ct2.WallNumber}, Base{base2})");
                    SinkCooktopMiddle.CreateCooktopInMiddle(ct2.WallNumber, base2, optionNum);

                    Log($"✅ Suggestion {++suggestionCount} complete.");
                }

                if (suggestionCount < 3 && win2)
                {
                    Log($"🪟 Calling sink under window on Wall{ct2.WallNumber}, base {base2}");
                    Log($"🔧 SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow('sink', Wall{ct2.WallNumber}, Base{base2})");
                    SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow("sink", ct2.WallNumber, base2, ct2.Start, 0, 60, 60, floorWidth, floorLength, ct2.Start, ct2.End);

                    Log($"🔥 Cooktop in middle on Wall{ct1.WallNumber}, base {base1}");
                    Log($"🔧 SinkCooktopMiddle.CreateCooktopInMiddle(Wall{ct1.WallNumber}, Base{base1})");
                    SinkCooktopMiddle.CreateCooktopInMiddle(ct1.WallNumber, base1, optionNum);

                    Log($"✅ Suggestion {++suggestionCount} complete.");
                }

                if (suggestionCount < 3 && !win1 && !win2)
                {
                    Log($"🚿 Sink in middle on Wall{ct1.WallNumber}, base {base1}");
                    Log($"🔧 SinkCooktopMiddle.CreateSinkInMiddle(Wall{ct1.WallNumber}, Base{base1})");
                    SinkCooktopMiddle.CreateSinkInMiddle(ct1.WallNumber, base1, optionNum);

                    Log($"🔥 Cooktop in middle on Wall{ct2.WallNumber}, base {base2}");
                    Log($"🔧 SinkCooktopMiddle.CreateCooktopInMiddle(Wall{ct2.WallNumber}, Base{base2})");
                    SinkCooktopMiddle.CreateCooktopInMiddle(ct2.WallNumber, base2, optionNum);

                    Log($"✅ Suggestion {++suggestionCount} complete.");
                }

                if (hasIsland && suggestionCount < 3)
                {
                    Log($"🚿 Sink in middle on Wall{ct1.WallNumber}, base {base1}, cooktop on island");
                    Log($"🔧 SinkCooktopMiddle.CreateSinkInMiddle(Wall{ct1.WallNumber}, Base{base1})");
                    SinkCooktopMiddle.CreateSinkInMiddle(ct1.WallNumber, base1, optionNum);

                    Log("🧊 Cooktop assumed to be on island (not created in SW).");
                    Log($"✅ Suggestion {++suggestionCount} complete.");
                }
            }
            else if (numWalls > 2)
            {
                var largestPerWall = grouped.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OrderByDescending(c => c.Width).First());
                var wallIds = largestPerWall.Keys.ToList();

                Log($"💡 Using only fridge wall Wall{fridgeWallNumber} as fixed reference.");

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
                        Log($"🔍 Testing combo: SinkWall {sinkWall}, CooktopWall {cooktopWall}, Window={winSink}");

                        if (winSink)
                        {
                            Log($"🪟 Sink under window on Wall{sinkWall}, base {sinkBase}");
                            Log($"🔧 SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow('sink', Wall{sinkWall}, Base{sinkBase})");
                            SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow("sink", sinkWall, sinkBase, sinkCt.Start, 0, 60, 60, floorWidth, floorLength, sinkCt.Start, sinkCt.End);

                            Log($"🔥 Cooktop in middle on Wall{cooktopWall}, base {cookBase}");
                            Log($"🔧 SinkCooktopMiddle.CreateCooktopInMiddle(Wall{cooktopWall}, Base{cookBase})");
                            SinkCooktopMiddle.CreateCooktopInMiddle(cooktopWall, cookBase, optionNum);
                        }
                        else
                        {
                            Log($"🚿 Sink in middle on Wall{sinkWall}, base {sinkBase}");
                            Log($"🔧 SinkCooktopMiddle.CreateSinkInMiddle(Wall{sinkWall}, Base{sinkBase})");
                            SinkCooktopMiddle.CreateSinkInMiddle(sinkWall, sinkBase, optionNum);

                            Log($"🔥 Cooktop in middle on Wall{cooktopWall}, base {cookBase}");
                            Log($"🔧 SinkCooktopMiddle.CreateCooktopInMiddle(Wall{cooktopWall}, Base{cookBase})");
                            SinkCooktopMiddle.CreateCooktopInMiddle(cooktopWall, cookBase, optionNum);
                        }

                        Log($"✅ Suggestion {++suggestionCount} complete.");

                        if (hasIsland && suggestionCount < 3)
                        {
                            string useIsland = new Random().Next(2) == 0 ? "Sink" : "Cooktop";
                            int otherWall = useIsland == "Sink" ? cooktopWall : sinkWall;
                            var otherCt = largestPerWall[otherWall];
                            int otherBase = GetBaseNumber(otherCt.BaseKey);

                            Log($"🧊 {useIsland} on island, calling function for {(useIsland == "Sink" ? "cooktop" : "sink")} on Wall{otherWall}, base {otherBase}");

                            if (useIsland == "Sink")
                            {
                                Log($"🔧 SinkCooktopMiddle.CreateCooktopInMiddle(Wall{otherWall}, Base{otherBase})");
                                SinkCooktopMiddle.CreateCooktopInMiddle(otherWall, otherBase, optionNum);
                            }
                            else
                            {
                                Log($"🔧 SinkCooktopMiddle.CreateSinkInMiddle(Wall{otherWall}, Base{otherBase})");
                                SinkCooktopMiddle.CreateSinkInMiddle(otherWall, otherBase, optionNum);
                            }

                            Log($"✅ Suggestion {++suggestionCount} complete.");
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
