using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using Kitchenbuilder.Models;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public static class TwoCountertopSelector
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Sink-Cooktop\TwoCountertopSelector.txt";

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}");
        }

        public static void SuggestLayouts(List<Countertop> countertops, int optionNum, IModelDoc2 model)
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
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
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
                            floorWidth, floorLength, ct1.Start, ct1.End, model);
                        SaveSinkCooktopImage.Save(model, optionNum, $"sink_on_window_wall{ct1.WallNumber}");
                    }

                    _ = SinkCooktopMiddle.CreateCooktopInMiddle(ct2.WallNumber, baseNum2, optionNum, model);
                    SaveSinkCooktopImage.Save(model, optionNum, $"cooktop_middle_wall{ct2.WallNumber}");
                }
                else
                {
                    Log("✅ Sink centered on first countertop (multiple windows), cooktop on second.");
                    _ = SinkCooktopMiddle.CreateSinkInMiddle(ct1.WallNumber, baseNum1, optionNum, model);
                    SaveSinkCooktopImage.Save(model, optionNum, $"sink_middle_wall{ct1.WallNumber}");

                    _ = SinkCooktopMiddle.CreateCooktopInMiddle(ct2.WallNumber, baseNum2, optionNum, model);
                    SaveSinkCooktopImage.Save(model, optionNum, $"cooktop_middle_wall{ct2.WallNumber}");
                }

                if (hasIsland)
                    Log("✅ Alternative: Sink on first, cooktop on island.");
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
                            floorWidth, floorLength, ct2.Start, ct2.End, model);
                        SaveSinkCooktopImage.Save(model, optionNum, $"sink_on_window_wall{ct2.WallNumber}");
                    }

                    _ = SinkCooktopMiddle.CreateCooktopInMiddle(ct1.WallNumber, baseNum1, optionNum, model);
                    SaveSinkCooktopImage.Save(model, optionNum, $"cooktop_middle_wall{ct1.WallNumber}");
                }
                else
                {
                    Log("✅ Sink centered on second countertop (multiple windows), cooktop on first.");
                    _ = SinkCooktopMiddle.CreateSinkInMiddle(ct2.WallNumber, baseNum2, optionNum, model);
                    SaveSinkCooktopImage.Save(model, optionNum, $"sink_middle_wall{ct2.WallNumber}");

                    _ = SinkCooktopMiddle.CreateCooktopInMiddle(ct1.WallNumber, baseNum1, optionNum, model);
                    SaveSinkCooktopImage.Save(model, optionNum, $"cooktop_middle_wall{ct1.WallNumber}");
                }

                if (hasIsland)
                    Log("✅ Alternative: Sink on second, cooktop on island.");
            }
            else
            {
                bool sinkFirst = new Random().Next(2) == 0;

                if (!hasIsland)
                {
                    Log($"✅ No windows: Sink on {(sinkFirst ? "first" : "second")}, cooktop on {(sinkFirst ? "second" : "first")}.");

                    var sinkWall = sinkFirst ? ct1.WallNumber : ct2.WallNumber;
                    var sinkBase = sinkFirst ? baseNum1 : baseNum2;
                    _ = SinkCooktopMiddle.CreateSinkInMiddle(sinkWall, sinkBase, optionNum, model);
                    SaveSinkCooktopImage.Save(model, optionNum, $"sink_middle_wall{sinkWall}");

                    var cooktopWall = sinkFirst ? ct2.WallNumber : ct1.WallNumber;
                    var cooktopBase = sinkFirst ? baseNum2 : baseNum1;
                    _ = SinkCooktopMiddle.CreateCooktopInMiddle(cooktopWall, cooktopBase, optionNum, model);
                    SaveSinkCooktopImage.Save(model, optionNum, $"cooktop_middle_wall{cooktopWall}");
                }
                else
                {
                    Log($"✅ No windows: {(sinkFirst ? "Sink on first, cooktop on island" : "Cooktop on first, sink on island")}.");

                    if (sinkFirst)
                    {
                        _ = SinkCooktopMiddle.CreateSinkInMiddle(ct1.WallNumber, baseNum1, optionNum, model);
                        SaveSinkCooktopImage.Save(model, optionNum, $"sink_middle_wall{ct1.WallNumber}_island");
                        // TODO: Add cooktop on island
                    }
                    else
                    {
                        _ = SinkCooktopMiddle.CreateCooktopInMiddle(ct1.WallNumber, baseNum1, optionNum, model);
                        SaveSinkCooktopImage.Save(model, optionNum, $"cooktop_middle_wall{ct1.WallNumber}_island");
                        // TODO: Add sink on island
                    }
                }
            }
        }
    }
}
