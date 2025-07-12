using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Kitchenbuilder.Core.Models;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
namespace Kitchenbuilder.Core
{
    public static class ApplyUpperCabinet
    {
        private static readonly string SavePath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\UpperCabinets.json";
        private static readonly string StationPathBase = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\Option";
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\upper\apply_upper_cabinet_debug.txt";

        public static string Apply(IModelDoc2 model, int optionNum, CabinetInfo baseCabinet, UpperCabinetStation station, int copiesCount, string sequenceDirection)
        {
            try
            {
                if (baseCabinet.Width < 5 || baseCabinet.Depth < 5 || baseCabinet.Height < 5)
                {
                    string error = $"❌ Cabinet size too small. Width: {baseCabinet.Width}, Depth: {baseCabinet.Depth}, Height: {baseCabinet.Height}. Each must be ≥ 5.";
                    Log(error);
                    return error;
                }

                string stationJsonPath = $"{StationPathBase}{optionNum}SLD_stations.json";
                var usedSketchNums = new HashSet<int>();

                // Load used sketch numbers
                if (File.Exists(stationJsonPath))
                {
                    string json = File.ReadAllText(stationJsonPath);
                    var stations = JsonSerializer.Deserialize<List<StationInfo>>(json) ?? new();

                    foreach (var s in stations.Where(s => s.WallNumber == station.WallNumber && s.Cabinets != null))
                        foreach (var c in s.Cabinets)
                            ExtractSketchNumber(c.SketchName, station.WallNumber, usedSketchNums);
                }

                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    var wallCabinets = JsonSerializer.Deserialize<Dictionary<string, List<CabinetInfo>>>(json);
                    string wallKey = $"Wall{station.WallNumber}";

                    if (wallCabinets != null && wallCabinets.ContainsKey(wallKey))
                        foreach (var c in wallCabinets[wallKey])
                            ExtractSketchNumber(c.SketchName, station.WallNumber, usedSketchNums);
                }

                // 2. Generate all cabinets with calculated positions
                List<CabinetInfo> newCabinets = new();

                for (int i = 0; i < copiesCount; i++)
                {
                    var cabinet = new CabinetInfo
                    {
                        Width = baseCabinet.Width,
                        Height = baseCabinet.Height,
                        Depth = baseCabinet.Depth,
                        HasDrawers = false,
                        Drawers = null,
                        DistanceX = baseCabinet.DistanceX + (sequenceDirection == "Horizontal" ? i * baseCabinet.Width : 0),
                        DistanceY = baseCabinet.DistanceY + (sequenceDirection == "Vertical" ? i * baseCabinet.Height : 0)
                    };

                    // Validate
                    string legality = CabinetPositionValidator.CheckDownPosition(optionNum, station.WallNumber, cabinet);
                    if (!legality.StartsWith("✅"))
                    {
                        string failMsg = $"❌ Copy {i + 1} failed: {legality}";
                        Log(failMsg);
                        return failMsg;
                    }

                    // Assign sketch name
                    int cabinetNum = 1;
                    while (usedSketchNums.Contains(cabinetNum) && cabinetNum <= 99)
                        cabinetNum++;

                    cabinet.SketchName = $"Sketch_Cabinet{station.WallNumber}_{cabinetNum}";
                    usedSketchNums.Add(cabinetNum);
                    newCabinets.Add(cabinet);
                }

                // 3. Save if all legal
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

                upperCabinets[wallKeyFinal].AddRange(newCabinets);

                File.WriteAllText(SavePath, JsonSerializer.Serialize(upperCabinets, new JsonSerializerOptions { WriteIndented = true }));
                // 4. Apply dimensions in SolidWorks
                var tempStation = new StationInfo
                {
                    WallNumber = station.WallNumber,

                    Cabinets = newCabinets
                };

                ApplyCabinetDimensions.Apply(model, new List<StationInfo> { tempStation });

                string successMsg = $"✅ {copiesCount} cabinet(s) created and applied successfully.";
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
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {msg}{System.Environment.NewLine}");
        }
    }
}
