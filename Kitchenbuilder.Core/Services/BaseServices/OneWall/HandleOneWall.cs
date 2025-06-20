using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kitchenbuilder.Core
{
    public static class HandleOneWall
    {
        public static (
            Dictionary<int, (string appliance, double start, double end)> layout,
            Dictionary<int, (double start, double end)> suggestedBases,
            Dictionary<int, string> suggestedDescriptions)
        Evaluate(
            Kitchen kitchen,
            Dictionary<int, List<(double start, double end)>> emptySpaces)
        {
            var layout = new Dictionary<int, (string appliance, double start, double end)>();
            var suggestedBases = new Dictionary<int, (double start, double end)>();
            var suggestedDescriptions = new Dictionary<int, string>();

            //Line shape 

            bool handled = LineShapeSelectorOneWall.TryFindWallForLineShape(
                kitchen,
                emptySpaces
             );

            if (handled)
            {
                Console.WriteLine("✅ Line shape handled and saved.");
            }
            else
            {
                Console.WriteLine("❌ No suitable line shape found.");
            }

            // Try L-Shape One Wall
            bool lShapeHandled = LShapeSelectorOneWall.TryFindWallsForLShape(
                kitchen,
                emptySpaces
            );

            if (lShapeHandled)
            {
                Console.WriteLine("✅ L-shape (one-wall) handled and saved.");
            }
            else
            {
                Console.WriteLine("❌ No suitable L-shape (one-wall) found.");
            }



            return (layout, suggestedBases, suggestedDescriptions);
        }
    }
}
