using System;
using System.IO;
using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core
{
    public static class WindowRangeChecker
    {
        private static readonly string InputJsonPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Kitchenbuilder", "JSON", "input.json"
        );

        private static readonly string OutputLogPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Output", "Sink-Cooktop", "WindowRangeChecker.txt"
        );

        private static void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(OutputLogPath)!);
                File.AppendAllText(OutputLogPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to write to WindowRangeChecker.txt: {ex.Message}");
            }
        }

        public static bool IsWindowInRange(double start, double end, int wallNumber)
        {
            Log($"🔍 Checking Wall {wallNumber} countertop range [{start}, {end}]");

            if (!File.Exists(InputJsonPath))
            {
                Log("❌ input.json not found.");
                return false;
            }

            var inputJson = JsonNode.Parse(File.ReadAllText(InputJsonPath))?.AsObject();
            if (inputJson == null || !inputJson.ContainsKey("Walls"))
            {
                Log("❌ input.json structure is invalid.");
                return false;
            }

            var walls = inputJson["Walls"]?.AsArray();
            if (walls == null || wallNumber < 1 || wallNumber > walls.Count)
            {
                Log($"❌ Wall {wallNumber} not found in Walls array.");
                return false;
            }

            var wall = walls[wallNumber - 1]?.AsObject();
            if (wall == null || wall["HasWindows"]?.GetValue<bool>() != true)
            {
                Log($"ℹ️ Wall {wallNumber} has no windows.");
                return false;
            }

            var windows = wall["Windows"]?.AsArray();
            if (windows == null)
            {
                Log($"ℹ️ Wall {wallNumber} has a null windows array.");
                return false;
            }

            foreach (var window in windows)
            {
                double windowStart = window?["DistanceX"]?.GetValue<double>() ?? -1;
                double windowWidth = window?["Width"]?.GetValue<double>() ?? -1;
                double windowEnd = windowStart + windowWidth;

                Log($"🪟 Window: [{windowStart}, {windowEnd}]");

                if (windowStart < end && windowEnd > start)
                {
                    Log("✅ Window overlaps with countertop range.");
                    return true;
                }
            }

            Log("❌ No overlapping windows found.");
            return false;
        }
        public static int CountWindowsInRange(double start, double end, int wallNumber)
        {
            int count = 0;

            string inputPath = Path.Combine(
                KitchenConfig.Get().BasePath,
                "Kitchenbuilder", "Kitchenbuilder", "JSON", "input.json"
            );


            if (!File.Exists(inputPath))
                return 0;

            var inputJson = JsonNode.Parse(File.ReadAllText(inputPath))?.AsObject();
            var walls = inputJson?["Walls"]?.AsArray();
            if (walls == null || wallNumber < 1 || wallNumber > walls.Count)
                return 0;

            var wall = walls[wallNumber - 1]?.AsObject();
            if (wall == null || wall["HasWindows"]?.GetValue<bool>() != true)
                return 0;

            var windows = wall["Windows"]?.AsArray();
            if (windows == null) return 0;

            foreach (var window in windows)
            {
                double windowStart = window?["DistanceX"]?.GetValue<double>() ?? -1;
                double windowWidth = window?["Width"]?.GetValue<double>() ?? -1;
                double windowEnd = windowStart + windowWidth;

                if (windowStart < end && windowEnd > start)
                    count++;
            }

            return count;
        }

    }
}
