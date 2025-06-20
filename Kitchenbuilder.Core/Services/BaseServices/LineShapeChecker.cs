using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Kitchenbuilder.Core
{
    public static class LineShapeChecker
    {
        public static bool EvaluateWall(
            Kitchen kitchen,
            int wallIndex,
            List<(double start, double end)> spacesWall,
            double floorLength,
            string outputPath,
            bool exposed,
            bool corner,
            int fridgeWall,
            double fridgeStart,
            double fridgeEnd)
        {
            const double totalRequiredLength = 325;
            const double secondarySpaceThreshold = 240;

            var windows = kitchen.Walls[wallIndex].Windows ?? new List<Window>();

            // 1. Check if there's a window in the fridge placement range
            if (HasWindowInRange(windows, fridgeStart, fridgeEnd))
            {
                LogDebug($"❌ Window found in fridge range {fridgeStart}-{fridgeEnd}");
                return false;
            }

            // 2. Find the space the fridge is in
            var mainSpace = spacesWall.FirstOrDefault(s => s.start <= fridgeStart && s.end >= fridgeEnd);
            if (mainSpace == default)
            {
                LogDebug("❌ Fridge does not lie within any empty segment.");
                return false;
            }

            double mainSpaceLength = mainSpace.end - mainSpace.start;

            // 3. Check if main space is enough (>= 325)
            if (mainSpaceLength >= totalRequiredLength)
            {
                SaveOption1Json(spacesWall, fridgeStart, fridgeEnd, wallIndex);
                LogDebug($"✅ Main space ({mainSpace.start}-{mainSpace.end}) is sufficient ({mainSpaceLength} cm)");
                return true;
            }

            // 4. Otherwise check if there's another space >= 240
            var secondary = spacesWall.FirstOrDefault(s => s != mainSpace && (s.end - s.start) >= secondarySpaceThreshold);
            if (secondary != default)
            {
                SaveOption1Json(spacesWall, fridgeStart, fridgeEnd, wallIndex);
                LogDebug($"✅ Secondary space found ({secondary.start}-{secondary.end}) with length {(secondary.end - secondary.start)} cm");
                return true;
            }

            LogDebug("❌ No valid main or secondary space found for line shape.");
            return false;
        }

        private static bool HasWindowInRange(List<Window> windows, double from, double to)
        {
            return windows.Any(w =>
                (w.DistanceX >= from && w.DistanceX <= to) ||
                (w.DistanceX + w.Width >= from && w.DistanceX + w.Width <= to));
        }

        private static void SaveOption1Json(List<(double start, double end)> wallSpaces, double fridgeStart, double fridgeEnd, int wallIndex)
        {
            var option = new
            {
                Title = "Line Shape",
                Wall1 = wallIndex + 1,
                SpacesWall1 = wallSpaces.Select(s => new { Start = s.start, End = s.end }).ToList(),
                FridgeWall = wallIndex + 1,
                Fridge = new { Start = fridgeStart, End = fridgeEnd },
                Corner = false,
                Exposed = false
            };

            string folder = @"C:\\Users\\chouse\\Downloads\\Kitchenbuilder\\Kitchenbuilder\\JSON";
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "Option1.json");
            string json = JsonSerializer.Serialize(option, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private static void LogDebug(string message)
        {
            string path = @"C:\\Users\\chouse\\Downloads\\Kitchenbuilder\\Output\\LineShapeChecker.txt";
            File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
    }
}
