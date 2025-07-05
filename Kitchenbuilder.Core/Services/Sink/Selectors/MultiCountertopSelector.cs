using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Kitchenbuilder.Models;

namespace Kitchenbuilder.Core
{
    /// <summary>
    /// Suggests up to 3 layout options when 3 or more countertops are available.
    /// It groups countertops by wall, selects the largest per wall, and tries different combinations for placing sink, cooktop, and fridge.
    /// Preference is given to placing the sink under a window if possible.
    /// If an island exists, one layout may involve placing either the sink or cooktop on the island, but total suggestions are capped at 3.
    /// </summary>
    public static class MultiCountertopSelector
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Sink-Cooktop\MultiCountertopSelector.txt";

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        public static void SuggestLayouts(List<Countertop> countertops, int optionNum)
        {
            Log($"🛠️ Processing {countertops.Count} countertops for option {optionNum}...");

            var grouped = countertops.GroupBy(c => c.WallNumber).ToDictionary(g => g.Key, g => g.ToList());
            int numWalls = grouped.Count;
            Log($"📊 Countertops are on {numWalls} wall(s).");

            // Load HasIsland flag
            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "Kitchenbuilder", "Kitchenbuilder", "JSON");
            string jsonPath = Path.Combine(basePath, $"Option{optionNum}SLD.json");

            bool hasIsland = false;
            if (File.Exists(jsonPath))
            {
                var json = JsonNode.Parse(File.ReadAllText(jsonPath))!.AsObject();
                hasIsland = json["HasIsland"]?.ToString()?.ToLower() == "true";
                Log($"🔍 HasIsland = {hasIsland}");
            }

            int suggestionCount = 0;

            if (numWalls == 2)
            {
                var wallList = grouped.Keys.ToList();
                var ct1 = grouped[wallList[0]].OrderByDescending(c => c.End - c.Start).First();
                var ct2 = grouped[wallList[1]].OrderByDescending(c => c.End - c.Start).First();

                bool win1 = WindowRangeChecker.IsWindowInRange(ct1.Start, ct1.End, ct1.WallNumber);
                bool win2 = WindowRangeChecker.IsWindowInRange(ct2.Start, ct2.End, ct2.WallNumber);

                if (suggestionCount < 3 && win1)
                {
                    Log($"✅ Suggestion {++suggestionCount}: Sink under window on Wall{ct1.WallNumber}, cooktop on Wall{ct2.WallNumber}.");
                }

                if (suggestionCount < 3 && win2)
                {
                    Log($"✅ Suggestion {++suggestionCount}: Sink under window on Wall{ct2.WallNumber}, cooktop on Wall{ct1.WallNumber}.");
                }

                if (suggestionCount < 3 && !win1 && !win2)
                {
                    Log($"✅ Suggestion {++suggestionCount}: Sink on Wall{ct1.WallNumber}, cooktop on Wall{ct2.WallNumber}.");
                }

                if (hasIsland && suggestionCount < 3)
                {
                    Log($"✅ Suggestion {++suggestionCount}: Sink on Wall{ct1.WallNumber}, cooktop on island.");
                }
            }
            else if (numWalls > 2)
            {
                var largestPerWall = grouped.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OrderByDescending(c => c.End - c.Start).First());
                var wallIds = largestPerWall.Keys.ToList();

                // All 3-wall combinations
                foreach (var fridgeWall in wallIds)
                {
                    foreach (var sinkWall in wallIds.Where(w => w != fridgeWall))
                    {
                        foreach (var cooktopWall in wallIds.Where(w => w != fridgeWall && w != sinkWall))
                        {
                            if (suggestionCount >= 3)
                                break;

                            var sinkCt = largestPerWall[sinkWall];
                            bool winSink = WindowRangeChecker.IsWindowInRange(sinkCt.Start, sinkCt.End, sinkWall);

                            if (winSink)
                            {
                                Log($"✅ Suggestion {++suggestionCount}: Fridge on Wall{fridgeWall}, Sink under window on Wall{sinkWall}, Cooktop on Wall{cooktopWall}.");
                            }
                            else
                            {
                                Log($"✅ Suggestion {++suggestionCount}: Fridge on Wall{fridgeWall}, Sink on Wall{sinkWall}, Cooktop on Wall{cooktopWall}.");
                            }

                            if (hasIsland && suggestionCount < 3)
                            {
                                string useIsland = new Random().Next(2) == 0 ? "Sink" : "Cooktop";
                                int otherWall = useIsland == "Sink" ? cooktopWall : sinkWall;
                                Log($"✅ Suggestion {++suggestionCount}: Fridge on Wall{fridgeWall}, {useIsland} on island, other on Wall{otherWall}.");
                            }

                            if (suggestionCount >= 3)
                                break;
                        }
                        if (suggestionCount >= 3)
                            break;
                    }
                    if (suggestionCount >= 3)
                        break;
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
