using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kitchenbuilder.Core
{
    public static class HandleOneWall
    {
        private const string OutputFolder = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output";
        private const string OutputFile = "Evaluate Base.txt";

        public static Dictionary<int, (string appliance, double start, double end)> Evaluate(
            Kitchen kitchen,
            Dictionary<int, List<(double start, double end)>> emptySpaces)
        {
            var result = new Dictionary<int, (string appliance, double start, double end)>();
            const double sinkWidth = 60;
            const double cooktopWidth = 60;
            const double fridgeWidth = 85;
            const double workspace = 60;
            double requiredWidthStraight = sinkWidth + workspace + cooktopWidth + workspace + fridgeWidth; // 325 cm
            const double minRequiredWall0Length = 265;
            const double exposedBaseMinLength = 150;

            var wall0Segments = emptySpaces.ContainsKey(0)
                ? emptySpaces[0]
                : new List<(double start, double end)> { (0, kitchen.Walls[0].Width) };

            bool straightLineSuggested = false;
            bool lShapeSuggested = false;
            int lShapeCornerPosition = 2; // Default

            // 1️⃣ Straight-line layout
            foreach (var seg in wall0Segments)
            {
                double segLength = seg.end - seg.start;
                if (segLength >= requiredWidthStraight)
                {
                    double pos = CalculateFridgePositionConsideringWindow(kitchen, seg.start, seg.end, fridgeWidth);
                    result.Add(0, ("Sink", pos, pos + sinkWidth));
                    pos += sinkWidth + workspace;
                    result.Add(1, ("Cooktop", pos, pos + cooktopWidth));
                    pos += cooktopWidth + workspace;
                    result.Add(2, ("Fridge", pos, pos + fridgeWidth));
                    straightLineSuggested = true;
                    break;
                }
            }

            // 2️⃣ L-shape layout
            foreach (var seg0 in wall0Segments)
            {
                double seg0Length = seg0.end - seg0.start;
                if (seg0Length >= minRequiredWall0Length && kitchen.Floor.Length >= exposedBaseMinLength)
                {
                    double fridgePos = CalculateFridgePositionConsideringWindow(kitchen, seg0.start, seg0.end, fridgeWidth);
                    lShapeCornerPosition = DecideCornerPosition(kitchen, seg0.start, seg0.end);

                    // Option 1: Fridge + Cooktop on wall 0, Sink on exposed base
                    if (seg0Length >= fridgeWidth + cooktopWidth + workspace)
                    {
                        result.Add(10, ("Fridge (L-shape)", fridgePos, fridgePos + fridgeWidth));
                        double p0 = fridgePos + fridgeWidth + workspace;
                        result.Add(11, ("Cooktop (L-shape)", p0, p0 + cooktopWidth));
                        result.Add(12, ("Sink (L-shape - Exposed Base)", 0, sinkWidth));
                        lShapeSuggested = true;
                        break;
                    }
                    // Option 2: Fridge + Sink on wall 0, Cooktop on exposed base
                    if (seg0Length >= fridgeWidth + sinkWidth + workspace)
                    {
                        result.Add(10, ("Fridge (L-shape)", fridgePos, fridgePos + fridgeWidth));
                        double p0 = fridgePos + fridgeWidth + workspace;
                        result.Add(11, ("Sink (L-shape)", p0, p0 + sinkWidth));
                        result.Add(12, ("Cooktop (L-shape - Exposed Base)", 0, cooktopWidth));
                        lShapeSuggested = true;
                        break;
                    }
                }
            }

            // 3️⃣ Write results to file
            WriteResultToFile(result, straightLineSuggested, lShapeSuggested, lShapeCornerPosition);

            return result;
        }

        private static double CalculateFridgePositionConsideringWindow(Kitchen kitchen, double segmentStart, double segmentEnd, double fridgeWidth)
        {
            if (kitchen.Walls[0].HasWindows && kitchen.Walls[0].Windows != null)
            {
                foreach (var window in kitchen.Walls[0].Windows)
                {
                    if (window.DistanceY > 90)
                    {
                        double availableRight = segmentEnd - window.DistanceX - window.Width;
                        double availableLeft = window.DistanceX;

                        if (availableRight >= fridgeWidth)
                            return window.DistanceX + window.Width + 5; // 5 cm buffer from window

                        if (availableLeft >= fridgeWidth)
                            return segmentStart + 5; // 5 cm buffer from start
                    }
                }
            }
            return segmentStart;
        }

        private static int DecideCornerPosition(Kitchen kitchen, double segmentStart, double segmentEnd)
        {
            if (kitchen.Walls[0].HasWindows && kitchen.Walls[0].Windows != null)
            {
                var windows = kitchen.Walls[0].Windows
                    .Where(w => w.DistanceY > 90)
                    .OrderBy(w => w.DistanceX)
                    .ToList();

                if (windows.Any())
                {
                    var firstWindow = windows.First();
                    var lastWindow = windows.Last();

                    double availableLeft = firstWindow.DistanceX;
                    double availableRight = segmentEnd - lastWindow.DistanceX - lastWindow.Width;

                    if (availableRight > availableLeft)
                        return 2; // Right corner
                    else if (availableLeft > availableRight)
                        return 4; // Left corner
                    else
                        return 2; // Default to right if equal
                }
            }
            return 2; // Default corner
        }

        private static void WriteResultToFile(
            Dictionary<int, (string appliance, double start, double end)> layout,
            bool straightLineSuggested,
            bool lShapeSuggested,
            int lShapeCornerPosition)
        {
            Directory.CreateDirectory(OutputFolder);
            string fullPath = Path.Combine(OutputFolder, OutputFile);

            using (var writer = new StreamWriter(fullPath, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("=== Kitchen Base Evaluation ===");
                writer.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
                writer.WriteLine();

                if (straightLineSuggested)
                {
                    writer.WriteLine("▶️ Straight-line kitchen layout:");
                    var straightLine = layout
                        .Where(kvp => kvp.Key >= 0 && kvp.Key <= 2)
                        .OrderBy(kvp => kvp.Key);

                    foreach (var kvp in straightLine)
                    {
                        var (appliance, start, end) = kvp.Value;
                        writer.WriteLine($"  • {appliance.PadRight(20)} Start: {start,6:0.##} cm   End: {end,6:0.##} cm");
                    }
                    writer.WriteLine();
                }

                if (lShapeSuggested)
                {
                    writer.WriteLine("▶️ L-shape kitchen layout:");
                    writer.WriteLine($"  • Suggested corner position: {lShapeCornerPosition}");
                    var lShape = layout
                        .Where(kvp => kvp.Key >= 10)
                        .OrderBy(kvp => kvp.Key);

                    foreach (var kvp in lShape)
                    {
                        var (appliance, start, end) = kvp.Value;
                        writer.WriteLine($"  • {appliance.PadRight(20)} Start: {start,6:0.##} cm   End: {end,6:0.##} cm");
                    }
                    writer.WriteLine();
                }

                if (!straightLineSuggested && !lShapeSuggested)
                {
                    writer.WriteLine("⚠️ No valid layout could be suggested. Please consider a custom design.");
                }
            }

            Console.WriteLine($"Output written to: {fullPath}");
        }
    }
}
