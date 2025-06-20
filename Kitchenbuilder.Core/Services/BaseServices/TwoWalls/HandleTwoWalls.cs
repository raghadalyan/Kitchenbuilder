using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class HandleTwoWalls
    {
        public static (Dictionary<int, (string appliance, double start, double end)> layout,
                       Dictionary<int, double> suggestedBases,
                       Dictionary<int, string> suggestedDescriptions)
            Evaluate(Kitchen kitchen, Dictionary<int, List<(double start, double end)>> simpleEmptySpaces)
        {
            //Check LineShape Option 
            bool success = LineShapeSelectorTwoWalls.TryFindWallForLineShape(kitchen, simpleEmptySpaces);

            string outputPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\HandleTwoWalls.txt";
            string message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - LineShapeSelector result: {(success ? "✅ Success" : "❌ Failed")}";
            
            //Check L shape Option 
            bool lShapeSuccess = LShapeSelector.TryFindWallsForLShape(kitchen, simpleEmptySpaces);
            File.AppendAllText(outputPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - LShapeSelector result: {(lShapeSuccess ? "✅ Success" : "❌ Failed")}{Environment.NewLine}");
            // ✅ Check U-shape Option
            bool uShapeSuccess = UShapeSelector.TryFindWallsForUShape(kitchen, simpleEmptySpaces);
            File.AppendAllText(outputPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - UShapeSelector result: {(uShapeSuccess ? "✅ Success" : "❌ Failed")}{Environment.NewLine}");

            File.AppendAllText(outputPath, message + Environment.NewLine);

            return (new Dictionary<int, (string appliance, double start, double end)>(),
                    new Dictionary<int, double>(),
                    new Dictionary<int, string>());
        }
    }
}
