using System;
using System.IO;
using System.Text.Json.Nodes;
using Kitchenbuilder.Models;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public static class OneCountertopSelector
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Sink-Cooktop\OneCountertopSelector.txt";

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}");
        }
        public static void SuggestLayouts(Countertop countertop, int optionNum, IModelDoc2 model)
        {
            Log($"🛠️ Suggesting layout for Wall{countertop.WallNumber} {countertop.BaseKey} (Start={countertop.Start}, End={countertop.End})");

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
            var inputJson = JsonNode.Parse(File.ReadAllText(inputPath))!.AsObject();

            bool hasIsland = json["HasIsland"]?.ToString()?.ToLower() == "true";
            Log($"🔍 HasIsland = {hasIsland}");

            bool windowInRange = WindowRangeChecker.IsWindowInRange(countertop.Start, countertop.End, countertop.WallNumber);
            int windowCountInRange = WindowRangeChecker.CountWindowsInRange(countertop.Start, countertop.End, countertop.WallNumber);

            Log($"🪟 Window in range = {windowInRange}");
            Log($"🔢 Windows in range = {windowCountInRange}");

            if (!int.TryParse(countertop.BaseKey.Replace("Base", ""), out int baseNum))
            {
                Log("❌ Could not parse base number from BaseKey.");
                return;
            }

            double floorWidth = json["Floor"]?["Width"]?["Size"]?.GetValue<double>() ?? 0;
            double floorLength = json["Floor"]?["Length"]?["Size"]?.GetValue<double>() ?? 0;

            // ✅ Suggestion 1: Sink and cooktop on same countertop
            Log("✅ Suggestion 1: Place both sink and cooktop on the same countertop.");
            var (sink1, cooktop1) = SinkCooktopSameCountertop.Create(
                countertop.WallNumber,
                baseNum,
                countertop.Start,
                countertop.End,
                floorWidth,
                floorLength,
                model
            );

            if (sink1 != null)
                Log($"👉 Sink: X={sink1.DistanceX_Faucet_On_CT}, Y={sink1.DistanceY_Faucet_On_CT}, Angle={sink1.Angle_Sketch_Rotate_Faucet}");
            if (cooktop1 != null)
                Log($"👉 Cooktop: X={cooktop1.DistanceX_Cooktop_On_CT}, Y={cooktop1.DistanceY_Cooktop_On_CT}, Angle={cooktop1.Angle_Sketch_Rotate_Cooktop}");

            if (sink1 != null && cooktop1 != null)
                SaveSinkCooktopImage.Save(model, optionNum, "Suggestion1_SameCountertop");

            // ✅ Suggestion 2: One on countertop, one on island
            if (!hasIsland)
                return;

            bool suggestion2Valid = false;

            if (windowInRange)
            {
                if (windowCountInRange > 1)
                {
                    Log("✅ Suggestion 2: Sink in middle (multiple windows), cooktop on island.");
                    var sink2 = SinkCooktopMiddle.CreateSinkInMiddle(countertop.WallNumber, baseNum, optionNum, model);
                    if (sink2 != null)
                    {
                        Log($"👉 Sink: X={sink2.DistanceX_Faucet_On_CT}, Y={sink2.DistanceY_Faucet_On_CT}, Angle={sink2.Angle_Sketch_Rotate_Faucet}");
                        suggestion2Valid = true;
                    }
                }
                else
                {
                    Log("✅ Suggestion 2: Sink under window, cooktop on island.");
                    int wallIndex = countertop.WallNumber - 1;
                    var windowArray = inputJson["Walls"]?[wallIndex]?["Windows"]?.AsArray();

                    if (windowArray != null && windowArray.Count > 0)
                    {
                        foreach (var window in windowArray)
                        {
                            double distanceX = window?["DistanceX"]?.GetValue<double>() ?? 0;
                            double width = window?["Width"]?.GetValue<double>() ?? 0;
                            double center = distanceX + width / 2;

                            if (center >= countertop.Start && center <= countertop.End)
                            {
                                var sink2 = SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow(
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
                                    countertop.End,
                                    model
                                );

                                if (sink2 is Sink sinkObj)
                                {
                                    Log($"👉 Sink under window: X={sinkObj.DistanceX_Faucet_On_CT}, Y={sinkObj.DistanceY_Faucet_On_CT}, Angle={sinkObj.Angle_Sketch_Rotate_Faucet}");
                                    suggestion2Valid = true;
                                }

                                break;
                            }
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
                    var cooktop2 = SinkCooktopMiddle.CreateCooktopInMiddle(countertop.WallNumber, baseNum, optionNum, model);
                    if (cooktop2 != null)
                    {
                        Log($"👉 Cooktop: X={cooktop2.DistanceX_Cooktop_On_CT}, Y={cooktop2.DistanceY_Cooktop_On_CT}, Angle={cooktop2.Angle_Sketch_Rotate_Cooktop}");
                        suggestion2Valid = true;
                    }
                }
                else
                {
                    Log("✅ Suggestion 2: Cooktop on island, sink on countertop.");
                    var sink2 = SinkCooktopMiddle.CreateSinkInMiddle(countertop.WallNumber, baseNum, optionNum, model);
                    if (sink2 != null)
                    {
                        Log($"👉 Sink: X={sink2.DistanceX_Faucet_On_CT}, Y={sink2.DistanceY_Faucet_On_CT}, Angle={sink2.Angle_Sketch_Rotate_Faucet}");
                        suggestion2Valid = true;
                    }
                }
            }

            if (suggestion2Valid)
                SaveSinkCooktopImage.Save(model, optionNum, "Suggestion2_Island");
        }

    }
}
