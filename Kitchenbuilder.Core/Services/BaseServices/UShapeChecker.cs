using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Kitchenbuilder.Core
{
    public static class UShapeChecker
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Ushapechecker.txt";

        private static void Log(string message)
        {
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
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
                return HandleExposedBaseWithCorner(
                    kitchen,
                    wall1Index,
                    wall2Index,
                    spacesWall1,
                    spacesWall2,
                    fridgeWall,
                    fridgeStart,
                    fridgeEnd,
                    outputPath,
                    exposedWallLength,
                    exposedWallNumber);
            }

            Log("❌ Unsupported case (not exposed or not corner)");
            return false;
        }

        private static bool HandleExposedBaseWithCorner(
            Kitchen kitchen,
            int wall1Index,
            int wall2Index,
            List<(double start, double end)> spacesWall1,
            List<(double start, double end)> spacesWall2,
            int fridgeWall,
            double fridgeStart,
            double fridgeEnd,
            string outputPath,
            double exposedWallLength,
            int exposedWallNumber)
        {
            var floorWidth = kitchen.Floor.Width;
            var floorLength = kitchen.Floor.Length;

            var windowsW1 = kitchen.Walls[wall1Index].Windows ?? new List<Window>();
            var windowsW2 = kitchen.Walls[wall2Index].Windows ?? new List<Window>();
            var allWindows = fridgeWall == wall1Index ? windowsW1 : windowsW2;

            if (HasWindow(fridgeStart, fridgeEnd, allWindows))
            {
                Log("❌ Fridge placement overlaps with a window");
                return false;
            }

            // === Case: Exposed Wall 3 ===
            if (exposedWallNumber == 3 && floorWidth > 150 && floorLength >= 240)
            {
                (double start, double end) exposedWallSpace = floorWidth <= 180 ? (0, floorWidth) : (0, 180);

                var lastSpace = spacesWall1[^1];
                if (Math.Abs(fridgeStart - lastSpace.start) < 1)
                {
                    double gapAfterFridge = lastSpace.end - fridgeEnd;
                    if (gapAfterFridge >= 60)
                    {
                        Log("✅ Wall 3: Fridge is in last space of Wall 1 and has enough space after (≥ 60 cm)");
                        return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2,
                            fridgeWall, fridgeStart, fridgeEnd, true, true, exposedWallNumber, exposedWallSpace);
                    }
                    else
                    {
                        Log($"❌ Wall 3: Not enough space after fridge: {gapAfterFridge} < 60 cm");
                        return false;
                    }
                }

                for (int i = 0; i < spacesWall1.Count - 1; i++)
                {
                    var space = spacesWall1[i];
                    if (Math.Abs(fridgeStart - space.start) < 1)
                    {
                        double length = space.end - space.start;
                        if (length >= 85)
                        {
                            Log($"✅ Wall 3: Fridge is in previous space {i} of Wall 1 with length {length} ≥ 85");
                            return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2,
                                fridgeWall, fridgeStart, fridgeEnd, true, true, exposedWallNumber, exposedWallSpace);
                        }
                        else
                        {
                            Log($"❌ Wall 3: Previous space {i} too small: {length} < 85");
                            return false;
                        }
                    }
                }

                Log("❌ Wall 3: Fridge not placed in last or valid previous space of Wall 1");
                return false;
            }

            // === Case: Exposed Wall 4 ===
            if (exposedWallNumber == 4 && floorLength > 150 && floorWidth >= 240)
            {
                (double start, double end) exposedWallSpace = floorLength <= 180 ? (0, floorLength) : (floorLength - 180, floorLength);

                var firstSpace = spacesWall2[0];
                if (Math.Abs(fridgeEnd - firstSpace.end) < 1)
                {
                    double gapBeforeFridge = fridgeStart - firstSpace.start;
                    if (gapBeforeFridge >= 60)
                    {
                        Log("✅ Wall 4: Fridge is in first space of Wall 2 and has enough space before (≥ 60 cm)");
                        return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2,
                            fridgeWall, fridgeStart, fridgeEnd, true, true, exposedWallNumber, exposedWallSpace);
                    }
                    else
                    {
                        Log($"❌ Wall 4: Not enough space before fridge: {gapBeforeFridge} < 60 cm");
                        return false;
                    }
                }

                for (int i = 1; i < spacesWall2.Count; i++)
                {
                    var space = spacesWall2[i];
                    if (Math.Abs(fridgeEnd - space.end) < 1)
                    {
                        double length = space.end - space.start;
                        if (length >= 85)
                        {
                            Log($"✅ Wall 4: Fridge is in next space {i} of Wall 2 with length {length} ≥ 85");
                            return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2,
                                fridgeWall, fridgeStart, fridgeEnd, true, true, exposedWallNumber, exposedWallSpace);
                        }
                        else
                        {
                            Log($"❌ Wall 4: Next space {i} too small: {length} < 85");
                            return false;
                        }
                    }
                }

                Log("❌ Wall 4: Fridge not placed in first or valid next space of Wall 2");
                return false;
            }

            Log("❌ Invalid exposure conditions for Wall 3 or 4");
            return false;
        }



        private static bool HasWindow(double from, double to, List<Window> windows)
        {
            return windows.Any(w =>
                Math.Max(from, w.DistanceX) < Math.Min(to, w.DistanceX + w.Width));
        }

        private static bool WriteSuccess(
            string outputPath,
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
                Wall1 = wall1Index+1,
                Wall2 = wall2Index + 1,
                SpacesWall1 = spacesWall1.Select(s => new { Start = s.start, End = s.end }),
                SpacesWall2 = spacesWall2.Select(s => new { Start = s.start, End = s.end }),
                FridgeWall = fridgeWall + 1,
                Fridge = new { Start = fridgeStart, End = fridgeEnd },
                Corner = corner,
                Exposed = exposed,
                NumOfExposedWall = numOfExposedWall,
                ExposedWallSpace = new { Start = exposedWallSpace.start, End = exposedWallSpace.end }
            };

            string json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outputPath, json);
            Log("📤 Result written to Option3.json");

            return true;
        }
    }
}
