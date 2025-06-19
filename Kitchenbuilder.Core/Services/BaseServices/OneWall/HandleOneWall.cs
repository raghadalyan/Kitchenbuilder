using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kitchenbuilder.Core
{
    public static class HandleOneWall
    {


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
            int lShapeCornerPosition = 2;

            string jsonFolder = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\";
            Directory.CreateDirectory(jsonFolder);

            foreach (var seg in wall0Segments)
            {
                double segLength = seg.end - seg.start;
                if (segLength >= requiredWidthStraight)
                {
                    var (pos, side) = CalculateFridgePositionConsideringWindow(kitchen, seg.start, seg.end, fridgeWidth);
                    layout.Add(0, ("Sink", pos, pos + sinkWidth));
                    pos += sinkWidth + workspace;
                    layout.Add(1, ("Cooktop", pos, pos + cooktopWidth));
                    pos += cooktopWidth + workspace;
                    layout.Add(2, ("Fridge", pos, pos + fridgeWidth));

                    suggestedBases.Clear();
                    suggestedBases[1] = (seg.start, seg.end);

                    // Write JSON for Line Shape
                    var lineShapeJson = new
                    {
                        Title = "Line Shape",
                        Wall1 = 1,
                        SpacesWall1 = new[] { new { Start = seg.start, End = seg.end } },
                        FridgeWall = 1,
                        Fridge = new { Start = pos, End = pos + fridgeWidth },
                        Corner = false,
                        Exposed = false
                    };
                    File.WriteAllText(Path.Combine(jsonFolder, "Option1.json"),
                        System.Text.Json.JsonSerializer.Serialize(lineShapeJson, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

                    straightLineSuggested = true;
                    break;
                }
            }

            foreach (var seg0 in wall0Segments)
            {
                double seg0Length = seg0.end - seg0.start;
                if (seg0Length >= minRequiredWall0Length && kitchen.Floor.Length >= exposedBaseMinLength)
                {
                    var (fridgePos, side) = CalculateFridgePositionConsideringWindow(kitchen, seg0.start, seg0.end, fridgeWidth);
                    lShapeCornerPosition = DecideCornerPosition(kitchen, seg0.start, seg0.end);

                    if (seg0Length >= fridgeWidth + cooktopWidth + workspace)
                    {
                        layout.Add(10, ("Fridge (L-shape)", fridgePos, fridgePos + fridgeWidth));
                        double p0 = fridgePos + fridgeWidth + workspace;
                        layout.Add(11, ("Cooktop (L-shape)", p0, p0 + cooktopWidth));
                        layout.Add(12, ("Sink (L-shape - Exposed Base)", 0, sinkWidth));

                        suggestedBases.Clear();
                        suggestedBases[1] = (seg0.start, seg0.end);
                        double cornerBaseLength = kitchen.Floor.Length < 180 && kitchen.Floor.Length > 150 ? kitchen.Floor.Length : 180;
                        suggestedBases[lShapeCornerPosition] = (0, cornerBaseLength);

                        // Write JSON for LShape
                        var lShapeJson = new
                        {
                            Title = "LShape",
                            Wall1 = 1,
                            SpacesWall1 = new[] { new { Start = seg0.start, End = seg0.end } },
                            FridgeWall = 1,
                            Fridge = new { Start = fridgePos, End = fridgePos + fridgeWidth },
                            Corner = true,
                            Exposed = true,
                            NumOfExposedWall = lShapeCornerPosition,
                            ExposedWallSpace = new { Start = 0, End = cornerBaseLength }
                        };
                        File.WriteAllText(Path.Combine(jsonFolder, "Option2.json"),
                            System.Text.Json.JsonSerializer.Serialize(lShapeJson, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

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
                        double cornerBaseLength = kitchen.Floor.Length < 180 && kitchen.Floor.Length > 150 ? kitchen.Floor.Length : 180;
                        suggestedBases[lShapeCornerPosition] = (0, cornerBaseLength);

                        var lShapeJson = new
                        {
                            Title = "LShape",
                            Wall1 = 1,
                            SpacesWall1 = new[] { new { Start = seg0.start, End = seg0.end } },
                            FridgeWall = 1,
                            Fridge = new { Start = fridgePos, End = fridgePos + fridgeWidth },
                            Corner = true,
                            Exposed = true,
                            NumOfExposedWall = lShapeCornerPosition,
                            ExposedWallSpace = new { Start = 0, End = cornerBaseLength }
                        };
                        File.WriteAllText(Path.Combine(jsonFolder, "Option2.json"),
                            System.Text.Json.JsonSerializer.Serialize(lShapeJson, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

                        lShapeSuggested = true;
                        break;
                    }
                }
            }

            return (layout, suggestedBases, suggestedDescriptions);
        }


        private static (double fridgeX, string side) CalculateFridgePositionConsideringWindow(Kitchen kitchen, double segmentStart, double segmentEnd, double fridgeWidth)
        {
            if (kitchen.Walls[0].HasWindows && kitchen.Walls[0].Windows?.Any() == true)
            {
                var windows = kitchen.Walls[0].Windows
                    .Where(w => w.DistanceY > 90)
                    .OrderBy(w => w.DistanceX)
                    .ToList();

                if (windows.Count > 0)
                {
                    double firstWinX = windows.First().DistanceX;
                    double lastWinX = windows.Last().DistanceX + windows.Last().Width;
                    double rightClear = segmentEnd - lastWinX;

                    if (firstWinX > rightClear)
                    {
                        // Place fridge on the left
                        return (segmentStart + 5, "left");
                    }
                    else
                    {
                        // Place fridge on the right
                        return (segmentEnd - fridgeWidth - 5, "right");
                    }
                }
            }

            // No valid window → default to left
            return (segmentStart + 5, "left");
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

                    double middle = (segmentStart + segmentEnd) / 2;

                    // אם החלון ממוקם בצד שמאל -> wall 4, אחרת wall 2
                    if (lastWindow.DistanceX + lastWindow.Width / 2 < middle)
                        return 4;
                    else
                        return 2;
                }
            }
            return 2;
        }



    }
}
