using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kitchenbuilder.Core
{
    public static class FilterEmptySpaces
    {
        public static Dictionary<int, List<(double, double, bool, bool, bool, bool)>> FilterSpaces(
            Dictionary<int, List<(double, double)>> analyzedSpaces)
        {
            var filteredSpaces = new Dictionary<int, List<(double, double, bool, bool, bool, bool)>>();

            foreach (var kvp in analyzedSpaces)
            {
                int wallIndex = kvp.Key;
                var spaces = kvp.Value;

                var filteredList = new List<(double, double, bool, bool, bool, bool)>();

                foreach (var space in spaces)
                {
                    double width = space.Item2 - space.Item1;

                    if (width >= 30)
                    {
                        bool canCabinet = width >= 30;
                        bool canSink = width >= 90;
                        bool canCooktop = width >= 90;
                        bool canFridge = width >= 85;

                        filteredList.Add((space.Item1, space.Item2, canCabinet, canSink, canCooktop, canFridge));
                    }
                }

                if (filteredList.Any())
                {
                    filteredSpaces[wallIndex] = filteredList;
                }
            }

            // Write to debug.txt
            string debugFilePath = Path.Combine(
                KitchenConfig.Get().BasePath,
                "Kitchenbuilder", "Output", "filtered_spaces.txt"
            );
            using (StreamWriter writer = new StreamWriter(debugFilePath))
            {
                writer.WriteLine("Filtered Empty Spaces:");
                foreach (var kvp in filteredSpaces)
                {
                    writer.WriteLine($"Wall {kvp.Key + 1}:");
                    foreach (var space in kvp.Value)
                    {
                        writer.WriteLine($"  From {space.Item1} cm to {space.Item2} cm | " +
                            $"Cabinet: {space.Item3} | Sink: {space.Item4} | Cooktop: {space.Item5} | Fridge: {space.Item6}");
                    }
                }
            }

            return filteredSpaces;
        }
    }
}
