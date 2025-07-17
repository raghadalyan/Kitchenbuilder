using System.Text.Json.Nodes;
using System.Text.Json;
using Kitchenbuilder.Models;
/*
 * IdentifyRelevantCountertops.cs
 * ------------------------------
 * This class processes Option{X}SLD.json and Option{X}.json to identify countertops 
 * that are wide enough (width ≥ 150 cm) to host a sink or cooktop.
 *
 * Key Responsibilities:
 * - Adjusts the `Start` and `End` positions of each countertop based on:
 *     • Whether it's the first base on an exposed wall.
 *     • Whether it's part of a corner layout.
 *     • Special handling for Wall4 when its last base ends at the floor length.
 * - Includes countertop only if:
 *     • It's marked visible.
 *     • It has a `Countertop` section.
 *     • The final computed width is ≥ 150 cm.
 * - Writes updated `Start` and `End` positions back to the Option{X}SLD.json.
 * - Logs all decisions and selected countertops to: Sink-Cooktop/debug.txt
 *
 * Returns:
 * A list of `Countertop` objects that are eligible for further sink/cooktop analysis.
 */

namespace Kitchenbuilder.Core
{
    public static class IdentifyRelevantCountertops
    {
        private static readonly string DebugPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Output", "Sink-Cooktop", "debug.txt"
        );

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }

        public static List<Countertop> Process(int optionNum)


        {
            string jsonFolder = Path.Combine(
                KitchenConfig.Get().BasePath,
                "Kitchenbuilder", "Kitchenbuilder", "JSON"
            );
            string sldPath = Path.Combine(jsonFolder, $"Option{optionNum}SLD.json");
            string metaPath = Path.Combine(jsonFolder, $"Option{optionNum}.json");

            Log($"▶️ Processing Option {optionNum}");

            if (!File.Exists(sldPath) || !File.Exists(metaPath))
            {
                Log("❌ One or both JSON files not found.");
                return new List<Countertop>();
            }

            var sldJson = JsonNode.Parse(File.ReadAllText(sldPath))!.AsObject();
            var metaJson = JsonNode.Parse(File.ReadAllText(metaPath))!.AsObject();
            var relevant = new List<Countertop>();

            HashSet<int> cornerWalls = new();
            bool specialCorner14 = false;
            int maxCorner = -1;

            if (metaJson["Corner"] is JsonArray corners && corners[0] is JsonArray pair)
            {
                int a = pair[0]!.GetValue<int>();
                int b = pair[1]!.GetValue<int>();
                cornerWalls.Add(a);
                cornerWalls.Add(b);
                maxCorner = Math.Max(a, b);
                specialCorner14 = (a == 1 && b == 4) || (a == 4 && b == 1);
                Log($"Corner walls found: [{string.Join(",", cornerWalls)}], Special14={specialCorner14}");
            }

            bool forceWall1And2 = false;
            double floorLength = sldJson["Floor"]?["Length"]?["Size"]?.GetValue<double>() ?? -1;

            if (sldJson["Wall4"]?["Exposed"]?.GetValue<bool>() == true)
            {
                var basesW4 = sldJson["Wall4"]?["Bases"]?.AsObject();
                if (basesW4 != null)
                {
                    var lastVisibleBase = basesW4
                        .Select(kv => kv.Value?.AsObject())
                        .Where(x => x != null && x["Visible"]?.GetValue<bool>() == true)
                        .OrderByDescending(x => x["End"]?.GetValue<double>() ?? 0)
                        .FirstOrDefault();

                    double lastEnd = lastVisibleBase?["End"]?.GetValue<double>() ?? -2;

                    if (Math.Abs(lastEnd - floorLength) < 0.001)
                    {
                        forceWall1And2 = true;
                        Log("🟡 Wall4 is exposed and last base ends at floor length. Treat Wall1 & Wall2 first base as Start=0");
                    }
                }
            }

            for (int wall = 1; wall <= 4; wall++)
            {
                string wallKey = $"Wall{wall}";
                if (!sldJson.ContainsKey(wallKey)) continue;

                var wallObj = sldJson[wallKey]!.AsObject();
                var bases = wallObj["Bases"]?.AsObject();
                if (bases == null) continue;

                bool isExposed = wallObj["Exposed"]?.GetValue<bool>() == true;
                bool isCornerMax = cornerWalls.Contains(wall) && wall == maxCorner && !specialCorner14;

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

                    // ✅ Handle special case: preserve Wall4 first base Start/End if L==0, R==0, and End==floorLength
                    bool isWall4ExactFit = wall == 4 && isExposed && isFirstBase &&
                                           Math.Abs(end - floorLength) < 0.001 &&
                                           L == 0 && R == 0;

                    double newStart = isWall4ExactFit ? start : (isFirstBase && (
                        (isExposed && wall != 4) || isCornerMax || (forceWall1And2 && (wall == 1 || wall == 2)))
                        ? 0 : start + L);

                    double newEnd = isWall4ExactFit ? end : end - R;
                    double width = newEnd - newStart;

                    ct["Start"] = newStart;
                    ct["End"] = newEnd;

                    Log($"✔️ Wall{wall} {basePair.Key}: Start={newStart}, End={newEnd}, Width={width}");
                    if (width >= 150)
                    {
                        relevant.Add(new Countertop
                        {
                            BaseKey = basePair.Key,
                            Start = newStart,
                            End = newEnd,
                            WallNumber = wall
                        });
                    }


                    baseIndex++;
                }
            }

            File.WriteAllText(sldPath, JsonSerializer.Serialize(sldJson, new JsonSerializerOptions { WriteIndented = true }));
            Log("✅ JSON update complete.");

            if (relevant.Count > 0)
                Log($"👉 Relevant countertops: {string.Join(", ", relevant.Select(c => $"Wall{c.WallNumber}.{c.BaseKey} [{c.Start}-{c.End}]"))}");
            else
                Log("⚠️ No countertops with width >= 150 found.");
            return relevant;

        }

    }
}
