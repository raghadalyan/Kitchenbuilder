using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
/*
 * LineShapeSelector.cs
 * ---------------------
 * This file is responsible for identifying the best wall to apply a straight-line kitchen layout
 * (fridge + sink + cooktop). It searches for the first wall that contains an empty space of at
 * least 325 cm and delegates the validation and result-saving logic to the LineShapeChecker.
 *
 * Output: Saves evaluation result to a JSON file if a suitable wall is found.
 */

namespace Kitchenbuilder.Core
{
    public static class LineShapeSelector
    {
        const double requiredWidthStraight = 325; // sink + workspace + cooktop + workspace + fridge
        private const string outputPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\Option1.json";

        public static bool TryFindWallForLineShape(
            Kitchen kitchen,
            Dictionary<int, List<(double start, double end)>> simpleEmptySpaces)
        {
            foreach (var wallEntry in simpleEmptySpaces)
            {
                int wallIndex = wallEntry.Key;
                var sortedSpaces = wallEntry.Value
                    .OrderByDescending(s => s.end - s.start)
                    .ToList();

                foreach (var (start, end) in sortedSpaces)
                {
                    if (end - start >= requiredWidthStraight)
                    {
                        // Delegate to checker with the selected wall
                        return LineShapeChecker.EvaluateWall(kitchen, wallIndex, sortedSpaces, outputPath);
                    }
                }
            }

            return false;
        }
    }
}
