using System;
using System.Collections.Generic;
using System.IO;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core
{
    public static class BaseAnalyzer
    {
        public static Dictionary<int, (double, double)> AnalyzeEmptySpaces(Kitchen kitchen)
        {
            Dictionary<int, (double, double)> emptySpaces = new Dictionary<int, (double, double)>();

            for (int i = 0; i < kitchen.Walls.Count; i++)
            {
                Wall currentWall = kitchen.Walls[i];

                double emptyStart = 0; // default
                double emptyEnd = currentWall.Width;

                if (currentWall.HasDoors && currentWall.Doors != null)
                {
                    foreach (var door in currentWall.Doors)
                    {
                        double spaceAfterDoor = currentWall.Width - door.DistanceX - door.Width;
                        if (spaceAfterDoor < 60)
                        {
                            int nextWallIndex = i + 1;

                            if (nextWallIndex >= kitchen.Walls.Count)
                            {
                                // Only wrap around if there are exactly 4 walls
                                if (kitchen.Walls.Count == 4)
                                {
                                    nextWallIndex = 0;
                                }
                                else
                                {
                                    // Skip applying empty space to a non-existent wall
                                    continue;
                                }
                            }

                            Wall nextWall = kitchen.Walls[nextWallIndex];
                            double nextWallEmptyStart = currentWall.Width - door.DistanceX;
                            emptySpaces[nextWallIndex] = (nextWallEmptyStart, nextWall.Width);
                        }
                    }

                }

                // Save current wall space (if not already in dictionary from the previous wall)
                if (!emptySpaces.ContainsKey(i))
                {
                    emptySpaces[i] = (emptyStart, emptyEnd);
                }
            }


            // Write to debug.txt
            string debugFilePath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\debug.txt";
            using (StreamWriter writer = new StreamWriter(debugFilePath))
            {
                writer.WriteLine("Empty Spaces Analysis:");
                foreach (var kvp in emptySpaces)
                {
                    writer.WriteLine($"Wall {kvp.Key + 1}: from {kvp.Value.Item1} cm to {kvp.Value.Item2} cm");
                }
            }
            return emptySpaces;

        }
    }
}
