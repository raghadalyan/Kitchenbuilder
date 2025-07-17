using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core
{
    public static class CalculateStationsUpperCabinets
    {
        private static readonly string LogPath = Path.Combine(KitchenConfig.Get().BasePath, "Kitchenbuilder", "Output", "upper", "calculate_stations_debug.txt");

        public static List<UpperCabinetStation> GetStations(string jsonFilePath)
        {
            var stations = new List<UpperCabinetStation>();

            if (!File.Exists(jsonFilePath))
            {
                Log($"❌ JSON file not found: {jsonFilePath}");
                return stations;
            }

            try
            {
                Log($"📥 Reading JSON: {jsonFilePath}");
                string json = File.ReadAllText(jsonFilePath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Read floor width and length
                int floorWidth = 0;
                int floorLength = 0;

                if (root.TryGetProperty("Floor", out var floor))
                {
                    if (floor.TryGetProperty("Width", out var widthProp))
                        floorWidth = widthProp.GetProperty("Size").GetInt32();

                    if (floor.TryGetProperty("Length", out var lengthProp))
                        floorLength = lengthProp.GetProperty("Size").GetInt32();

                    Log($"📏 Floor: Width={floorWidth}, Length={floorLength}");
                }

                foreach (var wallKey in new[] { "Wall1", "Wall2", "Wall3", "Wall4" })
                {
                    if (!root.TryGetProperty(wallKey, out var wall))
                    {
                        Log($"⚠️ Wall not found: {wallKey}");
                        continue;
                    }

                    if (wall.TryGetProperty("Exposed", out var exposed) && exposed.GetBoolean())
                    {
                        Log($"🚫 Skipping exposed wall: {wallKey}");
                        continue;
                    }

                    if (wall.TryGetProperty("Bases", out var bases))
                    {
                        bool hasVisibleBase = bases.EnumerateObject().Any(baseProp =>
                            baseProp.Value.TryGetProperty("Visible", out var visible) && visible.GetBoolean());

                        if (hasVisibleBase)
                        {
                            int wallNum = int.Parse(wallKey.Replace("Wall", ""));
                            int wallLength = (wallNum == 1 || wallNum == 3) ? floorWidth : floorLength;

                            stations.Add(new UpperCabinetStation
                            {
                                WallName = wallKey,
                                WallNumber = wallNum,
                                Length = wallLength
                            });

                            Log($"✅ Added station for: {wallKey} with length {wallLength}");
                        }
                        else
                        {
                            Log($"➖ No visible base on wall: {wallKey}");
                        }
                    }
                    else
                    {
                        Log($"⚠️ No 'Bases' found in {wallKey}");
                    }
                }

                Log($"📊 Total stations found: {stations.Count}");
                return stations;
            }
            catch (Exception ex)
            {
                Log($"❌ Exception while parsing JSON: {ex.Message}");
                return stations;
            }
        }

        private static void Log(string message)
        {
            try
            {
                string folder = Path.GetDirectoryName(LogPath)!;
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"❌ Failed to write to log: {logEx.Message}");
            }
        }
    }
}
