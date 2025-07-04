using System;
using System.IO;
using System.Text.Json.Nodes;
using Kitchenbuilder.Models;

/*
 * OneCountertopSelector.cs
 * -------------------------
 * This class suggests layout options for placing the sink and cooktop 
 * when only one valid countertop is available in the kitchen design.
 *
 * Logic Summary:
 * - Always suggests placing both sink and cooktop on the same countertop.
 * - If there is an island:
 *     - If windows exist in the countertop's range:
 *         - If multiple windows: place the sink in the middle of the countertop.
 *         - Else: place the sink under the window and cooktop on the island.
 *     - If no window in range: randomly assign sink or cooktop to the island.
 *
 */
namespace Kitchenbuilder.Core
{
    public static class OneCountertopSelector
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Sink-Cooktop\OneCountertopSelector.txt";

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        public static void SuggestLayouts(Countertop countertop, int optionNum)
        {
            Log($"🛠️ Suggesting layout for Wall{countertop.WallNumber} {countertop.BaseKey} (Start={countertop.Start}, End={countertop.End})");

            // Build generic path to Option{num}SLD.json
            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "Kitchenbuilder",
                "Kitchenbuilder",
                "JSON"
            );

            string jsonPath = Path.Combine(basePath, $"Option{optionNum}SLD.json");

            bool hasIsland = false;
            bool windowInRange = false;
            int windowCountInRange = 0;

            if (File.Exists(jsonPath))
            {
                var json = JsonNode.Parse(File.ReadAllText(jsonPath))!.AsObject();

                string islandVal = json["HasIsland"]?.ToString()?.ToLower() ?? "false";
                hasIsland = islandVal == "true";
                Log($"🔍 HasIsland = {hasIsland}");

                windowInRange = WindowRangeChecker.IsWindowInRange(countertop.Start, countertop.End, countertop.WallNumber);
                windowCountInRange = WindowRangeChecker.CountWindowsInRange(countertop.Start, countertop.End, countertop.WallNumber);
                Log($"🪟 Window in range = {windowInRange}");
                Log($"🔢 Windows in range = {windowCountInRange}");
            }
            else
            {
                Log("❌ Option JSON not found.");
            }

            // Always suggest placing both on the same countertop
            Log("✅ Suggestion 1: Place both sink and cooktop on the same countertop.");

            if (hasIsland)
            {
                if (windowInRange)
                {
                    if (windowCountInRange > 1)
                    {
                        Log("✅ Suggestion 2: Sink placed in the middle of the countertop (multiple windows), cooktop on island.");
                    }
                    else
                    {
                        Log("✅ Suggestion 2: Sink on countertop (under window), cooktop on island.");
                    }
                }
                else
                {
                    bool sinkOnIsland = new Random().Next(2) == 0;
                    string suggestion = sinkOnIsland
                        ? "✅ Suggestion 2: Sink on island, cooktop on countertop."
                        : "✅ Suggestion 2: Cooktop on island, sink on countertop.";
                    Log(suggestion);
                }
            }
        }
    }
}
