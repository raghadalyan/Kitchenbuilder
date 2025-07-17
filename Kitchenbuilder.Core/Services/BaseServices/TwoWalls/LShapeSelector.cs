using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kitchenbuilder.Core
{
    public static class LShapeSelector
    {
        private static readonly string DebugPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Output", "HandleTwoWalls.txt"
        );

        private static void LogDebug(string message)
        {
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }

        public static bool TryFindWallsForLShape(
            Kitchen kitchen,
            Dictionary<int, List<(double start, double end)>> simpleEmptySpaces)
        {
            string outputPath = Path.Combine(
                KitchenConfig.Get().BasePath,
                "Kitchenbuilder", "Kitchenbuilder", "JSON", "Option2.json"
            );

            // 1. Try classic L-shape: Wall 1 (index 0) and Wall 2 (index 1)
            if (simpleEmptySpaces.ContainsKey(0) && simpleEmptySpaces.ContainsKey(1))
            {
                var spacesWall1 = simpleEmptySpaces[0];
                var spacesWall2 = simpleEmptySpaces[1];

                if (spacesWall1.Count > 0 && spacesWall2.Count > 0)
                {
                    var wall1 = kitchen.Walls[0];
                    var wall2 = kitchen.Walls[1];
                    var lastSpaceW1 = spacesWall1[^1];
                    var firstSpaceW2 = spacesWall2[0];

                    bool corner = lastSpaceW1.end == wall1.Width &&
                                  firstSpaceW2.start == 0 &&
                                  (lastSpaceW1.end - lastSpaceW1.start) > 60 &&
                                  (firstSpaceW2.end - firstSpaceW2.start) > 60;

                    if (corner)
                    {
                        LogDebug("🔍 Corner detected between Wall 1 and Wall 2");

                        // Option 1: Fridge in last space of wall 1
                        double fridgeStart1 = lastSpaceW1.start;
                        double fridgeEnd1 = fridgeStart1 + 85;
                        LogDebug($"🚪 Option 1: Trying fridge in Wall 1 from {fridgeStart1} to {fridgeEnd1}");

                        if (LShapeChecker.EvaluateLShape(
                            kitchen, 0, 1, spacesWall1, spacesWall2, 0, -1, outputPath,
                            false, true, 1, fridgeStart1, fridgeEnd1))
                        {
                            LogDebug("✅ Success: Fridge placed in Wall 1 (Option 1)");
                            return true;
                        }

                        // Option 2: Fridge in first space of wall 2
                        double fridgeEnd2 = firstSpaceW2.end;
                        double fridgeStart2 = fridgeEnd2 - 85;
                        LogDebug($"🚪 Option 2: Trying fridge in Wall 2 from {fridgeStart2} to {fridgeEnd2}");

                        if (LShapeChecker.EvaluateLShape(
                            kitchen, 0, 1, spacesWall1, spacesWall2, 0, -1, outputPath,
                            false, true, 2, fridgeStart2, fridgeEnd2))
                        {
                            LogDebug("✅ Success: Fridge placed in Wall 2 (Option 2)");
                            return true;
                        }

                        // Option 3: Previous space in Wall 1
                        if (spacesWall1.Count >= 2)
                        {
                            var prevSpaceW1 = spacesWall1[^2];
                            if ((prevSpaceW1.end - prevSpaceW1.start) >= 85)
                            {
                                double fridgeStart = prevSpaceW1.start;
                                double fridgeEnd = fridgeStart + 85;
                                LogDebug($"🚪 Option 3: Trying previous space in Wall 1 from {fridgeStart} to {fridgeEnd}");

                                if (LShapeChecker.EvaluateLShape(
                                    kitchen, 0, 1, spacesWall1, spacesWall2, 0, -1, outputPath,
                                    false, true, 1, fridgeStart, fridgeEnd))
                                {
                                    LogDebug("✅ Success: Fridge placed in previous space of Wall 1 (Option 3)");
                                    return true;
                                }
                            }
                        }

                        // Option 4: Second space in Wall 2 (place fridge in last 85 cm of space)
                        if (spacesWall2.Count >= 2)
                        {
                            var secondSpaceW2 = spacesWall2[1];
                            if ((secondSpaceW2.end - secondSpaceW2.start) >= 85)
                            {
                                double fridgeEnd = secondSpaceW2.end;
                                double fridgeStart = fridgeEnd - 85;
                                LogDebug($"🚪 Option 4: Trying second space in Wall 2 (last 85cm) from {fridgeStart} to {fridgeEnd}");

                                if (LShapeChecker.EvaluateLShape(
                                    kitchen, 0, 1, spacesWall1, spacesWall2, 0, -1, outputPath,
                                    false, true, 2, fridgeStart, fridgeEnd))
                                {
                                    LogDebug("✅ Success: Fridge placed in second space of Wall 2 (Option 4)");
                                    return true;
                                }
                            }
                        }


                        LogDebug("❌ No valid corner placement found");
                    }
                    //--------------------------------------------------------------------------------------
                    if (!corner)
                    {
                        LogDebug("🔍 No corner found – trying L-shape using largest empty space");

                        // Combine all spaces with wall index
                        var allSpaces = new List<(int wall, double start, double end)>();
                        allSpaces.AddRange(spacesWall1.Select(s => (wall: 1, start: s.start, end: s.end)));
                        allSpaces.AddRange(spacesWall2.Select(s => (wall: 2, start: s.start, end: s.end)));

                        // Sort by descending length
                        var sortedSpaces = allSpaces
                            .Where(s => (s.end - s.start) >= 85)
                            .OrderByDescending(s => s.end - s.start)
                            .ToList();

                        foreach (var space in sortedSpaces)
                        {
                            double spaceLength = space.end - space.start;

                            // Try end first if space is in Wall 2
                            if (space.wall == 2)
                            {
                                // Option A: Place fridge at end of the space
                                double fridgeEnd = space.end;
                                double fridgeStart = fridgeEnd - 85;
                                LogDebug($"🚪 Trying Wall 2 (end of space) from {fridgeStart} to {fridgeEnd}");

                                if (LShapeChecker.EvaluateLShape(
                                    kitchen, 0, 1, spacesWall1, spacesWall2, 0, -1, outputPath,
                                    false, false, space.wall, fridgeStart, fridgeEnd))
                                {
                                    LogDebug("✅ Success: Fridge placed at end of Wall 2 space");
                                    return true;
                                }

                                // Option B: Try start of the space
                                fridgeStart = space.start;
                                fridgeEnd = fridgeStart + 85;
                                LogDebug($"🚪 Trying Wall 2 (start of space) from {fridgeStart} to {fridgeEnd}");

                                if (LShapeChecker.EvaluateLShape(
                                    kitchen, 0, 1, spacesWall1, spacesWall2, 0, -1, outputPath,
                                    false, false, space.wall, fridgeStart, fridgeEnd))
                                {
                                    LogDebug("✅ Success: Fridge placed at start of Wall 2 space");
                                    return true;
                                }
                            }
                            else
                            {
                                // Wall 1 logic – start first
                                double fridgeStart = space.start;
                                double fridgeEnd = fridgeStart + 85;
                                LogDebug($"🚪 Trying Wall 1 (start of space) from {fridgeStart} to {fridgeEnd}");

                                if (LShapeChecker.EvaluateLShape(
                                    kitchen, 0, 1, spacesWall1, spacesWall2, 0, -1, outputPath,
                                    false, false, space.wall, fridgeStart, fridgeEnd))
                                {
                                    LogDebug("✅ Success: Fridge placed at start of Wall 1 space");
                                    return true;
                                }

                                // Try end of space second
                                fridgeEnd = space.end;
                                fridgeStart = fridgeEnd - 85;
                                LogDebug($"🚪 Trying Wall 1 (end of space) from {fridgeStart} to {fridgeEnd}");

                                if (LShapeChecker.EvaluateLShape(
                                    kitchen, 0, 1, spacesWall1, spacesWall2, 0, -1, outputPath,
                                    false, false, space.wall, fridgeStart, fridgeEnd))
                                {
                                    LogDebug("✅ Success: Fridge placed at end of Wall 1 space");
                                    return true;
                                }
                            }
                        }

                        LogDebug("❌ No valid L-shape placement found (non-corner)");
                    }





                }
            }



            // 2. Fallback – check wall with largest space
            double max1 = simpleEmptySpaces.ContainsKey(0) ? simpleEmptySpaces[0].Max(s => s.end - s.start) : 0;
            double max2 = simpleEmptySpaces.ContainsKey(1) ? simpleEmptySpaces[1].Max(s => s.end - s.start) : 0;

            if (max1 >= max2 && max1 > 0)
            {
                var spacesWall1 = simpleEmptySpaces[0];
                var wall4Spaces = new List<(double start, double end)> { (0, kitchen.Floor.Width) };

                double fridgeStart = spacesWall1[0].start;
                double fridgeEnd = fridgeStart + 85;

                LogDebug($"🔄 Fallback: Trying Wall 1 with exposed Wall 4 from {fridgeStart} to {fridgeEnd}");

                if (LShapeChecker.EvaluateLShape(
                    kitchen, 0, 3, spacesWall1, wall4Spaces, kitchen.Floor.Width, 3,
                    outputPath, true, false, 1, fridgeStart, fridgeEnd))
                {
                    LogDebug("✅ Success: Fallback fridge placed in Wall 1 (Wall 4 exposed)");
                    return true;
                }
            }
            else if (max2 > 0)
            {
                var spacesWall2 = simpleEmptySpaces[1];
                var wall3Spaces = new List<(double start, double end)> { (0, kitchen.Floor.Length) };

                double fridgeStart = spacesWall2[0].start;
                double fridgeEnd = fridgeStart + 85;

                LogDebug($"🔄 Fallback: Trying Wall 2 with exposed Wall 3 from {fridgeStart} to {fridgeEnd}");

                if (LShapeChecker.EvaluateLShape(
                    kitchen, 1, 2, spacesWall2, wall3Spaces, kitchen.Floor.Length, 2,
                    outputPath, true, false, 1, fridgeStart, fridgeEnd))
                {
                    LogDebug("✅ Success: Fallback fridge placed in Wall 2 (Wall 3 exposed)");
                    return true;
                }
            }

            LogDebug("❌ No valid fallback placement found");
            return false;
        }
    }
}
