using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core
{
    public static class ApplyUpperCabinet
    {
        private static readonly string SavePath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\UpperCabinets.json";
        private static readonly string StationPathBase = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\Option";
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\upper\apply_upper_cabinet_debug.txt";

        public static string Apply(int optionNum, CabinetInfo cabinet, UpperCabinetStation station)
        {
            try
            {
                // 1. Validate dimensions
                if (cabinet.Width < 5 || cabinet.Depth < 5 || cabinet.Height < 5)
                {
                    string error = $"❌ Cabinet size too small. Width: {cabinet.Width}, Depth: {cabinet.Depth}, Height: {cabinet.Height}. Each must be ≥ 5.";
                    Log(error);
                    return error;
                }

                // 2. Validate position legality
                string legality = CabinetPositionValidator.CheckDownPosition(optionNum, cabinet);
                if (!legality.StartsWith("✅"))
                {
                    Log(legality);
                    return legality;
                }


                string stationJsonPath = $"{StationPathBase}{optionNum}SLD_stations.json";
                var usedSketchNums = new HashSet<int>();

                // 1. Check OptionXSLD_stations.json
                if (File.Exists(stationJsonPath))
                {
                    string json = File.ReadAllText(stationJsonPath);
                    var stations = JsonSerializer.Deserialize<List<StationInfo>>(json) ?? new();

                    foreach (var s in stations.Where(s => s.WallNumber == station.WallNumber))
                    {
                        if (s.Cabinets != null)
                        {
                            foreach (var c in s.Cabinets)
                            {
                                ExtractSketchNumber(c.SketchName, station.WallNumber, usedSketchNums);
                            }
                        }
                    }
                }

                // 2. Check UpperCabinets.json
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    var wallCabinets = JsonSerializer.Deserialize<Dictionary<string, List<CabinetInfo>>>(json);
                    string wallKey = $"Wall{station.WallNumber}";

                    if (wallCabinets != null && wallCabinets.ContainsKey(wallKey))
                    {
                        foreach (var c in wallCabinets[wallKey])
                        {
                            ExtractSketchNumber(c.SketchName, station.WallNumber, usedSketchNums);
                        }
                    }
                }

                // 3. Assign next unused number
                int cabinetNum = 1;
                while (usedSketchNums.Contains(cabinetNum) && cabinetNum <= 19)
                    cabinetNum++;

                cabinet.SketchName = $"Sketch_Cabinet{station.WallNumber}_{cabinetNum}";

                // 4. Save to UpperCabinets.json
                Dictionary<string, List<CabinetInfo>> upperCabinets;

                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    upperCabinets = JsonSerializer.Deserialize<Dictionary<string, List<CabinetInfo>>>(json)
                                    ?? new();
                }
                else
                {
                    upperCabinets = new();
                }

                string wallKeyFinal = $"Wall{station.WallNumber}";
                if (!upperCabinets.ContainsKey(wallKeyFinal))
                    upperCabinets[wallKeyFinal] = new();

                upperCabinets[wallKeyFinal].Add(cabinet);

                File.WriteAllText(SavePath, JsonSerializer.Serialize(upperCabinets, new JsonSerializerOptions { WriteIndented = true }));

                string successMsg = "✅ Cabinet created successfully.";
                Log(successMsg);
                return successMsg;
            }
            catch (Exception ex)
            {
                string error = $"❌ Error in Apply: {ex.Message}";
                Log(error);
                return error;
            }
        }

        private static void ExtractSketchNumber(string sketchName, int wallNum, HashSet<int> used)
        {
            if (sketchName.StartsWith($"Sketch_Cabinet{wallNum}_"))
            {
                string[] parts = sketchName.Split('_');
                if (parts.Length == 3 && int.TryParse(parts[2], out int num))
                    used.Add(num);
            }
        }

        private static void Log(string msg)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        }
    }
}
