using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core
{
    public static class CalculateStationsCabinets
    {
        private static void Log(string message)
        {
            string debugPath = Path.Combine(KitchenConfig.Get().BasePath, "Kitchenbuilder", "Output", "Debug", "Station_Calculation_Debug.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(debugPath)!);
            File.AppendAllText(debugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        public static void ProcessAll()
        {
            string folderPath = Path.Combine(KitchenConfig.Get().BasePath, "Kitchenbuilder", "Kitchenbuilder", "JSON");
            var files = Directory.GetFiles(folderPath, "Option*SLD.json");
            Log($"📁 Found {files.Length} files to process in {folderPath}");

            foreach (var file in files)
            {
                Log($"📄 Processing file: {file}");
                var stations = Calculate(file);
                Log($"✅ Calculated {stations.Count} stations for {Path.GetFileName(file)}");

                var outputPath = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file) + "_stations.json");

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(outputPath, JsonSerializer.Serialize(stations, options));
                Log($"💾 Saved stations to: {outputPath}");
            }
        }

        public static List<StationInfo> Calculate(string jsonPath)
        {
            var result = new List<StationInfo>();

            try
            {
                var json = File.ReadAllText(jsonPath);
                var root = JsonNode.Parse(json)?.AsObject();

                if (root == null)
                {
                    Log($"❌ Failed to parse root JSON object in file: {jsonPath}");
                    return result;
                }

                foreach (var wallKey in new[] { "Wall1", "Wall2", "Wall3", "Wall4" })
                {
                    if (!root.ContainsKey(wallKey)) continue;

                    int wallNumber = int.Parse(wallKey.Replace("Wall", ""));
                    var wall = root[wallKey]?.AsObject();
                    var bases = wall?["Bases"]?.AsObject();
                    if (bases == null) continue;

                    foreach (var baseEntry in bases)
                    {
                        var baseObj = baseEntry.Value?.AsObject();
                        if (baseObj == null || baseObj["Visible"]?.GetValue<bool>() != true)
                        {
                            Log($"❌ Skipping hidden or null base: {baseEntry.Key}");
                            continue;
                        }

                        string sketchName = baseObj["SketchName"]?.ToString() ?? "";
                        string extrudeName = baseObj["ExtrudeName"]?.ToString() ?? "";

                        if (sketchName.Contains("fridge_base"))
                        {
                            Log($"⛔ Skipping fridge base: {sketchName}");
                            continue;
                        }

                        var smartDims = baseObj["SmartDim"]?.AsArray();
                        int distanceX = smartDims?.FirstOrDefault(d => d?["Name"]?.ToString().StartsWith("DistanceX") == true)?["Size"]?.GetValue<int>() ?? 0;
                        int length = smartDims?.FirstOrDefault(d => d?["Name"]?.ToString().StartsWith("length") == true)?["Size"]?.GetValue<int>() ?? 0;

                        var countertop = baseObj["Countertop"]?.AsObject();
                        string baseName = baseEntry.Key;

                        int totalStart = distanceX;
                        int totalEnd = distanceX + length;

                        if (countertop == null || countertop.Count == 0)
                        {
                            result.Add(new StationInfo
                            {
                                BaseName = baseName,
                                ExtrudeName = extrudeName,
                                StationStart = totalStart,
                                StationEnd = totalEnd,
                                WallNumber = wallNumber,
                                HasCountertop = false,
                                SketchName = sketchName
                            });
                            Log($"➕ Added full base station: {baseName}, Start={totalStart}, End={totalEnd}, CT=false");
                        }
                        else
                        {
                            int L = countertop?["L"]?.GetValue<int>() ?? 0;
                            int R = countertop?["R"]?.GetValue<int>() ?? 0;

                            if (L == 0 && R == 0)
                            {
                                result.Add(new StationInfo
                                {
                                    BaseName = baseName,
                                    ExtrudeName = extrudeName,
                                    StationStart = totalStart,
                                    StationEnd = totalEnd,
                                    WallNumber = wallNumber,
                                    HasCountertop = true,
                                    SketchName = sketchName
                                });
                                Log($"➕ Added full countertop station: {baseName}, Start={totalStart}, End={totalEnd}, CT=true");
                            }
                            else
                            {
                                if (L > 0)
                                {
                                    result.Add(new StationInfo
                                    {
                                        BaseName = baseName,
                                        ExtrudeName = extrudeName,
                                        StationStart = totalStart,
                                        StationEnd = totalStart + L,
                                        WallNumber = wallNumber,
                                        HasCountertop = false,
                                        SketchName = sketchName
                                    });
                                    Log($"➕ Left base part: {baseName}, Start={totalStart}, End={totalStart + L}, CT=false");
                                }

                                result.Add(new StationInfo
                                {
                                    BaseName = baseName,
                                    ExtrudeName = extrudeName,
                                    StationStart = totalStart + L,
                                    StationEnd = totalEnd - R,
                                    WallNumber = wallNumber,
                                    HasCountertop = true,
                                    SketchName = sketchName
                                });
                                Log($"➕ Center countertop: {baseName}, Start={totalStart + L}, End={totalEnd - R}, CT=true");

                                if (R > 0)
                                {
                                    result.Add(new StationInfo
                                    {
                                        BaseName = baseName,
                                        ExtrudeName = extrudeName,
                                        StationStart = totalEnd - R,
                                        StationEnd = totalEnd,
                                        WallNumber = wallNumber,
                                        HasCountertop = false,
                                        SketchName = sketchName
                                    });
                                    Log($"➕ Right base part: {baseName}, Start={totalEnd - R}, End={totalEnd}, CT=false");
                                }
                            }
                        }
                    }
                }

                Log($"📊 Total stations for {Path.GetFileName(jsonPath)}: {result.Count}");
            }
            catch (Exception ex)
            {
                Log($"❌ Exception while calculating stations for {jsonPath}: {ex.Message}");
            }

            return result;
        }
    }
}
