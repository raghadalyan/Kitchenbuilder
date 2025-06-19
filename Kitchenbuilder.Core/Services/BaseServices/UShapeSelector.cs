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

            if (!corner)
            {
                Log("❌ No corner found.");
                return false;
            }

            Log("🔍 Corner found between Wall 1 and Wall 2");

            // ================================================
            // 📐 U-Shape Logic when Exposed Wall is Wall 3
            // ================================================
            if (floorWidth >= floorLength)
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

            // ================================================
            // 📐 U-Shape Logic when Exposed Wall is Wall 4
            // ================================================
            else
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

            return false;
        }
    }
}
