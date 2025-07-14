using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core
{
    public static class SpacePositionValidator
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\upper\validator_space_debug.txt";

        public static string CheckDownPosition(int optionNum, int wallNumber, Space space)
        {
            try
            {
                Log($"🧱 Checking SPACE on Wall {wallNumber}: (X={space.DistanceX}, Y={space.DistanceY}, W={space.Width}, H={space.Height})");

                string basePath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON";
                string optionPath = Path.Combine(basePath, $"Option{optionNum}SLD.json");
                string stationPath = Path.Combine(basePath, $"Option{optionNum}SLD_stations.json");
                string upperPath = Path.Combine(basePath, "UpperCabinets.json");

                string wallKey = $"Wall{wallNumber}";

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
                        if (!baseObj.GetProperty("Visible").GetBoolean()) continue;

                        double start = baseObj.GetProperty("Start").GetDouble();
                        double end = baseObj.GetProperty("End").GetDouble();

                        if (space.DistanceX >= start && space.DistanceX < end)
                        {
                            string sketch = baseObj.GetProperty("SketchName").GetString();
                            Log($"🔍 SPACE overlaps base: {sketch}");

                            if (sketch.Contains("fridge") && space.DistanceY < 182)
                                return "❌ Space too close to fridge base. DistanceY must be ≥ 182 cm.";

                            if (baseObj.TryGetProperty("Countertop", out var ct))
                            {
                                double ctStart = ct.GetProperty("Start").GetDouble();
                                double ctEnd = ct.GetProperty("End").GetDouble();

                                if (space.DistanceX >= ctStart && space.DistanceX < ctEnd && space.DistanceY < 150)
                                    return "❌ Space too close to countertop. DistanceY must be ≥ 150 cm.";
                            }
                        }
                    }
                }

                // 2. Check overlap with station cabinets
                if (File.Exists(stationPath))
                {
                    Log($"📄 Checking station overlaps from: {stationPath}");
                    var stations = JsonSerializer.Deserialize<List<StationInfo>>(File.ReadAllText(stationPath));
                    foreach (var station in stations.Where(s => s.WallNumber == wallNumber && s.Cabinets != null))
                    {
                        foreach (var existing in station.Cabinets)
                        {
                            Log($"↔ Compare with station cabinet {existing.SketchName} at (X={existing.DistanceX}, Y={existing.DistanceY})");
                            if (IsXOverlap(space, existing) && IsYOverlap(space, existing))
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
                            if (IsXOverlap(space, existing) && IsYOverlap(space, existing))
                                return $"❌ Overlaps with cabinet in UpperCabinets.json (Sketch: {existing.SketchName})";
                        }
                    }
                }

                Log("✅ Position is legal.");
                return "✅ Position is legal (space check).";
            }
            catch (Exception ex)
            {
                Log($"❌ Exception: {ex.Message}");
                return $"❌ Error in CheckDownPositionForSpace: {ex.Message}";
            }
        }

        private static bool IsXOverlap(Space s, CabinetInfo c) =>
            s.DistanceX < c.DistanceX + c.Width && s.DistanceX + s.Width > c.DistanceX;

        private static bool IsYOverlap(Space s, CabinetInfo c) =>
            s.DistanceY < c.DistanceY + c.Height && s.DistanceY + s.Height > c.DistanceY;

        private static void Log(string msg)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        }
    }
}
