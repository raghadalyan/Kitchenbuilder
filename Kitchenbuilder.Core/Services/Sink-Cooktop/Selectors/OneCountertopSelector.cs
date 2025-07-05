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

            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "Kitchenbuilder", "Kitchenbuilder", "JSON");

            string jsonPath = Path.Combine(basePath, $"Option{optionNum}SLD.json");
            JsonObject? json = null;
            string inputPath = Path.Combine(basePath, "input.json");
            JsonObject? inputJson = null;

            if (File.Exists(inputPath))
            {
                inputJson = JsonNode.Parse(File.ReadAllText(inputPath))!.AsObject();
            }
            else
            {
                Log("❌ input.json not found.");
                return;
            }
            bool hasIsland = false;
            bool windowInRange = false;
            int windowCountInRange = 0;

            if (File.Exists(jsonPath))
            {
                json = JsonNode.Parse(File.ReadAllText(jsonPath))!.AsObject();
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
                return;
            }

            // Always suggest placing both on the same countertop
            Log("✅ Suggestion 1: Place both sink and cooktop on the same countertop.");
            if (!int.TryParse(countertop.BaseKey.Replace("Base", ""), out int baseNum))
            {
                Log("❌ Could not parse base number from BaseKey.");
                return;
            }

            double floorWidth = json["Floor"]?["Width"]?["Size"]?.GetValue<double>() ?? 0;
            double floorLength = json["Floor"]?["Length"]?["Size"]?.GetValue<double>() ?? 0;

            var (sink3, cooktop3) = SinkCooktopSameCountertop.Create(
                countertop.WallNumber,
                baseNum,
                countertop.Start,
                countertop.End,
                floorWidth,
                floorLength
            );

            if (sink3 != null)
                Log($"👉 Sink created: X={sink3.DistanceX_Faucet_On_CT}, Y={sink3.DistanceY_Faucet_On_CT}, Angle={sink3.Angle_Sketch_Rotate_Faucet}");

            if (cooktop3 != null)
                Log($"👉 Cooktop created: X={cooktop3.DistanceX_Cooktop_On_CT}, Y={cooktop3.DistanceY_Cooktop_On_CT}, Angle={cooktop3.Angle_Sketch_Rotate_Cooktop}");

            if (!hasIsland) return;

    

            if (windowInRange)
            {
                if (windowCountInRange > 1)
                {
                    Log("✅ Suggestion 2: Sink placed in the middle of the countertop (multiple windows), cooktop on island.");
                    var sink = SinkCooktopMiddle.CreateSinkInMiddle(countertop.WallNumber, baseNum, optionNum);
                    if (sink != null)
                        Log($"👉 Sink created at X={sink.DistanceX_Faucet_On_CT}, Y={sink.DistanceY_Faucet_On_CT}, Angle={sink.Angle_Sketch_Rotate_Faucet}");
                }
                else
                {
                    Log("✅ Suggestion 2: Sink on countertop (under window), cooktop on island.");


                    // ✅ Load window from input.json
                    int wallIndex = countertop.WallNumber - 1;
                    var wall = inputJson["Walls"]?[wallIndex];
                    var windows = wall?["Windows"]?.AsArray();

                    if (windows == null || windows.Count == 0)
                    {
                        Log("❌ No window data found in input.json.");
                        return;
                    }

                    foreach (var window in windows)
                    {
                        double distanceX = window?["DistanceX"]?.GetValue<double>() ?? 0;
                        double width = window?["Width"]?.GetValue<double>() ?? 0;
                        double center = distanceX + width / 2;

                        if (center >= countertop.Start && center <= countertop.End)
                        {
                            var sink = SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow(
     "sink",
     countertop.WallNumber,
     baseNum,
     distanceX,
     window?["DistanceY"]?.GetValue<double>() ?? 0,
     width,
     window?["Height"]?.GetValue<double>() ?? 0,
     floorWidth,
     floorLength,
     countertop.Start,
     countertop.End  // ✅ FIXED: added missing argument
 );


                            if (sink is Sink sinkObj)
                            {
                                Log($"👉 Sink under window (from input.json): X={sinkObj.DistanceX_Faucet_On_CT}, Y={sinkObj.DistanceY_Faucet_On_CT}, Angle={sinkObj.Angle_Sketch_Rotate_Faucet}");
                            }

                            break;
                        }
                    }

                }
            }
            else
            {
                bool sinkOnIsland = new Random().Next(2) == 0;
                if (sinkOnIsland)
                {
                    Log("✅ Suggestion 2: Sink on island, cooktop on countertop.");
                    var cooktop = SinkCooktopMiddle.CreateCooktopInMiddle(countertop.WallNumber, baseNum, optionNum);
                    if (cooktop != null)
                        Log($"👉 Cooktop created at X={cooktop.DistanceX_Cooktop_On_CT}, Y={cooktop.DistanceY_Cooktop_On_CT}, Angle={cooktop.Angle_Sketch_Rotate_Cooktop}");
                }
                else
                {
                    Log("✅ Suggestion 2: Cooktop on island, sink on countertop.");
                    var sink = SinkCooktopMiddle.CreateSinkInMiddle(countertop.WallNumber, baseNum, optionNum);
                    if (sink != null)
                        Log($"👉 Sink created at X={sink.DistanceX_Faucet_On_CT}, Y={sink.DistanceY_Faucet_On_CT}, Angle={sink.Angle_Sketch_Rotate_Faucet}");
                }
            }
        }
    }
}