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
        private static readonly string OutputPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\Option3.json";

        private static void Log(string msg) => File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n");

        public static bool TryFindWallsForUShape(Kitchen kitchen, Dictionary<int, List<(double start, double end)>> simpleEmptySpaces)
        {
            double floorWidth = kitchen.Floor.Width;
            double floorLength = kitchen.Floor.Length;

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

            var lastSpaceW1 = spacesWall1.Last();
            var firstSpaceW2 = spacesWall2.First();

            bool corner =
                Math.Abs(lastSpaceW1.end - wall1.Width) < 1 &&
                firstSpaceW2.start < 1 &&
                (lastSpaceW1.end - lastSpaceW1.start) > 60 &&
                (firstSpaceW2.end - firstSpaceW2.start) > 60;



            // ================================================
            // 📐 U-Shape Logic when Exposed Wall is Wall 3
            // ================================================
            if (floorWidth >= floorLength)
            {
                if (corner)
                {
                    Log("📐 Exposed wall is Wall 3. Trying fridge in last space of Wall 1.");

                    double fridgeStart = lastSpaceW1.start;
                    double fridgeEnd = fridgeStart + 85;

                    if (UShapeChecker.EvaluateUShape(
                            kitchen,
                            wall1Index,
                            wall2Index,
                            spacesWall1,
                            spacesWall2,
                            floorWidth,
                            3,
                            OutputPath,
                            exposed: true,
                            corner: true,
                            fridgeWall: wall1Index,
                            fridgeStart: fridgeStart,
                            fridgeEnd: fridgeEnd))
                    {
                        Log("✅ Fridge placed in last space of Wall 1");
                        return true;
                    }

                    // Try earlier spaces in Wall 1
                    for (int i = spacesWall1.Count - 2; i >= 0; i--)
                    {
                        var space = spacesWall1[i];
                        if ((space.end - space.start) >= 85)
                        {
                            fridgeStart = space.start;
                            fridgeEnd = fridgeStart + 85;

                            Log($"🔁 Trying previous space {i} in Wall 1 from {fridgeStart} to {fridgeEnd}");

                            if (UShapeChecker.EvaluateUShape(
                                    kitchen,
                                    wall1Index,
                                    wall2Index,
                                    spacesWall1,
                                    spacesWall2,
                                    floorWidth,
                                    3,
                                    OutputPath,
                                    exposed: true,
                                    corner: true,
                                    fridgeWall: wall1Index,
                                    fridgeStart: fridgeStart,
                                    fridgeEnd: fridgeEnd))
                            {
                                Log($"✅ Fridge placed in previous space {i} of Wall 1");
                                return true;
                            }
                        }
                    }

                    Log("❌ Could not place fridge in any space of Wall 1");
                }
                else
                {
                    Log("📐 No corner. Exposed wall is Wall 3. Trying smart placement on Wall 1 then Wall 2.");

                    // 1. Try placing fridge in largest spaces of Wall 1 (descending by size)
                    var sortedWall1Spaces = spacesWall1.OrderByDescending(s => s.end - s.start).ToList();

                    foreach (var space in sortedWall1Spaces)
                    {
                        double spaceLength = space.end - space.start;
                        if (spaceLength < 85) continue;

                        // Try placing at start
                        double fridgeStart = space.start;
                        double fridgeEnd = fridgeStart + 85;

                        Log($"🔍 Trying space in Wall 1 at start: {fridgeStart} → {fridgeEnd}");
                        if (UShapeChecker.EvaluateUShape(
                                kitchen,
                                wall1Index,
                                wall2Index,
                                spacesWall1,
                                spacesWall2,
                                floorWidth,
                                3,
                                OutputPath,
                                exposed: true,
                                corner: false,
                                fridgeWall: wall1Index,
                                fridgeStart: fridgeStart,
                                fridgeEnd: fridgeEnd))
                        {
                            Log("✅ Fridge placed at start of space in Wall 1");
                            return true;
                        }

                        // Try placing at end
                        fridgeEnd = space.end;
                        fridgeStart = fridgeEnd - 85;

                        Log($"🔍 Trying space in Wall 1 at end: {fridgeStart} → {fridgeEnd}");
                        if (UShapeChecker.EvaluateUShape(
                                kitchen,
                                wall1Index,
                                wall2Index,
                                spacesWall1,
                                spacesWall2,
                                floorWidth,
                                3,
                                OutputPath,
                                exposed: true,
                                corner: false,
                                fridgeWall: wall1Index,
                                fridgeStart: fridgeStart,
                                fridgeEnd: fridgeEnd))
                        {
                            Log("✅ Fridge placed at end of space in Wall 1");
                            return true;
                        }
                    }

                    // 2. If all failed, try first space in Wall 2 at the start
                    var fallbackFirstSpaceW2 = spacesWall2[0];
                    if ((fallbackFirstSpaceW2.end - fallbackFirstSpaceW2.start) >= 85)
                    {
                        double fridgeStart = fallbackFirstSpaceW2.start;
                        double fridgeEnd = fridgeStart + 85;

                        Log($"🔍 Trying fallback: Wall 2, first space from {fridgeStart} → {fridgeEnd}");
                        if (UShapeChecker.EvaluateUShape(
                                kitchen,
                                wall1Index,
                                wall2Index,
                                spacesWall1,
                                spacesWall2,
                                floorWidth,
                                3,
                                OutputPath,
                                exposed: true,
                                corner: false,
                                fridgeWall: wall2Index,
                                fridgeStart: fridgeStart,
                                fridgeEnd: fridgeEnd))
                        {
                            Log("✅ Fallback success: Fridge placed at start of first space in Wall 2");
                            return true;
                        }
                    }

                    Log("❌ No valid placement found in Wall 1 or fallback in Wall 2 (no corner)");
                }

            }

            // ================================================
            // 📐 U-Shape Logic when Exposed Wall is Wall 4
            // ================================================
            else
            {
                if (corner)
                {
                    Log("📐 Exposed wall is Wall 4. Trying fridge in first space of Wall 2.");

                    double fridgeEnd = firstSpaceW2.end;
                    double fridgeStart = fridgeEnd - 85;

                    if (UShapeChecker.EvaluateUShape(
                            kitchen,
                            wall1Index,
                            wall2Index,
                            spacesWall1,
                            spacesWall2,
                            floorLength,
                            4,
                            OutputPath,
                            exposed: true,
                            corner: true,
                            fridgeWall: wall2Index,
                            fridgeStart: fridgeStart,
                            fridgeEnd: fridgeEnd))
                    {
                        Log("✅ Fridge placed in first space of Wall 2");
                        return true;
                    }

                    // Try next spaces in Wall 2
                    for (int i = 1; i < spacesWall2.Count; i++)
                    {
                        var space = spacesWall2[i];
                        if ((space.end - space.start) >= 85)
                        {
                            fridgeEnd = space.end;
                            fridgeStart = fridgeEnd - 85;

                            Log($"🔁 Trying next space {i} in Wall 2 from {fridgeStart} to {fridgeEnd}");

                            if (UShapeChecker.EvaluateUShape(
                                    kitchen,
                                    wall1Index,
                                    wall2Index,
                                    spacesWall1,
                                    spacesWall2,
                                    floorLength,
                                    4,
                                    OutputPath,
                                    exposed: true,
                                    corner: true,
                                    fridgeWall: wall2Index,
                                    fridgeStart: fridgeStart,
                                    fridgeEnd: fridgeEnd))
                            {
                                Log($"✅ Fridge placed in next space {i} of Wall 2");
                                return true;
                            }
                        }
                    }

                    Log("❌ Could not place fridge in any space of Wall 2");
                }

                else
                {
                    Log("📐 No corner. Exposed wall is Wall 4. Trying smart placement on Wall 2 then fallback on Wall 1.");

                    // 1. Sort Wall 2's empty spaces by length (descending)
                    var sortedWall2Spaces = spacesWall2.OrderByDescending(s => s.end - s.start).ToList();

                    foreach (var space in sortedWall2Spaces)
                    {
                        double spaceLength = space.end - space.start;
                        if (spaceLength < 85) continue;

                        // Try placing at end
                        double fridgeEnd = space.end;
                        double fridgeStart = fridgeEnd - 85;

                        Log($"🔍 Trying space in Wall 2 at end: {fridgeStart} → {fridgeEnd}");
                        if (UShapeChecker.EvaluateUShape(
                                kitchen,
                                wall1Index,
                                wall2Index,
                                spacesWall1,
                                spacesWall2,
                                floorLength,
                                4,
                                OutputPath,
                                exposed: true,
                                corner: false,
                                fridgeWall: wall2Index,
                                fridgeStart: fridgeStart,
                                fridgeEnd: fridgeEnd))
                        {
                            Log("✅ Fridge placed at end of space in Wall 2");
                            return true;
                        }

                        // Try placing at start
                        fridgeStart = space.start;
                        fridgeEnd = fridgeStart + 85;

                        Log($"🔍 Trying space in Wall 2 at start: {fridgeStart} → {fridgeEnd}");
                        if (UShapeChecker.EvaluateUShape(
                                kitchen,
                                wall1Index,
                                wall2Index,
                                spacesWall1,
                                spacesWall2,
                                floorLength,
                                4,
                                OutputPath,
                                exposed: true,
                                corner: false,
                                fridgeWall: wall2Index,
                                fridgeStart: fridgeStart,
                                fridgeEnd: fridgeEnd))
                        {
                            Log("✅ Fridge placed at start of space in Wall 2");
                            return true;
                        }
                    }

                    // 2. Fallback: Try placing in the last space of Wall 1 at the end
                    var lastSpaceW1Fallback = spacesWall1[^1];
                    if ((lastSpaceW1Fallback.end - lastSpaceW1Fallback.start) >= 85)
                    {
                        double fridgeEnd = lastSpaceW1Fallback.end;
                        double fridgeStart = fridgeEnd - 85;

                        Log($"🔍 Fallback: Trying end of last space in Wall 1 from {fridgeStart} → {fridgeEnd}");
                        if (UShapeChecker.EvaluateUShape(
                                kitchen,
                                wall1Index,
                                wall2Index,
                                spacesWall1,
                                spacesWall2,
                                floorLength,
                                4,
                                OutputPath,
                                exposed: true,
                                corner: false,
                                fridgeWall: wall1Index,
                                fridgeStart: fridgeStart,
                                fridgeEnd: fridgeEnd))
                        {
                            Log("✅ Fallback success: Fridge placed at end of last space in Wall 1");
                            return true;
                        }
                    }

                    Log("❌ No valid placement found in Wall 2 or fallback in Wall 1 (no corner)");
                }


            }

            return false;
        }
    }
}
