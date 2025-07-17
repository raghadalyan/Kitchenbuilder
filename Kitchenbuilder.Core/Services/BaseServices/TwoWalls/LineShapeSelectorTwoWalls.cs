using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kitchenbuilder.Core
{
    public static class LineShapeSelectorTwoWalls
    {
        const double fridgeWidth = 85;
        const double requiredWidthStraight = 325;
        private static readonly string outputPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Kitchenbuilder", "JSON", "Option1.json"
        );

        public static bool TryFindWallForLineShape(
            Kitchen kitchen,
            Dictionary<int, List<(double start, double end)>> simpleEmptySpaces)
        {
            // Flatten all segments with wall index reference
            var allSpaces = simpleEmptySpaces
                .SelectMany(kvp => kvp.Value.Select(seg => (wallIndex: kvp.Key, seg.start, seg.end)))
                .Where(s => s.end - s.start >= fridgeWidth)
                .OrderByDescending(s => s.end - s.start)
                .ToList();

            foreach (var (wallIndex, start, end) in allSpaces)
            {
                double floorLength = kitchen.Floor.Length;
                var wallSpaces = simpleEmptySpaces[wallIndex];

                // Try placing fridge at the start of the segment
                double fridgeStart = start;
                double fridgeEnd = start + fridgeWidth;

                bool success = LineShapeChecker.EvaluateWall(
                    kitchen, wallIndex, wallSpaces, floorLength,
                    outputPath, exposed: false, corner: false,
                    fridgeWall: wallIndex + 1, fridgeStart: fridgeStart, fridgeEnd: fridgeEnd);

                if (success)
                {
                    LogDebug($"✅ Fridge placed at start of segment {start}-{end} on wall {wallIndex}");
                    return true;
                }

                // Try placing fridge at the end of the segment
                fridgeStart = end - fridgeWidth;
                fridgeEnd = end;

                success = LineShapeChecker.EvaluateWall(
                    kitchen, wallIndex, wallSpaces, floorLength,
                    outputPath, exposed: false, corner: false,
                    fridgeWall: wallIndex + 1, fridgeStart: fridgeStart, fridgeEnd: fridgeEnd);

                if (success)
                {
                    LogDebug($"✅ Fridge placed at end of segment {start}-{end} on wall {wallIndex}");
                    return true;
                }
            }

            LogDebug("❌ No valid line shape found across two walls.");
            return false;
        }

        private static void LogDebug(string message)
        {
            string path = Path.Combine(
                KitchenConfig.Get().BasePath,
                "Kitchenbuilder", "Output", "LineShapeSelectorTwoWalls.txt"
            );
            File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
    }
}
