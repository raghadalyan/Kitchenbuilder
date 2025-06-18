using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kitchenbuilder.Core
{
    public static class UShapeChecker
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\UShapeCheckerDebug.txt";

        private static void Log(string message)
        {
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }

        private static bool HasWindow(double start, double end, List<Window> windows)
        {
            return windows.Any(w =>
                Math.Max(start, w.DistanceX) < Math.Min(end, w.DistanceX + w.Width));
        }

        public static bool EvaluateUShape(
            Kitchen kitchen,
            int wall1Index,
            int wall2Index,
            List<(double start, double end)> spacesWall1,
            List<(double start, double end)> spacesWall2,
            double exposedWallLength,
            int exposedWallNumber,
            string outputPath,
            bool exposed,
            bool corner,
            int fridgeWall,
            double fridgeStart,
            double fridgeEnd)
        {
            if (exposed && corner)
            {
                return HandleExposedBaseWithCorner(kitchen, wall1Index, wall2Index,
                    spacesWall1, spacesWall2, exposedWallLength,
                    exposedWallNumber, fridgeWall, fridgeStart, fridgeEnd, outputPath);
            }

            // fallback logic here if needed
            return false;
        }

        private static bool HandleExposedBaseWithCorner(
           Kitchen kitchen,
           int wall1Index,
           int wall2Index,
           List<(double start, double end)> spacesWall1,
           List<(double start, double end)> spacesWall2,
           double exposedWallLength,
           int exposedWallNumber,
           int fridgeWall,
           double fridgeStart,
           double fridgeEnd,
           string outputPath)
        {
            int wallX = Math.Min(wall1Index, wall2Index);
            int wallY = Math.Max(wall1Index, wall2Index);
            var spacesX = wallX == wall1Index ? spacesWall1 : spacesWall2;
            var spacesY = wallY == wall1Index ? spacesWall1 : spacesWall2;
            var wallXData = kitchen.Walls[wallX];
            var wallYData = kitchen.Walls[wallY];
            var windowsX = wallXData.Windows ?? new List<Window>();
            var windowsY = wallYData.Windows ?? new List<Window>();

            double floorWidth = kitchen.Floor.Width;
            double floorLength = kitchen.Floor.Length;

            Log("*************** HandleExposedBaseWithCorner PARAMETERS ***************");
            Log($"wall1Index: {wall1Index}, wall2Index: {wall2Index}");
            Log($"spacesWall1: [{string.Join(", ", spacesWall1.Select(s => $"({s.start}, {s.end})"))}]");
            Log($"spacesWall2: [{string.Join(", ", spacesWall2.Select(s => $"({s.start}, {s.end})"))}]");
            Log($"exposedWallLength: {exposedWallLength}, exposedWallNumber: {exposedWallNumber}");
            Log($"fridgeWall: {fridgeWall}, fridgeStart: {fridgeStart}, fridgeEnd: {fridgeEnd}");
            Log($"floorWidth: {floorWidth}, floorLength: {floorLength}");
            Log("**********************************************************************");

            bool isExposedWall3 = exposedWallNumber == 3 && floorWidth > 150 && floorLength >= 240;
            bool isExposedWall4 = exposedWallNumber == 4 && floorLength > 150 && floorWidth >= 240;

            if (isExposedWall3 || isExposedWall4)
            {
                var relevantSpaces = isExposedWall3 ? spacesX : spacesY;
                var oppositeSpaces = isExposedWall3 ? spacesY : spacesX;
                var relevantWindows = isExposedWall3 ? windowsX : windowsY;

                var last = relevantSpaces[^1];
                double spaceLen = last.end - last.start;

                // Check spacing depending on direction
                double spacingCheck = isExposedWall3
                    ? last.end - fridgeEnd  // AFTER fridge on wallX
                    : fridgeStart - last.start; // BEFORE fridge on wallY

                if (spaceLen >= 145 &&
                    spacingCheck > 60 &&
                    oppositeSpaces.Any(s => (s.end - s.start) >= 240) &&
                    !HasWindow(fridgeStart, fridgeEnd, relevantWindows))
                {
                    Log("✅ Valid U-Shape: Fridge in last space of relevant wall");
                    return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2,
                        fridgeWall, fridgeStart, fridgeEnd, true, true, exposedWallNumber, (0, exposedWallLength));
                }

                if (relevantSpaces.Count >= 2)
                {
                    var beforeLast = relevantSpaces[^2];
                    double len = beforeLast.end - beforeLast.start;

                    if (((len > 175 && oppositeSpaces.Any(s => (s.end - s.start) >= 240)) ||
                         (len > 240 && oppositeSpaces.Any(s => (s.end - s.start) >= 240))) &&
                        !HasWindow(fridgeStart, fridgeEnd, relevantWindows))
                    {
                        Log("✅ Valid U-Shape: Fridge in before-last space of relevant wall");
                        return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2,
                            fridgeWall, fridgeStart, fridgeEnd, true, true, exposedWallNumber, (0, exposedWallLength));
                    }
                }
            }

            Log("❌ No valid U-Shape corner placement found.");
            return false;
        }


        private static bool WriteSuccess(string outputPath,
            int wall1Index,
            int wall2Index,
            List<(double start, double end)> spacesWall1,
            List<(double start, double end)> spacesWall2,
            int fridgeWall,
            double fridgeStart,
            double fridgeEnd,
            bool exposed,
            bool corner,
            int numOfExposedWall,
            (double start, double end) exposedWallSpace)
        {
            var result = new
            {
                Title = "UShape",
                Wall1 = wall1Index + 1,
                Wall2 = wall2Index + 1,
                SpacesWall1 = spacesWall1.Select(s => new { Start = s.start, End = s.end }).ToList(),
                SpacesWall2 = spacesWall2.Select(s => new { Start = s.start, End = s.end }).ToList(),
                FridgeWall = fridgeWall,
                Fridge = new { Start = fridgeStart, End = fridgeEnd },
                Corner = corner,
                Exposed = exposed,
                NumOfExposedWall = numOfExposedWall,
                ExposedWallSpace = new { Start = exposedWallSpace.start, End = exposedWallSpace.end }
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(outputPath, JsonSerializer.Serialize(result, options));
            return true;
        }
    }
}
