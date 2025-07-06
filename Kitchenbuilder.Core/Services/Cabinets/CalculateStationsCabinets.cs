using System.Text.Json;
using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core
{


    public static class CalculateStationsCabinets
    {
        public static void ProcessAll()
        {
            string folderPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON";
            var files = Directory.GetFiles(folderPath, "Option*SLD.json");

            foreach (var file in files)
            {
                var stations = Calculate(file);
                var outputPath = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file) + "_stations.json");

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(outputPath, JsonSerializer.Serialize(stations, options));
            }
        }

        public static List<StationInfo> Calculate(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            var root = JsonNode.Parse(json)?.AsObject();
            var result = new List<StationInfo>();

            if (root == null) return result;

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
                    if (baseObj == null || baseObj["Visible"]?.GetValue<bool>() != true) continue;

                    string sketchName = baseObj["SketchName"]?.ToString() ?? "";
                    string extrudeName = baseObj["ExtrudeName"]?.ToString() ?? ""; // <-- Add this line

                    if (sketchName.Contains("fridge_base")) continue;

                    var smartDims = baseObj["SmartDim"]?.AsArray();
                    int distanceX = smartDims?.FirstOrDefault(d => d?["Name"]?.ToString().StartsWith("DistanceX") == true)?["Size"]?.GetValue<int>() ?? 0;
                    int length = smartDims?.FirstOrDefault(d => d?["Name"]?.ToString().StartsWith("length") == true)?["Size"]?.GetValue<int>() ?? 0;

                    var countertop = baseObj["Countertop"]?.AsObject(); // ✅ CORRECT: it's a JsonObject
                    string baseName = baseEntry.Key;

                    int totalStart = distanceX;
                    int totalEnd = distanceX + length;

                    // Add to each StationInfo the extrudeName
                    if (countertop == null || countertop.Count == 0)
                    {
                        result.Add(new StationInfo
                        {
                            BaseName = baseName,
                            ExtrudeName = extrudeName, // <-- add this
                            StationStart = totalStart,
                            StationEnd = totalEnd,
                            WallNumber = wallNumber,
                            HasCountertop = false,
                            SketchName = sketchName
                        });
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
                            }
                        }
                    }
                }

            }

            return result;
        }




    }
}
