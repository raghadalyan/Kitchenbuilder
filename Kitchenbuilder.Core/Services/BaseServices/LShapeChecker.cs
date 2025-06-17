using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Kitchenbuilder.Core
{
    public static class LShapeChecker
    {
        private static readonly string DebugLogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\HandleTwoWalls.txt";

        private static void Log(string message)
        {
            File.AppendAllText(DebugLogPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }

        public static bool EvaluateLShape(
            Kitchen kitchen,
            int wall1Index,
            int wall2Index,
            List<(double start, double end)> spacesWall1,
            List<(double start, double end)> spacesWall2,
            double floorLength,
            int exposedWallIndex,
            string outputPath,
            bool exposed,
            bool corner,
            int fridgeWall, 
            double fridgeStart,
            double fridgeEnd)
        {
            bool HasWindow(double from, double to, List<Window> windows)
            {
                return windows.Any(w => Math.Max(from, w.DistanceX) < Math.Min(to, w.DistanceX + w.Width));
            }

            if (!exposed)
            {
                if (corner)
                {
                    return HandleCornerPlacement(
                        kitchen, wall1Index, wall2Index,
                        spacesWall1, spacesWall2,
                        fridgeWall, fridgeStart, fridgeEnd,
                        outputPath, HasWindow);
                }
                else
                {
                    return HandleNonCornerPlacement(
                        kitchen, wall1Index, wall2Index,
                        spacesWall1, spacesWall2,
                        fridgeWall, fridgeStart, fridgeEnd,
                        outputPath, HasWindow);
                }
            }


            Log("❌ No valid L-Shape configuration found.");
            return false;
        }



        /// <summary>
        /// Handles validation of non-corner L-shape placement:
        /// 
        /// ✅ Steps:
        /// 1. Rejects placement if fridge area overlaps a window.
        /// 2. Identifies the exact space where the fridge is placed.
        /// 3. Rejects if that space is smaller than 85 cm.
        /// 4. If space is 85–205 cm:
        ///    - Accepts only if:
        ///        a. Another space (≥120cm)(120=60+60workspace) exists in the same wall, AND
        ///        b. A space (≥120cm) (120=60+60workspace)exists in the other wall
        ///    - OR accepts if:
        ///        c. A space (≥240cm)(60+60+120workspace) exists in the other wall.
        /// 5. If space is ≥205 (85 fridge+120(60+60workspace)) cm:
        ///    - Accepts if the other wall has a space ≥120 cm.
        /// 
        /// Returns true and writes result if valid; otherwise returns false.
        /// </summary>

        private static bool HandleNonCornerPlacement(
    Kitchen kitchen,
    int wall1Index,
    int wall2Index,
    List<(double start, double end)> spacesWall1,
    List<(double start, double end)> spacesWall2,
    int fridgeWall,
    double fridgeStart,
    double fridgeEnd,
    string outputPath,
    Func<double, double, List<Window>, bool> HasWindow)
        {
            var wall1 = kitchen.Walls[wall1Index];
            var wall2 = kitchen.Walls[wall2Index];
            var windows1 = wall1.Windows ?? new List<Window>();
            var windows2 = wall2.Windows ?? new List<Window>();

            var (spacesCurrentWall, spacesOtherWall, windowsCurrent, windowsOther) =
                fridgeWall == wall1Index + 1
                    ? (spacesWall1, spacesWall2, windows1, windows2)
                    : (spacesWall2, spacesWall1, windows2, windows1);

            // ❌ Block if window overlaps
            if (HasWindow(fridgeStart, fridgeEnd, windowsCurrent))
            {
                Log("🚫 Fridge area overlaps a window — aborting placement.");
                return false;
            }

            // 🧱 Identify the empty space that contains the fridge
            var matchingSpace = spacesCurrentWall.FirstOrDefault(s =>
                fridgeStart >= s.start && fridgeEnd <= s.end);

            double currentLength = matchingSpace.end - matchingSpace.start;
            if (currentLength < 85)
            {
                Log($"🚫 Fridge space too small: {currentLength}cm");
                return false;
            }

            Log($"📏 Current fridge space: {currentLength}cm");

            // Logic split by size
            if (currentLength >= 85 && currentLength < 205)
            {
                bool hasAnother120InSameWall = spacesCurrentWall.Any(s => s != matchingSpace && (s.end - s.start) >= 120);
                bool has120InOtherWall = spacesOtherWall.Any(s => (s.end - s.start) >= 120);

                if (hasAnother120InSameWall && has120InOtherWall)
                {
                    Log("✅ Valid: small fridge space with two medium zones (120+) on both walls.");
                    return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2, fridgeWall, fridgeStart, fridgeEnd, false, false);
                }

                if (spacesOtherWall.Any(s => (s.end - s.start) >= 240))
                {
                    Log("✅ Valid: small fridge space + one large (240+) on other wall.");
                    return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2, fridgeWall, fridgeStart, fridgeEnd, false, false);
                }

                return false;
            }

            if (currentLength >= 205)
            {
                if (spacesOtherWall.Any(s => (s.end - s.start) >= 120))
                {
                    Log("✅ Valid: long fridge space (205+) and large space (120+) on other wall.");
                    return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2, fridgeWall, fridgeStart, fridgeEnd, false, false);
                }

                return false;
            }

            return false;
        }

        private static bool HandleCornerPlacement(
    Kitchen kitchen,
    int wall1Index,
    int wall2Index,
    List<(double start, double end)> spacesWall1,
    List<(double start, double end)> spacesWall2,
    int fridgeWall,
    double fridgeStart,
    double fridgeEnd,
    string outputPath,
    Func<double, double, List<Window>, bool> HasWindow)
        {
            int wallX = Math.Min(wall1Index, wall2Index);
            int wallY = Math.Max(wall1Index, wall2Index);
            var spacesX = wallX == wall1Index ? spacesWall1 : spacesWall2;
            var spacesY = wallY == wall1Index ? spacesWall1 : spacesWall2;
            var wallXData = kitchen.Walls[wallX];
            var wallYData = kitchen.Walls[wallY];
            var windowsX = wallXData.Windows ?? new List<Window>();
            var windowsY = wallYData.Windows ?? new List<Window>();

            // Recalculate corner
            var lastX = spacesX[^1];
            var firstY = spacesY[0];

            bool actualCorner = lastX.end == wallXData.Width && firstY.start == 0;
            Log($"🧩 Checking corner condition between wall {wallX + 1} and wall {wallY + 1}: {(actualCorner ? "TRUE" : "FALSE")}");
            if (!actualCorner) return false;

            // Fridge in wallX (smaller index wall)
            if (fridgeWall == wallX + 1)
            {
                // Fridge in the last space of wallX
                // Fridge in the last space of wallX
                if (Math.Abs(fridgeStart - lastX.start) < 1)
                {
                    double spaceLen = lastX.end - lastX.start;

                    if (spaceLen < 145)
                    {
                        Log($"⛔ Skipping: fridge is in last space of wall {wallX + 1}, but spaceLen={spaceLen} < 145");
                        return false;
                    }

                    double afterFridge = lastX.end - fridgeEnd;

                    Log($"🧊 Fridge in last space of wall {wallX + 1}, size={spaceLen}, afterFridge={afterFridge}");

                    // Case 1: Fridge in usable space (145–235) with at least 60cm left and no big space in wallY
                    if (spaceLen <= 235 &&
                        !spacesY.Any(s => (s.end - s.start) > 250) &&
                        afterFridge > 60 &&
                        !HasWindow(fridgeStart, fridgeEnd, windowsX))
                    {
                        Log("✅ Valid L-Shape (Fridge in last space wallX, sink/cooktop not possible)");
                        return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2, fridgeWall, fridgeStart, fridgeEnd, true, false);
                    }

                    // Case 2: Long space (>235), enough room after fridge, and wallY has large enough space for sink/cooktop
                    if (spaceLen > 235 &&
                        spacesY.Any(s => (s.end - s.start) > 150) &&
                        afterFridge > 150 &&
                        !HasWindow(fridgeStart, fridgeEnd, windowsX))
                    {
                        Log("✅ Valid L-Shape (Fridge in last space wallX, sink/cooktop possible)");
                        return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2, fridgeWall, fridgeStart, fridgeEnd, true, false);
                    }

                    // If conditions not met, return false
                    return false;
                }



                // Fridge in the space before last in wallX
                if (spacesX.Count >= 2)
                {
                    var beforeLast = spacesX[^2];
                    double len = beforeLast.end - beforeLast.start;
                    Log($"🧊 Fridge in before-last space of wall {wallX + 1}, size={len}");

                    if ((len > 175 && spacesY.Any(s => (s.end - s.start) > 150) ||
                         (len >= 85 && spacesX.Any(s => (s.end - s.start) > 150) && spacesY.Any(s => (s.end - s.start) > 150))) &&
                        !HasWindow(fridgeStart, fridgeEnd, windowsX))
                    {
                        Log("✅ Valid L-Shape (Fridge in before-last space wallX)");
                        return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2, fridgeWall, fridgeStart, fridgeEnd, true, false);
                    }
                }
            }

            // Fridge in wallY (larger index wall)
            if (fridgeWall == wallY + 1)
            {
                // Fridge in the first space of wallY
                if (fridgeStart >= spacesY[0].start && fridgeEnd <= spacesY[0].end)
                {
                    double spaceLen = spacesY[0].end - spacesY[0].start;

                    if (spaceLen < 145)
                    {
                        Log($"⛔ Skipping: fridge is in first space of wall {wallY + 1}, but spaceLen={spaceLen} < 145");
                        return false;
                    }

                    double beforeFridge = fridgeStart - spacesY[0].start;
                    Log($"🧊 Fridge in first space of wall {wallY + 1}, size={spaceLen}, beforeFridge={beforeFridge}");

                    if (spaceLen >= 145 && spaceLen <= 235 &&
                        !spacesX.Any(s => (s.end - s.start) > 250) &&
                        beforeFridge > 60 &&
                        !HasWindow(fridgeStart, fridgeEnd, windowsY))
                    {
                        Log("✅ Valid L-Shape (Fridge in first space wallY, sink/cooktop not possible)");
                        return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2, fridgeWall, fridgeStart, fridgeEnd, true, false);
                    }

                    if (spaceLen > 235 &&
                        spacesX.Any(s => (s.end - s.start) > 150) &&
                        beforeFridge > 150 &&
                        !HasWindow(fridgeStart, fridgeEnd, windowsY))
                    {
                        Log("✅ Valid L-Shape (Fridge in first space wallY, sink/cooktop possible)");
                        return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2, fridgeWall, fridgeStart, fridgeEnd, true, false);
                    }
                }



                // Fridge in the second space of wallY
                if (spacesY.Count >= 2)
                {
                    var second = spacesY[1];
                    double len = second.end - second.start;
                    Log($"🧊 Fridge in second space of wall {wallY + 1}, size={len}");

                    if ((len > 175 && spacesX.Any(s => (s.end - s.start) > 150) ||
                         (len >= 85 && spacesX.Any(s => (s.end - s.start) > 150) && spacesY.Any(s => (s.end - s.start) > 150))) &&
                        !HasWindow(fridgeStart, fridgeEnd, windowsY))
                    {
                        Log("✅ Valid L-Shape (Fridge in second space wallY)");
                        return WriteSuccess(outputPath, wall1Index, wall2Index, spacesWall1, spacesWall2, fridgeWall, fridgeStart, fridgeEnd, true, false);
                    }
                }
            }
            return false;


        }


        private static bool WriteSuccess(string outputPath, int wall1, int wall2,
            List<(double start, double end)> s1,
            List<(double start, double end)> s2,
            int fridgeWall, double fridgeStart, double fridgeEnd,
            bool corner, bool exposed)
        {
            var wall1Spaces = s1.Select(space => new { Start = space.start, End = space.end }).ToList();
            var wall2Spaces = s2.Select(space => new { Start = space.start, End = space.end }).ToList();

            var json = new
            {
                Title = "LShape",
                Wall1 = wall1 + 1,
                Wall2 = wall2 + 1,
                SpacesWall1 = wall1Spaces,
                SpacesWall2 = wall2Spaces,
                FridgeWall = fridgeWall,
                Fridge = new { Start = fridgeStart, End = fridgeEnd },
                Corner = corner,
                Exposed = exposed
            };

            File.WriteAllText(outputPath, JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }));
            Log($"💾 Saved valid L-Shape configuration to: {outputPath}");
            return true;
        }

    }
}
