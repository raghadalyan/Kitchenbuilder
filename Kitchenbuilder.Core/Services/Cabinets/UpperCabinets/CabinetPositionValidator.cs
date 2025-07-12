using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Kitchenbuilder.Core.Models;
/*
 * CabinetPositionValidator.cs
 * ---------------------------
 * This utility validates whether a new upper cabinet can be legally added to the kitchen layout
 * without conflicting with existing elements. It performs multiple checks using input JSON files.
 *
 * Method: CheckDownPosition(int optionNum, CabinetInfo cabinet)
 * --------------------------------------------------------------
 * ✅ Detects the wall number based on the cabinet's DistanceX and visible base ranges.
 * ✅ Validates cabinet DistanceY based on proximity to:
 *    - Fridge base (must be ≥ 182 cm away)
 *    - Countertop (must be ≥ 150 cm away)
 * ✅ Checks for X/Y range overlap with:
 *    - Cabinets already placed in OptionXSLD_stations.json
 *    - Cabinets in UpperCabinets.json
 * ❌ If any collision or illegal positioning is detected, returns a specific error message.
 * ✅ Otherwise, returns a success message indicating the position is valid for the "down" side.
 *
 * Overlap Check Helpers:
 * - IsXOverlap(): Validates horizontal intersection
 * - IsYOverlap(): Validates vertical intersection
 */

namespace Kitchenbuilder.Core
{
    public static class CabinetPositionValidator
    {
        public static string CheckDownPosition(int optionNum, CabinetInfo cabinet)
        {
            try
            {
                string basePath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON";
                string optionPath = $@"{basePath}\Option{optionNum}SLD.json";
                string stationPath = $@"{basePath}\Option{optionNum}SLD_stations.json";
                string upperPath = $@"{basePath}\UpperCabinets.json";

                string optionJson = File.ReadAllText(optionPath);
                using var optionDoc = JsonDocument.Parse(optionJson);

                int detectedWall = -1;

                // 1. Detect Wall Number by DistanceX location
                foreach (var wall in optionDoc.RootElement.EnumerateObject().Where(p => p.Name.StartsWith("Wall")))
                {
                    if (wall.Value.TryGetProperty("Bases", out var bases))
                    {
                        foreach (var b in bases.EnumerateObject())
                        {
                            var baseObj = b.Value;
                            if (baseObj.TryGetProperty("Visible", out var vis) && vis.GetBoolean())
                            {
                                double start = baseObj.GetProperty("Start").GetDouble();
                                double end = baseObj.GetProperty("End").GetDouble();
                                if (cabinet.DistanceX >= start && cabinet.DistanceX < end)
                                {
                                    detectedWall = int.Parse(wall.Name.Replace("Wall", ""));
                                    break;
                                }
                            }
                        }
                    }

                    if (detectedWall != -1)
                        break;
                }

                if (detectedWall == -1)
                    return "❌ Cannot detect wall number for cabinet position.";

                string wallKey = $"Wall{detectedWall}";

                // 2. Check proximity to fridge and countertop
                var wallElement = optionDoc.RootElement.GetProperty(wallKey);
                if (wallElement.TryGetProperty("Bases", out var basesElement))
                {
                    foreach (var b in basesElement.EnumerateObject())
                    {
                        var baseObj = b.Value;
                        if (!baseObj.GetProperty("Visible").GetBoolean()) continue;

                        double start = baseObj.GetProperty("Start").GetDouble();
                        double end = baseObj.GetProperty("End").GetDouble();

                        if (cabinet.DistanceX >= start && cabinet.DistanceX < end)
                        {
                            string sketch = baseObj.GetProperty("SketchName").GetString();
                            if (sketch.Contains("fridge") && cabinet.DistanceY < 182)
                                return "❌ Cabinet too close to fridge base. DistanceY must be ≥ 182 cm.";

                            if (baseObj.TryGetProperty("Countertop", out var ct))
                            {
                                double ctStart = ct.GetProperty("Start").GetDouble();
                                double ctEnd = ct.GetProperty("End").GetDouble();

                                if (cabinet.DistanceX >= ctStart && cabinet.DistanceX < ctEnd && cabinet.DistanceY < 150)
                                    return "❌ Cabinet too close to countertop. DistanceY must be ≥ 150 cm.";
                            }
                        }
                    }
                }

                // 3. Check overlap with OptionXSLD_stations.json
                if (File.Exists(stationPath))
                {
                    string stationJson = File.ReadAllText(stationPath);
                    var stations = JsonSerializer.Deserialize<List<StationInfo>>(stationJson);

                    foreach (var station in stations.Where(s => s.WallNumber == detectedWall && s.Cabinets != null))
                    {
                        foreach (var existing in station.Cabinets)
                        {
                            if (IsXOverlap(cabinet, existing) && IsYOverlap(cabinet, existing))
                                return $"❌ Overlaps with cabinet in stations (Sketch: {existing.SketchName})";
                        }
                    }
                }

                // 4. Check overlap with UpperCabinets.json
                if (File.Exists(upperPath))
                {
                    string upperJson = File.ReadAllText(upperPath);
                    var wallDict = JsonSerializer.Deserialize<Dictionary<string, List<CabinetInfo>>>(upperJson);
                    string key = $"Wall{detectedWall}";

                    if (wallDict != null && wallDict.ContainsKey(key))
                    {
                        foreach (var existing in wallDict[key])
                        {
                            if (IsXOverlap(cabinet, existing) && IsYOverlap(cabinet, existing))
                                return $"❌ Overlaps with cabinet in UpperCabinets.json (Sketch: {existing.SketchName})";
                        }
                    }
                }

                return "✅ Position is legal (down side).";
            }
            catch (Exception ex)
            {
                return $"❌ Error in CheckDownPosition: {ex.Message}";
            }
        }

        private static bool IsXOverlap(CabinetInfo a, CabinetInfo b) =>
            a.DistanceX < b.DistanceX + b.Width && a.DistanceX + a.Width > b.DistanceX;

        private static bool IsYOverlap(CabinetInfo a, CabinetInfo b) =>
            a.DistanceY < b.DistanceY + b.Height && a.DistanceY + a.Height > b.DistanceY;
    }
}
