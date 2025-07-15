using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core
{
    /*
     * CabinetPositionValidator.cs
     * ---------------------------
     * This utility validates whether a new upper cabinet can be legally added to the kitchen layout
     * without conflicting with existing elements. It performs multiple checks using input JSON files.
     * 
     * Main Method:
     * - CheckDownPosition(): Validates based on proximity, collision, and layout rules.
     * 
     * Debug log saved at: \Output\upper\validator_debug.txt
     */

    public static class CabinetPositionValidator
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\upper\validator_debug.txt";

        public static string CheckDownPosition(int optionNum, int wallNumber, CabinetInfo cabinet)
        {
            try
            {
                Log($"📦 Checking cabinet on Wall {wallNumber}: (X={cabinet.DistanceX}, Y={cabinet.DistanceY}, W={cabinet.Width}, H={cabinet.Height})");

                string basePath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON";
                string optionPath = Path.Combine(basePath, $"Option{optionNum}SLD.json");
                string stationPath = Path.Combine(basePath, $"Option{optionNum}SLD_stations.json");
                string upperPath = Path.Combine(basePath, "UpperCabinets.json");

                int detectedWall = wallNumber;
                string wallKey = $"Wall{detectedWall}";

                if (!File.Exists(optionPath))
                    return $"❌ Option file not found: {optionPath}";

                using var optionDoc = JsonDocument.Parse(File.ReadAllText(optionPath));

                // 1. Check fridge/countertop proximity
                if (optionDoc.RootElement.TryGetProperty(wallKey, out var wallElement) &&
                    wallElement.TryGetProperty("Bases", out var basesElement))
                {
                    foreach (var b in basesElement.EnumerateObject())
                    {
                        var baseObj = b.Value;
                        if (!baseObj.TryGetProperty("Visible", out var visibleProp) || visibleProp.ValueKind != JsonValueKind.True)
                            continue;

                        double start = baseObj.GetProperty("Start").GetDouble();
                        double end = baseObj.GetProperty("End").GetDouble();

                        if (cabinet.DistanceX >= start && cabinet.DistanceX < end)
                        {
                            string sketch = baseObj.GetProperty("SketchName").GetString();
                            Log($"🔍 Cabinet overlaps base: {sketch}");

                            if (sketch.Contains("fridge") && cabinet.DistanceY < 182)
                                return "❌ Cabinet too close to fridge base. DistanceY must be ≥ 182 cm.";
                            if (baseObj.TryGetProperty("Countertop", out var ct) && ct.ValueKind == JsonValueKind.Object)
                            {
                                double ctStart = ct.GetProperty("Start").GetDouble();
                                double ctEnd = ct.GetProperty("End").GetDouble();


                                if (cabinet.DistanceX >= ctStart && cabinet.DistanceX < ctEnd && cabinet.DistanceY < 150)
                                    return "❌ Cabinet too close to countertop. DistanceY must be ≥ 150 cm.";
                            }
                        }
                    }
                }

                // 2. Check overlap with OptionXSLD_stations.json
                if (File.Exists(stationPath))
                {
                    Log($"📄 Checking station overlaps from: {stationPath}");
                    var stations = JsonSerializer.Deserialize<List<StationInfo>>(File.ReadAllText(stationPath));
                    foreach (var station in stations.Where(s => s.WallNumber == detectedWall && s.Cabinets != null))
                    {
                        foreach (var existing in station.Cabinets)
                        {
                            Log($"↔ Compare with station cabinet {existing.SketchName} at (X={existing.DistanceX}, Y={existing.DistanceY})");
                            if (IsXOverlap(cabinet, existing) && IsYOverlap(cabinet, existing))
                                return $"❌ Overlaps with cabinet in stations (Sketch: {existing.SketchName})";
                        }
                    }
                }

                // 3. Check overlap with UpperCabinets.json
                if (File.Exists(upperPath))
                {
                    Log($"📄 Checking upper cabinet overlaps from: {upperPath}");
                    var wallDict = JsonSerializer.Deserialize<Dictionary<string, WallCabinetWrapper>>(File.ReadAllText(upperPath));
                    if (wallDict != null && wallDict.TryGetValue(wallKey, out var wrapper) && wrapper.Cabinets != null)
                    {
                        foreach (var existing in wrapper.Cabinets)
                        {
                            Log($"↔ Compare with upper cabinet {existing.SketchName} at (X={existing.DistanceX}, Y={existing.DistanceY})");
                            if (IsXOverlap(cabinet, existing) && IsYOverlap(cabinet, existing))
                                return $"❌ Overlaps with cabinet in UpperCabinets.json (Sketch: {existing.SketchName})";
                        }
                    }

                }

                Log("✅ Position is legal.");
                return "✅ Position is legal (down side).";
            }
            catch (Exception ex)
            {
                Log($"❌ Exception: {ex.Message}");
                return $"❌ Error in CheckDownPosition: {ex.Message}";
            }
        }

        private static bool IsXOverlap(CabinetInfo a, CabinetInfo b) =>
            a.DistanceX < b.DistanceX + b.Width && a.DistanceX + a.Width > b.DistanceX;

        private static bool IsYOverlap(CabinetInfo a, CabinetInfo b) =>
            a.DistanceY < b.DistanceY + b.Height && a.DistanceY + a.Height > b.DistanceY;

        private static void Log(string msg)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        }
    }
}
