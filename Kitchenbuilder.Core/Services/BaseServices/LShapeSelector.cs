using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kitchenbuilder.Core
{
    public static class LShapeSelector
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\HandleTwoWalls.txt";

        private static void LogDebug(string message)
        {
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }

        public static bool TryFindWallsForLShape(
            Kitchen kitchen,
            Dictionary<int, List<(double start, double end)>> simpleEmptySpaces)
        {
            string outputPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\Option2.json";

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
