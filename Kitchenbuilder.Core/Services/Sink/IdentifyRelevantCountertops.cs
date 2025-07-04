using System.Text.Json.Nodes;
using System.Text.Json;

namespace Kitchenbuilder.Core
{
    public static class IdentifyRelevantCountertops
    {
        private static readonly string DebugPath = @"C:\\Users\\chouse\\Downloads\\Kitchenbuilder\\Output\\Sink-Cooktop\\debug.txt";

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!); // ✅ Ensure folder exists
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }


        public static void Process(int optionNum)
        {
            string jsonFolder = @"C:\\Users\\chouse\\Downloads\\Kitchenbuilder\\Kitchenbuilder\\JSON";
            string sldPath = Path.Combine(jsonFolder, $"Option{optionNum}SLD.json");
            string metaPath = Path.Combine(jsonFolder, $"Option{optionNum}.json");

            Log($"▶️ Processing Option {optionNum}");

            if (!File.Exists(sldPath) || !File.Exists(metaPath))
            {
                Log("❌ One or both JSON files not found.");
                return;
            }

            var sldJson = JsonNode.Parse(File.ReadAllText(sldPath))!.AsObject();
            var metaJson = JsonNode.Parse(File.ReadAllText(metaPath))!.AsObject();

            var relevant = new List<string>();

            HashSet<int> cornerWalls = new();
            if (metaJson["Corner"] is JsonArray corners && corners[0] is JsonArray pair)
            {
                cornerWalls.Add(pair[0]!.GetValue<int>());
                cornerWalls.Add(pair[1]!.GetValue<int>());
                Log($"Corner walls found: [{string.Join(",", cornerWalls)}]");
            }
            int maxCornerWall = cornerWalls.Count > 0 ? cornerWalls.Max() : -1;
            bool isSpecialCase14 = cornerWalls.SetEquals([1, 4]);

            for (int wall = 1; wall <= 4; wall++)
            {
                string wallKey = $"Wall{wall}";
                if (!sldJson.ContainsKey(wallKey)) continue;

                var wallObj = sldJson[wallKey]!.AsObject();
                var bases = wallObj["Bases"]?.AsObject();
                if (bases == null) continue;

                int baseIndex = 1;
                foreach (var basePair in bases)
                {
                    var baseObj = basePair.Value?.AsObject();
                    if (baseObj == null || baseObj["Visible"]?.GetValue<bool>() != true) continue;
                    if (baseObj["Countertop"] == null) continue;

                    double start = baseObj["Start"]?.GetValue<double>() ?? 0;
                    double end = baseObj["End"]?.GetValue<double>() ?? 0;
                    var ct = baseObj["Countertop"]!.AsObject();

                    double L = ct["L"]?.GetValue<double>() ?? 0;
                    double R = ct["R"]?.GetValue<double>() ?? 0;

                    bool isFirstBase = baseIndex == 1;
                    bool cornerForceStartZero = cornerWalls.Contains(wall) && wall == maxCornerWall && !isSpecialCase14 && isFirstBase;
                    bool exposedForceStartZero = wallObj["Exposed"]?.GetValue<bool>() == true && isFirstBase;

                    double newStart = cornerForceStartZero || exposedForceStartZero ? 0 : start + L;
                    double newEnd = end - R;
                    double width = newEnd - newStart;

                    ct["Start"] = newStart;
                    ct["End"] = newEnd;

                    Log($"✔️ Wall{wall} {basePair.Key}: Start={newStart}, End={newEnd}, Width={width}");

                    if (width >= 150)
                    {
                        string entry = $"Wall{wall}.{basePair.Key} (Width={width})";
                        relevant.Add(entry);
                    }

                    baseIndex++;
                }
            }

            File.WriteAllText(sldPath, JsonSerializer.Serialize(sldJson, new JsonSerializerOptions { WriteIndented = true }));
            Log("✅ JSON update complete.");

            if (relevant.Count > 0)
                Log($"👉 Relevant countertops: {string.Join(", ", relevant)}");
            else
                Log("⚠️ No countertops with width >= 150 found.");
        }

    }
}
