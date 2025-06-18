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

            int realWallX = Math.Min(wall1Index, wall2Index);
            int realWallY = Math.Max(wall1Index, wall2Index);
            var realSpacesWallX = realWallX == wall1Index ? spacesWall1 : spacesWall2;
            var realSpacesWallY = realWallY == wall1Index ? spacesWall1 : spacesWall2;

            var lastSpaceW1 = spacesWall1[^1];
            var firstSpaceW2 = spacesWall2[0];

            string outputPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\Option3.json";

            if (floorWidth >= floorLength)
            {
                LogDebug("🧱 U-shape attempt with exposed base on Wall 3");

                // Log for debugging
                LogDebug($"lastSpaceW1.end = {lastSpaceW1.end}, wall1.Width = {wall1.Width}");
                LogDebug($"firstSpaceW2.start = {firstSpaceW2.start}");
                LogDebug($"lastSpaceW1 length = {lastSpaceW1.end - lastSpaceW1.start}");
                LogDebug($"firstSpaceW2 length = {firstSpaceW2.end - firstSpaceW2.start}");

                bool corner =
                    Math.Abs(lastSpaceW1.end - wall1.Width) < 1 &&
                    firstSpaceW2.start < 1 &&
                    (lastSpaceW1.end - lastSpaceW1.start) > 60 &&
                    (firstSpaceW2.end - firstSpaceW2.start) > 60;

                if (corner)
                {
                    LogDebug("🔍 Corner detected between Wall 1 and Wall 2");

                    double fridgeStart1 = lastSpaceW1.start;
                    double fridgeEnd1 = fridgeStart1 + 85;
                    LogDebug($"🚪 Option 1: Trying fridge in Wall 1 from {fridgeStart1} to {fridgeEnd1}");

                    if (UShapeChecker.EvaluateUShape(kitchen, realWallX, realWallY, realSpacesWallX, realSpacesWallY, floorWidth, 3,
                        outputPath, true, true, 1, fridgeStart1, fridgeEnd1))
                    {
                        LogDebug("✅ Success: Fridge placed in Wall 1 (Option 1)");
                        return true;
                    }

                    if (spacesWall1.Count >= 2)
                    {
                        var prevSpaceW1 = spacesWall1[^2];
                        if ((prevSpaceW1.end - prevSpaceW1.start) >= 85)
                        {
                            double fridgeStart = prevSpaceW1.start;
                            double fridgeEnd = fridgeStart + 85;
                            LogDebug($"🚪 Option 3: Trying previous space in Wall 1 from {fridgeStart} to {fridgeEnd}");

                            if (UShapeChecker.EvaluateUShape(kitchen, realWallX, realWallY, realSpacesWallX, realSpacesWallY, floorWidth, 3,
                                outputPath, true, false, 1, fridgeStart, fridgeEnd))
                            {
                                LogDebug("✅ Success: Fridge placed in previous space of Wall 1 (Option 3)");
                                return true;
                            }
                        }
                    }
                }
            }
            else
            {
                LogDebug("🧱 U-shape attempt with exposed base on Wall 4");

                double fridgeEnd2 = firstSpaceW2.end;
                double fridgeStart2 = fridgeEnd2 - 85;
                LogDebug($"🚪 Option 2: Trying fridge in Wall 2 from {fridgeStart2} to {fridgeEnd2}");

                if (UShapeChecker.EvaluateUShape(kitchen, realWallX, realWallY, realSpacesWallX, realSpacesWallY, floorLength, 4,
                    outputPath, true, true, 2, fridgeStart2, fridgeEnd2))
                {
                    LogDebug("✅ Success: Fridge placed in Wall 2 (Option 2)");
                    return true;
                }

                if (spacesWall2.Count >= 2)
                {
                    var secondSpaceW2 = spacesWall2[1];
                    if ((secondSpaceW2.end - secondSpaceW2.start) >= 85)
                    {
                        double fridgeEnd = secondSpaceW2.end;
                        double fridgeStart = fridgeEnd - 85;
                        LogDebug($"🚪 Option 4: Trying second space in Wall 2 (last 85cm) from {fridgeStart} to {fridgeEnd}");

                        if (UShapeChecker.EvaluateUShape(kitchen, realWallX, realWallY, realSpacesWallX, realSpacesWallY, floorLength, 4,
                            outputPath, true, false, 2, fridgeStart, fridgeEnd))
                        {
                            LogDebug("✅ Success: Fridge placed in second space of Wall 2 (Option 4)");
                            return true;
                        }
                    }
                }
            }

            LogDebug("❌ No valid U-shape placement found");
            return false;
        }
    }
}
