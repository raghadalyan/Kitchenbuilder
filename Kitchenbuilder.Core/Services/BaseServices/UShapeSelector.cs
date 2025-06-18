using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kitchenbuilder.Core
{
    public static class UShapeSelector
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Ushapeselector.txt";

        private static void LogDebug(string message)
        {
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }

        public static bool TryFindWallsForUShape(Kitchen kitchen, Dictionary<int, List<(double start, double end)>> simpleEmptySpaces)
        {
            var floorWidth = kitchen.Floor.Width;
            var floorLength = kitchen.Floor.Length;

            if (!simpleEmptySpaces.ContainsKey(0) || !simpleEmptySpaces.ContainsKey(1))
                return false;

            int wall1Index = 0;
            int wall2Index = 1;

            var wall1 = kitchen.Walls[wall1Index];
            var wall2 = kitchen.Walls[wall2Index];
            var spacesWall1 = simpleEmptySpaces[wall1Index];
            var spacesWall2 = simpleEmptySpaces[wall2Index];

            if (spacesWall1.Count == 0 || spacesWall2.Count == 0)
                return false;

            var lastSpaceW1 = spacesWall1[^1];
            var firstSpaceW2 = spacesWall2[0];

            string outputPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\Option3.json";

            bool corner =
                Math.Abs(lastSpaceW1.end - wall1.Width) < 1 &&
                firstSpaceW2.start < 1 &&
                (lastSpaceW1.end - lastSpaceW1.start) > 60 &&
                (firstSpaceW2.end - firstSpaceW2.start) > 60;

            if (floorWidth >= floorLength)
            {
                // Wall 3 Exposed
                if (corner)
                {
                    LogDebug("🔍 Corner detected between Wall 1 and Wall 2");

                    double fridgeStart1 = lastSpaceW1.start;
                    double fridgeEnd1 = fridgeStart1 + 85;
                    LogDebug($"🚪 Option 1: Trying fridge in Wall 1 from {fridgeStart1} to {fridgeEnd1}");

                    if (UShapeChecker.EvaluateUShape(kitchen, wall1Index, wall2Index, spacesWall1, spacesWall2, floorWidth, 3,
                        outputPath, true, true, 1, fridgeStart1, fridgeEnd1))
                        return true;

                    if (spacesWall1.Count >= 2)
                    {
                        var prevSpace = spacesWall1[^2];
                        if ((prevSpace.end - prevSpace.start) >= 85)
                        {
                            double fridgeStart = prevSpace.start;
                            double fridgeEnd = fridgeStart + 85;
                            LogDebug($"🚪 Option 3: Trying previous space in Wall 1 from {fridgeStart} to {fridgeEnd}");

                            if (UShapeChecker.EvaluateUShape(kitchen, wall1Index, wall2Index, spacesWall1, spacesWall2, floorWidth, 3,
                                outputPath, true, false, 1, fridgeStart, fridgeEnd))
                                return true;
                        }
                    }
                }
                else
                {
                    var largest = spacesWall1.OrderByDescending(s => s.end - s.start).FirstOrDefault(s => s.end - s.start >= 85);
                    if (largest != default)
                    {
                        double fridgeStart = largest.start;
                        double fridgeEnd = fridgeStart + 85;
                        LogDebug($"🚪 Trying start of largest Wall 1 space from {fridgeStart} to {fridgeEnd}");

                        if (UShapeChecker.EvaluateUShape(kitchen, wall1Index, wall2Index, spacesWall1, spacesWall2, floorWidth, 3,
                            outputPath, true, false, 1, fridgeStart, fridgeEnd))
                            return true;

                        fridgeEnd = largest.end;
                        fridgeStart = fridgeEnd - 85;
                        LogDebug($"🚪 Trying end of largest Wall 1 space from {fridgeStart} to {fridgeEnd}");

                        if (UShapeChecker.EvaluateUShape(kitchen, wall1Index, wall2Index, spacesWall1, spacesWall2, floorWidth, 3,
                            outputPath, true, false, 1, fridgeStart, fridgeEnd))
                            return true;

                        foreach (var space in spacesWall1.OrderByDescending(s => s.end - s.start))
                        {
                            if ((space.end - space.start) < 85) continue;
                            fridgeStart = space.start;
                            fridgeEnd = fridgeStart + 85;
                            LogDebug($"🚪 Fallback: Trying Wall 1 space from {fridgeStart} to {fridgeEnd}");

                            if (UShapeChecker.EvaluateUShape(kitchen, wall1Index, wall2Index, spacesWall1, spacesWall2, floorWidth, 3,
                                outputPath, true, false, 1, fridgeStart, fridgeEnd))
                                return true;
                        }
                    }

                    var firstW2 = spacesWall2.FirstOrDefault();
                    if ((firstW2.end - firstW2.start) >= 85)
                    {
                        double fridgeStart = firstW2.start;
                        double fridgeEnd = fridgeStart + 85;
                        LogDebug($"🚪 Fallback: Trying Wall 2 with Wall 3 exposed from {fridgeStart} to {fridgeEnd}");

                        if (UShapeChecker.EvaluateUShape(kitchen, wall1Index, wall2Index, spacesWall1, spacesWall2, floorWidth, 3,
                            outputPath, true, false, 2, fridgeStart, fridgeEnd))
                            return true;
                    }
                }
            }
            else
            {
                // Wall 4 Exposed
                if (corner)
                {
                    double fridgeEnd2 = firstSpaceW2.end;
                    double fridgeStart2 = fridgeEnd2 - 85;
                    LogDebug($"🚪 Option 2: Trying fridge in Wall 2 from {fridgeStart2} to {fridgeEnd2}");

                    if (UShapeChecker.EvaluateUShape(kitchen, wall1Index, wall2Index, spacesWall1, spacesWall2, floorLength, 4,
                        outputPath, true, true, 2, fridgeStart2, fridgeEnd2))
                        return true;

                    if (spacesWall2.Count >= 2)
                    {
                        var secondSpace = spacesWall2[1];
                        if ((secondSpace.end - secondSpace.start) >= 85)
                        {
                            double fridgeEnd = secondSpace.end;
                            double fridgeStart = fridgeEnd - 85;
                            LogDebug($"🚪 Option 4: Trying second space in Wall 2 from {fridgeStart} to {fridgeEnd}");

                            if (UShapeChecker.EvaluateUShape(kitchen, wall1Index, wall2Index, spacesWall1, spacesWall2, floorLength, 4,
                                outputPath, true, false, 2, fridgeStart, fridgeEnd))
                                return true;
                        }
                    }
                }
                else
                {
                    var largest = spacesWall2.OrderByDescending(s => s.end - s.start).FirstOrDefault(s => s.end - s.start >= 85);
                    if (largest != default)
                    {
                        double fridgeEnd = largest.end;
                        double fridgeStart = fridgeEnd - 85;
                        LogDebug($"🚪 Trying end of largest Wall 2 space from {fridgeStart} to {fridgeEnd}");

                        if (UShapeChecker.EvaluateUShape(kitchen, wall1Index, wall2Index, spacesWall1, spacesWall2, floorLength, 4,
                            outputPath, true, false, 2, fridgeStart, fridgeEnd))
                            return true;

                        fridgeStart = largest.start;
                        fridgeEnd = fridgeStart + 85;
                        LogDebug($"🚪 Trying start of largest Wall 2 space from {fridgeStart} to {fridgeEnd}");

                        if (UShapeChecker.EvaluateUShape(kitchen, wall1Index, wall2Index, spacesWall1, spacesWall2, floorLength, 4,
                            outputPath, true, false, 2, fridgeStart, fridgeEnd))
                            return true;

                        foreach (var space in spacesWall2.OrderByDescending(s => s.end - s.start))
                        {
                            if ((space.end - space.start) < 85) continue;
                            fridgeEnd = space.end;
                            fridgeStart = fridgeEnd - 85;
                            LogDebug($"🚪 Fallback: Trying Wall 2 space from {fridgeStart} to {fridgeEnd}");

                            if (UShapeChecker.EvaluateUShape(kitchen, wall1Index, wall2Index, spacesWall1, spacesWall2, floorLength, 4,
                                outputPath, true, false, 2, fridgeStart, fridgeEnd))
                                return true;
                        }
                    }

                    var firstW1 = spacesWall1.FirstOrDefault();
                    if ((firstW1.end - firstW1.start) >= 85)
                    {
                        double fridgeEnd = firstW1.end;
                        double fridgeStart = fridgeEnd - 85;
                        LogDebug($"🚪 Fallback: Trying Wall 1 with Wall 4 exposed from {fridgeStart} to {fridgeEnd}");

                        if (UShapeChecker.EvaluateUShape(kitchen, wall1Index, wall2Index, spacesWall1, spacesWall2, floorLength, 4,
                            outputPath, true, false, 1, fridgeStart, fridgeEnd))
                            return true;
                    }
                }
            }

            LogDebug("❌ No valid U-shape placement found");
            return false;
        }
    }
}
