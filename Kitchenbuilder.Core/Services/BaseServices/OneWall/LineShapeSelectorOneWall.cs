using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kitchenbuilder.Core
{
    public static class LineShapeSelectorOneWall
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\LineShapeSelectorOneWall.txt";

        private static void LogDebug(string message)
        {
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }

        public static bool TryFindWallForLineShape(
            Kitchen kitchen,
            Dictionary<int, List<(double start, double end)>> simpleEmptySpaces)
        {
            const double fridgeWidth = 85;
            int wallIndex = 0;
            double floorLength = kitchen.Floor.Length;
            string outputPath = @"C:\\Users\\chouse\\Downloads\\Kitchenbuilder\\Kitchenbuilder\\JSON\\Option1.json";

            if (!simpleEmptySpaces.ContainsKey(wallIndex))
            {
                LogDebug("❌ Wall 0 does not contain any empty space.");
                return false;
            }

            var wallSpaces = simpleEmptySpaces[wallIndex]
                .OrderByDescending(seg => seg.end - seg.start)
                .ToList();

            foreach (var (start, end) in wallSpaces)
            {
                double segLength = end - start;
                if (segLength < fridgeWidth)
                    continue;

                // 1. Try fridge at the start of this segment
                double fridgeStart = start;
                double fridgeEnd = start + fridgeWidth;

                bool success = LineShapeChecker.EvaluateWall(
                    kitchen, wallIndex, wallSpaces, floorLength,
                    outputPath, exposed: false, corner: false,
                    fridgeWall: 1, fridgeStart: fridgeStart, fridgeEnd: fridgeEnd);

                if (success)
                {
                    LogDebug($"✅ Fridge at start of segment {start}-{end}");
                    return true;
                }

                // 2. Try fridge at the end of this segment
                fridgeStart = end - fridgeWidth;
                fridgeEnd = end;

                success = LineShapeChecker.EvaluateWall(
                    kitchen, wallIndex, wallSpaces, floorLength,
                    outputPath, exposed: false, corner: false,
                    fridgeWall: 1, fridgeStart: fridgeStart, fridgeEnd: fridgeEnd);

                if (success)
                {
                    LogDebug($"✅ Fridge at end of segment {start}-{end}");
                    return true;
                }
            }

            LogDebug("❌ No suitable fridge placement found after checking all segments.");
            return false;
        }

    }
}