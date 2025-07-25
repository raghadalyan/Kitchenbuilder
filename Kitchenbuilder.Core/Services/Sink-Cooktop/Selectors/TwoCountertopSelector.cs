﻿using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using Kitchenbuilder.Models;
using SolidWorks.Interop.sldworks;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core
{
    public static class TwoCountertopSelector
    {
        private static readonly string DebugPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Output", "Sink-Cooktop", "TwoCountertopSelector.txt"
        );

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}");
        }

        public static void SuggestLayouts(List<Countertop> countertops, int optionNum, IModelDoc2 model)
        {
            Log("🔍 Starting TwoCountertopSelector.SuggestLayouts...");

            if (countertops.Count != 2)
            {
                Log("❌ Expected exactly 2 countertops.");
                return;
            }

            bool usedWindowLayout = false;
            int layoutCounter = 1;

            var ct1 = countertops[0];
            var ct2 = countertops[1];
            Sink? sink = null;
            Cooktop? cooktop = null;

            Log($"🛠️ Found 2 countertops: Wall{ct1.WallNumber} {ct1.BaseKey}, Wall{ct2.WallNumber} {ct2.BaseKey}");

            string basePath = Path.Combine(KitchenConfig.Get().BasePath, "Kitchenbuilder", "Kitchenbuilder", "JSON");
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

            // Step 1: Try same countertop
            double width1 = ct1.End - ct1.Start;
            double width2 = ct2.End - ct2.Start;
            var widerCT = width1 >= width2 ? ct1 : ct2;
            int widerBase = int.Parse(widerCT.BaseKey.Replace("Base", ""));

            Log($"📏 Trying sink & cooktop on wider CT: Wall{widerCT.WallNumber}");

            (sink, cooktop) = SinkCooktopSameCountertop.Create(
                widerCT.WallNumber,
                widerBase,
                widerCT.Start,
                widerCT.End,
                floorWidth,
                floorLength,
                model
            );

            if (sink != null && cooktop != null)
            {
                Log("✅ Same countertop layout succeeded.");
                string imageName = $"sink_cooktop_same_wall{widerCT.WallNumber}";
                SaveSinkCooktopImage.Save(model, layoutCounter, imageName, optionNum, sink, cooktop);
                Log($"📂 Created layout {layoutCounter}: {imageName}");
                layoutCounter++;
            }
            else Log("⚠️ Same countertop layout failed.");

            // Step 2: Window logic
            int baseNum1 = int.Parse(ct1.BaseKey.Replace("Base", ""));
            int baseNum2 = int.Parse(ct2.BaseKey.Replace("Base", ""));
            bool winInRange1 = WindowRangeChecker.IsWindowInRange(ct1.Start, ct1.End, ct1.WallNumber);
            bool winInRange2 = WindowRangeChecker.IsWindowInRange(ct2.Start, ct2.End, ct2.WallNumber);
            int winCount1 = winInRange1 ? WindowRangeChecker.CountWindowsInRange(ct1.Start, ct1.End, ct1.WallNumber) : 0;
            int winCount2 = winInRange2 ? WindowRangeChecker.CountWindowsInRange(ct2.Start, ct2.End, ct2.WallNumber) : 0;

            if (winInRange1 && winCount1 == 1)
            {
                Log($"🌞 Window detected on Wall{ct1.WallNumber}");
                var window = input["Walls"]?[ct1.WallNumber - 1]?["Windows"]?[0];
                if (window != null)
                {
                    sink = (Sink)SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow("sink", ct1.WallNumber, baseNum1,
                        window["DistanceX"]?.GetValue<double>() ?? 0,
                        window["DistanceY"]?.GetValue<double>() ?? 0,
                        window["Width"]?.GetValue<double>() ?? 0,
                        window["Height"]?.GetValue<double>() ?? 0,
                        floorWidth, floorLength, ct1.Start, ct1.End, model);

                    cooktop =(Cooktop)SinkCooktopMiddle.CreateCooktopInMiddle(ct2.WallNumber, baseNum2, optionNum, model);
                    string imageName = $"sink_window_wall{ct1.WallNumber}_cooktop_middle_wall{ct2.WallNumber}";
                    SaveSinkCooktopImage.Save(model, layoutCounter, imageName, optionNum, sink, cooktop);
                    Log($"📂 Created layout {layoutCounter}: {imageName}");
                    layoutCounter++;
                    usedWindowLayout = true;
                }
            }
            else if (winInRange2 && winCount2 == 1)
            {
                Log($"🌞 Window detected on Wall{ct2.WallNumber}");
                var window = input["Walls"]?[ct2.WallNumber - 1]?["Windows"]?[0];
                if (window != null)
                {
                    sink = (Sink)SinkCooktopOnWindow.CreateSinkOrCooktopOnWindow("sink", ct2.WallNumber, baseNum2,
                        window["DistanceX"]?.GetValue<double>() ?? 0,
                        window["DistanceY"]?.GetValue<double>() ?? 0,
                        window["Width"]?.GetValue<double>() ?? 0,
                        window["Height"]?.GetValue<double>() ?? 0,
                        floorWidth, floorLength, ct2.Start, ct2.End, model);

                    cooktop = (Cooktop)SinkCooktopMiddle.CreateCooktopInMiddle(ct1.WallNumber, baseNum1, optionNum, model);
                    string imageName = $"sink_window_wall{ct2.WallNumber}_cooktop_middle_wall{ct1.WallNumber}";
                    SaveSinkCooktopImage.Save(model, layoutCounter, imageName, optionNum, sink, cooktop);
                    Log($"📂 Created layout {layoutCounter}: {imageName}");
                    layoutCounter++;
                    usedWindowLayout = true;
                }
            }

            // Step 3: Fallback
            if (!usedWindowLayout)
            {
                Log("🔄 Using fallback: middle-middle layout.");
                bool sinkFirst = new Random().Next(2) == 0;
                var sinkWall = sinkFirst ? ct1.WallNumber : ct2.WallNumber;
                var sinkBase = sinkFirst ? baseNum1 : baseNum2;
                var cooktopWall = sinkFirst ? ct2.WallNumber : ct1.WallNumber;
                var cooktopBase = sinkFirst ? baseNum2 : baseNum1;

                sink = SinkCooktopMiddle.CreateSinkInMiddle(sinkWall, sinkBase, optionNum, model);
                cooktop = SinkCooktopMiddle.CreateCooktopInMiddle(cooktopWall, cooktopBase, optionNum, model);
                string imageName = $"sink_middle_wall{sinkWall}_cooktop_middle_wall{cooktopWall}";
                SaveSinkCooktopImage.Save(model, layoutCounter, imageName, optionNum, sink, cooktop);
                Log($"📂 Created layout {layoutCounter}: {imageName}");
                layoutCounter++;
            }

            // Step 4: Island
            if (hasIsland)
            {
                Log("🌴 Island detected. Trying split layout.");
                var island = new Island
                {
                    Direction = json["Island"]?["Direction"]?.GetValue<double>() ?? 90,
                    DistanceX = json["Island"]?["DistanceX"]?.GetValue<double>() ?? 0,
                    DistanceY = json["Island"]?["DistanceY"]?.GetValue<double>() ?? 0,
                    Depth = (int)(json["Island"]?["Depth"]?.GetValue<double>() ?? 90),
                    Width = (int)(json["Island"]?["Width"]?.GetValue<double>() ?? 180),
                    Material = json["Island"]?["Material"]?.ToString() ?? ""
                };

                bool sinkOnIsland = new Random().Next(2) == 0;

                if (sinkOnIsland)
                {
                    sink = SinkCooktopOnIsland.CreateSinkOnIsland(ct1.WallNumber, island, model);
                    cooktop = SinkCooktopMiddle.CreateCooktopInMiddle(ct2.WallNumber, baseNum2, optionNum, model);
                    string imageName = $"sink_island_cooktop_wall{ct2.WallNumber}";
                    SaveSinkCooktopImage.Save(model, layoutCounter, imageName, optionNum, sink, cooktop);
                    Log($"📂 Created layout {layoutCounter}: {imageName}");
                    layoutCounter++;
                }
                else
                {
                    sink = SinkCooktopMiddle.CreateSinkInMiddle(ct1.WallNumber, baseNum1, optionNum, model);
                    cooktop = SinkCooktopOnIsland.CreateCooktopOnIsland(ct2.WallNumber, island, model);
                    string imageName = $"sink_wall{ct1.WallNumber}_cooktop_island";
                    SaveSinkCooktopImage.Save(model, layoutCounter, imageName, optionNum, sink, cooktop);
                    Log($"📂 Created layout {layoutCounter}: {imageName}");
                    layoutCounter++;
                }
            }

            Log("✅ SuggestLayouts finished.");
        }
    }
}
