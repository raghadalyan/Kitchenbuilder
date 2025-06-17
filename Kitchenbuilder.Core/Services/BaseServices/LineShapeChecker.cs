using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
/*
 * LineShapeChecker.cs
 * --------------------
 * This file handles the detailed evaluation logic for placing a straight-line kitchen layout
 * (fridge + sink + cooktop) on a specific wall. It checks for available space and window
 * interference and attempts to place the fridge in different valid positions.
 *
 * If a valid layout is found, it saves the result (including main and secondary empty spaces)
 * to the given JSON path.
 */

namespace Kitchenbuilder.Core
{
    public static class LineShapeChecker
    {
        const double sinkWidth = 60;
        const double cooktopWidth = 60;
        const double fridgeWidth = 85;
        const double workspace = 60;

        public static bool EvaluateWall(Kitchen kitchen, int wallIndex, List<(double start, double end)> emptySpaces, string outputPath)
        {
            var windows = kitchen.Walls[wallIndex].Windows ?? new List<Window>();

            foreach (var (start, end) in emptySpaces.OrderByDescending(e => e.end - e.start))
            {
                double length = end - start;
                if (length < sinkWidth + workspace + cooktopWidth + workspace + fridgeWidth)
                    continue;

                // Try placing fridge on the left
                if (!HasWindowInRange(windows, start, start + fridgeWidth))
                {
                    SaveResult(outputPath, "Line Shape", wallIndex, start, end, $"Fridge Left ({start}-{start + fridgeWidth})", emptySpaces);
                    return true;
                }

                // Try placing fridge on the right
                if (!HasWindowInRange(windows, end - fridgeWidth, end))
                {
                    SaveResult(outputPath, "Line Shape", wallIndex, start, end, $"Fridge Right ({end - fridgeWidth}-{end})", emptySpaces);
                    return true;
                }

                // Try placing fridge in a separate space on the same wall
                var other = emptySpaces.FirstOrDefault(s => s != (start, end) && s.end - s.start >= fridgeWidth);
                if (other != default && !HasWindowInRange(windows, other.start, other.start + fridgeWidth))
                {
                    SaveResult(outputPath, "Line Shape", wallIndex, start, end, $"Fridge Other ({other.start}-{other.start + fridgeWidth})", emptySpaces);
                    return true;
                }
            }

            return false;
        }

        private static bool HasWindowInRange(List<Window> windows, double from, double to)
        {
            return windows.Any(w =>
                (w.DistanceX >= from && w.DistanceX <= to) ||
                (w.DistanceX + w.Width >= from && w.DistanceX + w.Width <= to));
        }

        private static void SaveResult(string path, string title, int wall, double start, double end, string placement, List<(double start, double end)> allSpaces)
        {
            var mainSpace = new { Start = start, End = end };
            var secondarySpaces = allSpaces
                .Where(s => !(s.start == start && s.end == end) && (s.end - s.start >= 60))
                .Select(s => new { Start = s.start, End = s.end })
                .ToList();

            var option = new
            {
                Title = title,
                Wall = wall + 1,
                MainEmptySpace = mainSpace,
                SecondaryEmptySpaces = secondarySpaces,
                FridgePlacement = placement
            };

            string json = JsonSerializer.Serialize(option, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
