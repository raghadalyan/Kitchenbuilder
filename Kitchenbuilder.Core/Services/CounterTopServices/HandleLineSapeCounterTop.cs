using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core
{
    public static class HandleLineSapeCounterTop
    {
        private static readonly string JsonPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\Option1SLD.json";
        private static readonly string DebugOutputPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\countertop_debug\countertop_debug.txt";

        public static void Process()
        {
            try
            {
                if (!File.Exists(JsonPath))
                {
                    Console.WriteLine("❌ Option1SLD.json not found.");
                    return;
                }

                var json = File.ReadAllText(JsonPath);
                var root = JsonNode.Parse(json).AsObject();

                string usableWall = "";
                string largestBaseKey = "";
                JsonObject? largestBase = null;
                double maxLength = -1;

                for (int i = 1; i <= 4; i++)
                {
                    string wallKey = $"Wall{i}";
                    if (!root.ContainsKey(wallKey)) continue;

                    var wall = root[wallKey]?.AsObject();
                    if (wall == null || !wall.ContainsKey("Bases")) continue;

                    var bases = wall["Bases"]?.AsObject();
                    if (bases == null) continue;

                    foreach (var baseEntry in bases)
                    {
                        var baseData = baseEntry.Value?.AsObject();
                        if (baseData == null || baseData["Visible"]?.GetValue<bool>() != true)
                            continue;

                        var smartDims = baseData["SmartDim"]?.AsArray();
                        if (smartDims == null) continue;

                        foreach (var dim in smartDims)
                        {
                            var dimObj = dim?.AsObject();
                            if (dimObj != null && dimObj["Name"]?.GetValue<string>()?.StartsWith("length@") == true)
                            {
                                double length = dimObj["Size"]?.GetValue<double>() ?? 0;
                                if (length > maxLength)
                                {
                                    maxLength = length;
                                    largestBaseKey = baseEntry.Key;
                                    largestBase = baseData;
                                    usableWall = wallKey;
                                }
                            }
                        }
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(DebugOutputPath)!);
                using StreamWriter writer = new(DebugOutputPath, append: false);

                if (largestBase == null || string.IsNullOrEmpty(usableWall))
                {
                    writer.WriteLine("❌ No usable wall with valid visible base found.");
                    return;
                }

                writer.WriteLine($"✅ Usable wall: {usableWall}");
                writer.WriteLine($"📦 Largest base: {largestBaseKey}");
                writer.WriteLine($"📏 Length: {maxLength} cm");

                if (maxLength < 240)
                {
                    writer.WriteLine("❌ Base is too small for a countertop (minimum 240 cm).");
                    return;
                }

                // Create CounterTop if not present
                if (!largestBase.ContainsKey("CounterTop"))
                {
                    string sketchName = largestBase["SketchName"]?.GetValue<string>() ?? "unknown";
                    var ctObject = new JsonObject
                    {
                        ["Name"] = $"CT_{sketchName}",
                        ["RightName"] = $"R@CT_{sketchName}",
                        ["SizeRightName"] = 0,
                        ["LeftName"] = $"L@CT_{sketchName}",
                        ["SizeLeftName"] = 0
                    };
                    largestBase["CounterTop"] = ctObject;
                    writer.WriteLine($"⚠️ CounterTop info was missing. Added default CounterTop for {sketchName}.");
                }

                var ct = largestBase["CounterTop"]?.AsObject();
                if (ct == null)
                {
                    writer.WriteLine("⚠️ CounterTop section is malformed.");
                    return;
                }

                double extra = maxLength - 240;
                if (extra < 45)
                {
                    ct["SizeLeftName"] = 0;
                    ct["SizeRightName"] = 0;
                    writer.WriteLine("ℹ️ Extra space < 45 cm → Left/Right sizes set to 0.");
                }
                else
                {
                    writer.WriteLine($"ℹ️ Extra space is {extra} cm — countertop side sizes remain unchanged.");
                }

                // Save updated JSON
                File.WriteAllText(JsonPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine("✅ Countertop logic completed and JSON updated.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in HandleLineSapeCounterTop: {ex.Message}");
            }
        }
    }
}
