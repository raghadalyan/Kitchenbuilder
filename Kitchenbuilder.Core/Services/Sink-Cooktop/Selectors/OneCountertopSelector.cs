using System;
using System.IO;
using System.Text.Json.Nodes;
using Kitchenbuilder.Core.Models;
using Kitchenbuilder.Models;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public static class OneCountertopSelector
    {
        private static readonly string DebugPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Output", "Sink-Cooktop", "OneCountertopSelector.txt"
        );

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}");
        }

        public static void SuggestLayouts(Countertop countertop, int optionNum, IModelDoc2 model)
        {
            int layoutFolderIndex = 1;
            Sink? sink;
            Cooktop? cooktop;

            Log($"\ud83d\udc77\ufe0f Suggesting layout for Wall{countertop.WallNumber} {countertop.BaseKey} (Start={countertop.Start}, End={countertop.End})");

            string basePath = Path.Combine(KitchenConfig.Get().BasePath, "Kitchenbuilder", "Kitchenbuilder", "JSON");
            string jsonPath = Path.Combine(basePath, $"Option{optionNum}SLD.json");
            string inputPath = Path.Combine(basePath, "input.json");

            if (!File.Exists(jsonPath) || !File.Exists(inputPath))
            {
                Log("\u274c Required JSON files not found.");
                return;
            }

            var json = JsonNode.Parse(File.ReadAllText(jsonPath))!.AsObject();
            var inputJson = JsonNode.Parse(File.ReadAllText(inputPath))!.AsObject();

            bool hasIsland = json["HasIsland"]?.ToString()?.ToLower() == "true";
            Log($"\ud83d\udd0d HasIsland = {hasIsland}");

            Island? island = hasIsland ? new Island
            {
                Direction = json["Island"]?["Direction"]?.GetValue<double>() ?? 90,
                DistanceX = json["Island"]?["DistanceX"]?.GetValue<double>() ?? 0,
                DistanceY = json["Island"]?["DistanceY"]?.GetValue<double>() ?? 0,
                Depth = (int)(json["Island"]?["Depth"]?.GetValue<double>() ?? 90),
                Width = (int)(json["Island"]?["Width"]?.GetValue<double>() ?? 180),
                Material = json["Island"]?["Material"]?.ToString() ?? ""
            } : null;

            bool windowInRange = WindowRangeChecker.IsWindowInRange(countertop.Start, countertop.End, countertop.WallNumber);
            int windowCountInRange = WindowRangeChecker.CountWindowsInRange(countertop.Start, countertop.End, countertop.WallNumber);

            Log($"\ud83e\uddae Window in range = {windowInRange}");
            Log($"\ud83d\udd22 Windows in range = {windowCountInRange}");

            if (!int.TryParse(countertop.BaseKey.Replace("Base", ""), out int baseNum))
            {
                Log("\u274c Could not parse base number from BaseKey.");
                return;
            }

            double floorWidth = json["Floor"]?["Width"]?["Size"]?.GetValue<double>() ?? 0;
            double floorLength = json["Floor"]?["Length"]?["Size"]?.GetValue<double>() ?? 0;

            // Suggestion 1
            Log("\u2705 Suggestion 1: Sink and cooktop on the same countertop.");
            (sink, cooktop) = SinkCooktopSameCountertop.Create(
                countertop.WallNumber, baseNum, countertop.Start, countertop.End, floorWidth, floorLength, model
            );

            if (sink != null && cooktop != null)
            {
                Log($"\ud83d\udc49 Sink: X={sink.DistanceX_Faucet_On_CT}, Y={sink.DistanceY_Faucet_On_CT}, Angle={sink.Angle_Sketch_Rotate_Faucet}");
                Log($"\ud83d\udc49 Cooktop: X={cooktop.DistanceX_Cooktop_On_CT}, Y={cooktop.DistanceY_Cooktop_On_CT}, Angle={cooktop.Angle_Sketch_Rotate_Cooktop}");
                SaveSinkCooktopImage.Save(model, layoutFolderIndex++, "Suggestion1_SameCountertop", optionNum, sink, cooktop);
            }

            if (!hasIsland) return;
            bool suggestion2Valid = false;

            if (windowInRange)
            {
                if (windowCountInRange > 1)
                {
                    Log("\u2705 Suggestion 2: Sink in middle, cooktop on island.");
                    sink = SinkCooktopMiddle.CreateSinkInMiddle(countertop.WallNumber, baseNum, optionNum, model);
                    cooktop = SinkCooktopOnIsland.CreateCooktopOnIsland(countertop.WallNumber, island!, model);
                }
                else
                {
                    Log("\u2705 Suggestion 2: Sink under window, cooktop on island.");
                    int wallIndex = countertop.WallNumber - 1;
                    var windowArray = inputJson["Walls"]?[wallIndex]?["Windows"]?.AsArray();

                    sink = null;
                    cooktop = SinkCooktopOnIsland.CreateCooktopOnIsland(countertop.WallNumber, island!, model);

                    if (windowArray != null && windowArray.Count > 0)
                    {
                        foreach (var window in windowArray)
                        {
                            double distanceX = window?["DistanceX"]?.GetValue<double>() ?? 0;
                            double width = window?["Width"]?.GetValue<double>() ?? 0;
                            double center = distanceX + width / 2;

                            if (center >= countertop.Start && center <= countertop.End)
                            {
                                var sinkObj = SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow("sink", countertop.WallNumber, baseNum,
                                    distanceX,
                                    window?["DistanceY"]?.GetValue<double>() ?? 0,
                                    width,
                                    window?["Height"]?.GetValue<double>() ?? 0,
                                    floorWidth, floorLength, countertop.Start, countertop.End, model);

                                if (sinkObj is Sink castedSink)
                                {
                                    sink = castedSink;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Log("\u2705 Suggestion 2: Random island split.");
                bool sinkOnIsland = new Random().Next(2) == 0;

                if (sinkOnIsland)
                {
                    sink = SinkCooktopOnIsland.CreateSinkOnIsland(countertop.WallNumber, island!, model);
                    cooktop = SinkCooktopMiddle.CreateCooktopInMiddle(countertop.WallNumber, baseNum, optionNum, model);
                }
                else
                {
                    sink = SinkCooktopMiddle.CreateSinkInMiddle(countertop.WallNumber, baseNum, optionNum, model);
                    cooktop = SinkCooktopOnIsland.CreateCooktopOnIsland(countertop.WallNumber, island!, model);
                }
            }

            if (sink != null && cooktop != null)
            {
                SaveSinkCooktopImage.Save(model, layoutFolderIndex++, "Suggestion2_Island", optionNum, sink, cooktop);
                suggestion2Valid = true;
            }
        }
    }
}
