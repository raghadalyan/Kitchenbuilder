using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Attempts to place a fridge in a one-wall L-shape kitchen layout.
/// 
/// ✅ Logic:
/// 1. Finds the largest available space on Wall 1 (wall index 0).
/// 2. Tries placing the fridge at the **start** of the largest space (85cm) → exposes Wall 2 (index 1).
/// 3. If that fails, tries placing the fridge at the **end** of the largest space → exposes Wall 4 (index 3).
/// 4. If both fail, looks for other empty spaces ≥85cm in Wall 1:
///     - If the space is **after** the largest → place fridge at end, expose Wall 4.
///     - If the space is **before** the largest → place fridge at start, expose Wall 2.
/// 
/// Uses LShapeChecker.EvaluateLShape to validate the placement logic.
/// 
/// Returns true on first valid placement found; otherwise returns false.
/// </summary>


namespace Kitchenbuilder.Core
{
    public static class LShapeSelectorOneWall
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\LShapeSelectorOneWall.txt";
         static readonly string output  = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\Option2.json";

        private static void LogDebug(string message)
        {
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }

        public static bool TryFindWallsForLShape(Kitchen kitchen, Dictionary<int, List<(double start, double end)>> emptySpaces)
        {
            if (!emptySpaces.ContainsKey(0))
            {
                LogDebug("❌ No empty spaces in Wall 1.");
                return false;
            }

            var wall1Spaces = emptySpaces[0];
            var largest = wall1Spaces.OrderByDescending(s => s.end - s.start).FirstOrDefault();
            double largestLength = largest.end - largest.start;

            if (largestLength < 85)
            {
                LogDebug("❌ No space in Wall 1 is large enough for fridge.");
                return false;
            }

            LogDebug($"🔍 Trying largest space in Wall 1: {largest.start}-{largest.end}");

            // Try placing fridge at start of the largest space (→ expose Wall 2)
            double fridgeStart = largest.start;
            double fridgeEnd = fridgeStart + 85;
            if (LShapeChecker.EvaluateLShape(kitchen, 0, -1, wall1Spaces, null, kitchen.Floor.Length, 2, output, true, false, 1, fridgeStart, fridgeEnd))
            {
                LogDebug("✅ Fridge placed at start of largest space, exposed Wall 2.");
                return true;
            }

            // Try placing fridge at end of the largest space (→ expose Wall 4)
            fridgeEnd = largest.end;
            fridgeStart = fridgeEnd - 85;
            if (LShapeChecker.EvaluateLShape(kitchen, 0, -1, wall1Spaces, null, kitchen.Floor.Length, 4, output, true, false, 1, fridgeStart, fridgeEnd))
            {
                LogDebug("✅ Fridge placed at end of largest space, exposed Wall 4.");
                return true;
            }

            // Try other spaces ≥ 85
            foreach (var space in wall1Spaces)
            {
                double len = space.end - space.start;
                if (len < 85 || space.Equals(largest)) continue;

                LogDebug($"🔁 Trying other space: {space.start}-{space.end}");

                if (space.start > largest.start)
                {
                    fridgeEnd = space.end;
                    fridgeStart = fridgeEnd - 85;
                    if (LShapeChecker.EvaluateLShape(kitchen, 0, -1, wall1Spaces, null, kitchen.Floor.Length, 4, output, true, false, 1, fridgeStart, fridgeEnd))
                    {
                        LogDebug("✅ Fridge in later space, exposed Wall 4.");
                        return true;
                    }
                }
                else
                {
                    fridgeStart = space.start;
                    fridgeEnd = fridgeStart + 85;
                    if (LShapeChecker.EvaluateLShape(kitchen, 0, -1, wall1Spaces, null, kitchen.Floor.Length, 2, output, true, false, 1, fridgeStart, fridgeEnd))
                    {
                        LogDebug("✅ Fridge in earlier space, exposed Wall 2.");
                        return true;
                    }
                }
            }

            LogDebug("❌ No valid L-shape one-wall placement found.");
            return false;
        }
    }
}
