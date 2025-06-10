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

        public static (
            Dictionary<int, (string appliance, double start, double end)> layout,
            Dictionary<int, (double start, double end)> suggestedBases,
            Dictionary<int, string> suggestedDescriptions)
            Evaluate(
            Kitchen kitchen,
            Dictionary<int, List<(double start, double end)>> emptySpaces)
        {
            var layout = new Dictionary<int, (string appliance, double start, double end)>();
            var suggestedBases = new Dictionary<int, (double start, double end)>();
            var suggestedDescriptions = new Dictionary<int, string>();

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
                    layout.Add(0, ("Sink", pos, pos + sinkWidth));
                    pos += sinkWidth + workspace;
                    layout.Add(1, ("Cooktop", pos, pos + cooktopWidth));
                    pos += cooktopWidth + workspace;
                    layout.Add(2, ("Fridge", pos, pos + fridgeWidth));

                    suggestedBases.Clear();
                    suggestedBases[1] = (seg.start, seg.end);

                    suggestedDescriptions[1] = $"Wall 1: {seg.start}-{seg.end} cm (evaluating fridge placement: right)";

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

                    if (seg0Length >= fridgeWidth + cooktopWidth + workspace)
                    {
                        layout.Add(10, ("Fridge (L-shape)", fridgePos, fridgePos + fridgeWidth));
                        double p0 = fridgePos + fridgeWidth + workspace;
                        layout.Add(11, ("Cooktop (L-shape)", p0, p0 + cooktopWidth));
                        layout.Add(12, ("Sink (L-shape - Exposed Base)", 0, sinkWidth));

                        suggestedBases.Clear();
                        suggestedBases[1] = (seg0.start, seg0.end);
                        suggestedBases[lShapeCornerPosition] = (0, kitchen.Floor.Length);

                        suggestedDescriptions[2] = $"Wall 1: {seg0.start}-{seg0.end} cm, corner {lShapeCornerPosition} (evaluating fridge placement: right)\n" +
                                                   $"Wall {lShapeCornerPosition}: 0-{kitchen.Floor.Length} cm";

                        lShapeSuggested = true;
                        break;
                    }
                    if (seg0Length >= fridgeWidth + sinkWidth + workspace)
                    {
                        layout.Add(10, ("Fridge (L-shape)", fridgePos, fridgePos + fridgeWidth));
                        double p0 = fridgePos + fridgeWidth + workspace;
                        layout.Add(11, ("Sink (L-shape)", p0, p0 + sinkWidth));
                        layout.Add(12, ("Cooktop (L-shape - Exposed Base)", 0, cooktopWidth));

                        suggestedBases.Clear();
                        suggestedBases[1] = (seg0.start, seg0.end);
                        suggestedBases[lShapeCornerPosition] = (0, kitchen.Floor.Length);

                        suggestedDescriptions[2] = $"Wall 1: {seg0.start}-{seg0.end} cm, corner {lShapeCornerPosition} (evaluating fridge placement: right)\n" +
                                                   $"Wall {lShapeCornerPosition}: 0-{kitchen.Floor.Length} cm";

                        lShapeSuggested = true;
                        break;
                    }
                }
            }

            WriteResultToFile(layout, straightLineSuggested, lShapeSuggested, lShapeCornerPosition, suggestedBases, suggestedDescriptions, kitchen);

            ImplementOneWallInSld.CopyAndOpenFiles(suggestedDescriptions);


            return (layout, suggestedBases, suggestedDescriptions);
        }

        private static double CalculateFridgePositionConsideringWindow(Kitchen kitchen, double segmentStart, double segmentEnd, double fridgeWidth)
        {
            if (kitchen.Walls[0].HasWindows && kitchen.Walls[0].Windows?.Any() == true)
            {
                var windows = kitchen.Walls[0].Windows
                    .Where(w => w.DistanceY > 90)
                    .OrderBy(w => w.DistanceX)
                    .ToList();

                foreach (var window in windows)
                {
                    double availableRight = segmentEnd - window.DistanceX - window.Width;
                    double availableLeft = window.DistanceX;

                    if (availableRight >= fridgeWidth)
                        return window.DistanceX + window.Width + 5;
                    if (availableLeft >= fridgeWidth)
                        return segmentStart + 5;
                }
            }
            return segmentStart;
        }

        private static int DecideCornerPosition(Kitchen kitchen, double segmentStart, double segmentEnd)
        {
            if (kitchen.Walls[0].HasWindows && kitchen.Walls[0].Windows?.Any() == true)
            {
                var windows = kitchen.Walls[0].Windows
                    .Where(w => w.DistanceY > 90)
                    .OrderBy(w => w.DistanceX)
                    .ToList();

                if (windows.Count > 0)
                {
                    var firstWindow = windows.First();
                    var lastWindow = windows.Last();

                    double availableLeft = firstWindow.DistanceX;
                    double availableRight = segmentEnd - lastWindow.DistanceX - lastWindow.Width;

                    if (availableRight > availableLeft)
                        return 2;
                    else if (availableLeft > availableRight)
                        return 4;
                }
            }
            return 2;
        }

        private static void WriteResultToFile(
            Dictionary<int, (string appliance, double start, double end)> layout,
            bool straightLineSuggested,
            bool lShapeSuggested,
            int lShapeCornerPosition,
            Dictionary<int, (double start, double end)> suggestedBases,
            Dictionary<int, string> suggestedDescriptions,
            Kitchen kitchen)
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
                    foreach (var kvp in layout.Where(x => x.Key >= 0 && x.Key <= 2).OrderBy(x => x.Key))
                    {
                        var (appliance, start, end) = kvp.Value;
                        writer.WriteLine($"  • {appliance.PadRight(25)} Start: {start,6:0.##} cm   End: {end,6:0.##} cm");
                    }
                    writer.WriteLine();
                }

                if (lShapeSuggested)
                {
                    writer.WriteLine("▶️ L-shape kitchen layout:");
                    writer.WriteLine($"  • Suggested corner position: {lShapeCornerPosition}");
                    foreach (var kvp in layout.Where(x => x.Key >= 10).OrderBy(x => x.Key))
                    {
                        var (appliance, start, end) = kvp.Value;
                        writer.WriteLine($"  • {appliance.PadRight(25)} Start: {start,6:0.##} cm   End: {end,6:0.##} cm");
                    }
                    writer.WriteLine();
                }

                writer.WriteLine("📝 Suggested Layout Descriptions:");
                foreach (var kvp in suggestedDescriptions.OrderBy(x => x.Key))
                {
                    writer.WriteLine($"  Option {kvp.Key}: {kvp.Value}");
                }

                writer.WriteLine();
                writer.WriteLine("Suggested Bases for SolidWorks:");
                foreach (var kvp in suggestedBases.OrderBy(x => x.Key))
                {
                    writer.WriteLine($"  • Wall {kvp.Key}: Start: {kvp.Value.start} cm   End: {kvp.Value.end} cm");
                }

                if (!straightLineSuggested && !lShapeSuggested)
                {
                    writer.WriteLine("⚠️ No valid layout could be suggested. Please consider a custom design.");
                }
            }
        }
    }
}
