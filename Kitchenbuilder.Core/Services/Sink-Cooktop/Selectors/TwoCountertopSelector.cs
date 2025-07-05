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
                "Downloads", "Kitchenbuilder", "Kitchenbuilder", "JSON");

            string jsonPath = Path.Combine(basePath, $"Option{optionNum}SLD.json");
            string inputPath = Path.Combine(basePath, "input.json");

            if (!File.Exists(jsonPath) || !File.Exists(inputPath))
            {
                Log("❌ Required JSON files not found.");
                return;
            }

            var json = JsonNode.Parse(File.ReadAllText(jsonPath))!.AsObject();
            var input = JsonNode.Parse(File.ReadAllText(inputPath))!.AsObject();

            double floorWidth = json["Floor"]?["Width"]?["Size"]?.GetValue<double>() ?? 0;
            double floorLength = json["Floor"]?["Length"]?["Size"]?.GetValue<double>() ?? 0;
            bool hasIsland = (json["HasIsland"]?.ToString()?.ToLower() == "true");

            int baseNum1 = int.Parse(ct1.BaseKey.Replace("Base", ""));
            int baseNum2 = int.Parse(ct2.BaseKey.Replace("Base", ""));

            bool winInRange1 = WindowRangeChecker.IsWindowInRange(ct1.Start, ct1.End, ct1.WallNumber);
            bool winInRange2 = WindowRangeChecker.IsWindowInRange(ct2.Start, ct2.End, ct2.WallNumber);

            int winCount1 = winInRange1 ? WindowRangeChecker.CountWindowsInRange(ct1.Start, ct1.End, ct1.WallNumber) : 0;
            int winCount2 = winInRange2 ? WindowRangeChecker.CountWindowsInRange(ct2.Start, ct2.End, ct2.WallNumber) : 0;

            if (winInRange1)
            {
                if (winCount1 == 1)
                {
                    Log("✅ Sink under window on first countertop, cooktop on second.");

                    var window = input["Walls"]?[ct1.WallNumber - 1]?["Windows"]?[0];
                    if (window != null)
                    {
                        _ = SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow("sink", ct1.WallNumber, baseNum1,
                            window["DistanceX"]?.GetValue<double>() ?? 0,
                            window["DistanceY"]?.GetValue<double>() ?? 0,
                            window["Width"]?.GetValue<double>() ?? 0,
                            window["Height"]?.GetValue<double>() ?? 0,
                            floorWidth, floorLength, ct1.Start, ct1.End);
                    }

                    _ = SinkCooktopMiddle.CreateCooktopInMiddle(ct2.WallNumber, baseNum2, optionNum);
                }
                else
                {
                    Log("✅ Sink centered on first countertop (multiple windows), cooktop on second.");
                    _ = SinkCooktopMiddle.CreateSinkInMiddle(ct1.WallNumber, baseNum1, optionNum);
                    _ = SinkCooktopMiddle.CreateCooktopInMiddle(ct2.WallNumber, baseNum2, optionNum);
                }

                if (hasIsland)
                {
                    Log("✅ Alternative: Sink on first, cooktop on island.");
                    // TODO: Add island logic
                }
            }
            else if (winInRange2)
            {
                if (winCount2 == 1)
                {
                    Log("✅ Sink under window on second countertop, cooktop on first.");

                    var window = input["Walls"]?[ct2.WallNumber - 1]?["Windows"]?[0];
                    if (window != null)
                    {
                        _ = SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow("sink", ct2.WallNumber, baseNum2,
                            window["DistanceX"]?.GetValue<double>() ?? 0,
                            window["DistanceY"]?.GetValue<double>() ?? 0,
                            window["Width"]?.GetValue<double>() ?? 0,
                            window["Height"]?.GetValue<double>() ?? 0,
                            floorWidth, floorLength, ct2.Start, ct2.End);
                    }

                    _ = SinkCooktopMiddle.CreateCooktopInMiddle(ct1.WallNumber, baseNum1, optionNum);
                }
                else
                {
                    Log("✅ Sink centered on second countertop (multiple windows), cooktop on first.");
                    _ = SinkCooktopMiddle.CreateSinkInMiddle(ct2.WallNumber, baseNum2, optionNum);
                    _ = SinkCooktopMiddle.CreateCooktopInMiddle(ct1.WallNumber, baseNum1, optionNum);
                }

                if (hasIsland)
                {
                    Log("✅ Alternative: Sink on second, cooktop on island.");
                    // TODO: Add island logic
                }
            }
            else
            {
                bool sinkFirst = new Random().Next(2) == 0;

                if (!hasIsland)
                {
                    Log($"✅ No windows: Sink on {(sinkFirst ? "first" : "second")}, cooktop on {(sinkFirst ? "second" : "first")}.");

                    _ = SinkCooktopMiddle.CreateSinkInMiddle(
                        sinkFirst ? ct1.WallNumber : ct2.WallNumber,
                        sinkFirst ? baseNum1 : baseNum2,
                        optionNum);

                    _ = SinkCooktopMiddle.CreateCooktopInMiddle(
                        sinkFirst ? ct2.WallNumber : ct1.WallNumber,
                        sinkFirst ? baseNum2 : baseNum1,
                        optionNum);
                }
                else
                {
                    Log($"✅ No windows: {(sinkFirst ? "Sink on first, cooktop on island" : "Cooktop on first, sink on island")}.");

                    if (sinkFirst)
                    {
                        _ = SinkCooktopMiddle.CreateSinkInMiddle(ct1.WallNumber, baseNum1, optionNum);
                        // TODO: Add cooktop on island
                    }
                    else
                    {
                        _ = SinkCooktopMiddle.CreateCooktopInMiddle(ct1.WallNumber, baseNum1, optionNum);
                        // TODO: Add sink on island
                    }
                }
            }
        }

    }
}
