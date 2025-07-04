using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using Kitchenbuilder.Models;

namespace Kitchenbuilder.Core
{
    /// <summary>
    /// This class handles layout suggestions when two countertops are available.
    /// It analyzes window positions on each countertop and determines the best placement
    /// for the sink and cooktop. The logic considers whether there is an island, and:
    /// - If exactly one window is in range: place the sink centered under that window.
    /// - If multiple windows are in range: place the sink in the center of the countertop.
    /// - If no windows are in range: randomly assign sink/cooktop or use the island.
    /// </summary>
    public static class TwoCountertopSelector
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Sink-Cooktop\TwoCountertopSelector.txt";

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        public static void SuggestLayouts(List<Countertop> countertops, int optionNum)
        {
            if (countertops.Count != 2)
            {
                Log("❌ Expected exactly 2 countertops.");
                return;
            }

            var ct1 = countertops[0];
            var ct2 = countertops[1];

            Log($"🛠️ Two countertops found: Wall{ct1.WallNumber} {ct1.BaseKey} and Wall{ct2.WallNumber} {ct2.BaseKey}");

            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "Kitchenbuilder",
                "Kitchenbuilder",
                "JSON"
            );

            string jsonPath = Path.Combine(basePath, $"Option{optionNum}SLD.json");

            bool hasIsland = false;
            if (File.Exists(jsonPath))
            {
                var json = JsonNode.Parse(File.ReadAllText(jsonPath))!.AsObject();
                string islandVal = json["HasIsland"]?.ToString()?.ToLower() ?? "false";
                hasIsland = islandVal == "true";
                Log($"🔍 HasIsland = {hasIsland}");
            }
            else
            {
                Log("❌ Option JSON not found.");
            }

            // Check window range and count
            bool winInRange1 = WindowRangeChecker.IsWindowInRange(ct1.Start, ct1.End, ct1.WallNumber);
            bool winInRange2 = WindowRangeChecker.IsWindowInRange(ct2.Start, ct2.End, ct2.WallNumber);

            int winCount1 = winInRange1 ? WindowRangeChecker.CountWindowsInRange(ct1.Start, ct1.End, ct1.WallNumber) : 0;
            int winCount2 = winInRange2 ? WindowRangeChecker.CountWindowsInRange(ct2.Start, ct2.End, ct2.WallNumber) : 0;

            // Decision logic
            if (winInRange1)
            {
                if (winCount1 == 1)
                    Log("✅ Suggestion 1: Sink centered under the window on first countertop, cooktop on second.");
                else
                    Log("✅ Suggestion 1: Sink centered on first countertop (multiple windows), cooktop on second.");

                if (hasIsland)
                    Log("✅ Suggestion 2 (Island): Sink on first countertop, cooktop on island.");
            }
            else if (winInRange2)
            {
                if (winCount2 == 1)
                    Log("✅ Suggestion 1: Sink centered under the window on second countertop, cooktop on first.");
                else
                    Log("✅ Suggestion 1: Sink centered on second countertop (multiple windows), cooktop on first.");

                if (hasIsland)
                    Log("✅ Suggestion 2 (Island): Sink on second countertop, cooktop on island.");
            }
            else
            {
                // No windows in range
                bool sinkFirst = new Random().Next(2) == 0;

                if (!hasIsland)
                {
                    string suggestion = sinkFirst
                        ? "✅ Suggestion: Sink on first, cooktop on second."
                        : "✅ Suggestion: Sink on second, cooktop on first.";
                    Log(suggestion);
                }
                else
                {
                    string suggestion = sinkFirst
                        ? "✅ Suggestion: Sink on first, cooktop on island."
                        : "✅ Suggestion: Cooktop on first, sink on island.";
                    Log(suggestion);
                }
            }
        }
    }
}
