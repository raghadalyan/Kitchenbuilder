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

                double emptyStart = 0; // default start
                double emptyEnd = currentWall.Width; // default end

                // 🔺 Check for doors at the end of the current wall
                if (currentWall.HasDoors && currentWall.Doors != null)
                {
                    foreach (var door in currentWall.Doors)
                    {
                        double spaceAfterDoor = currentWall.Width - door.DistanceX - door.Width;
                        if (spaceAfterDoor < 60)
                        {
                            int nextWallIndex = (i + 1) % kitchen.Walls.Count;

                            Wall nextWall = kitchen.Walls[nextWallIndex];
                            double nextWallEmptyStart = currentWall.Width - door.DistanceX;

                            if (emptySpaces.ContainsKey(nextWallIndex))
                            {
                                var existing = emptySpaces[nextWallIndex];
                                emptySpaces[nextWallIndex] = (
                                    Math.Max(existing.Item1, nextWallEmptyStart),
                                    existing.Item2
                                );
                            }
                            else
                            {
                                emptySpaces[nextWallIndex] = (nextWallEmptyStart, nextWall.Width);
                            }
                        }
                    }
                }

                // 🔺 Check for doors at the start of the current wall that affect the previous wall
                if (currentWall.HasDoors && currentWall.Doors != null)
                {
                    foreach (var door in currentWall.Doors)
                    {
                        if (door.DistanceX < 60)
                        {
                            int prevWallIndex = (i - 1 + kitchen.Walls.Count) % kitchen.Walls.Count;

                            Wall prevWall = kitchen.Walls[prevWallIndex];
                            double prevWallEmptyEnd = prevWall.Width - door.DistanceX - door.Width;

                            if (emptySpaces.ContainsKey(prevWallIndex))
                            {
                                var existing = emptySpaces[prevWallIndex];
                                emptySpaces[prevWallIndex] = (
                                    existing.Item1,
                                    Math.Min(existing.Item2, prevWallEmptyEnd)
                                );
                            }
                            else
                            {
                                emptySpaces[prevWallIndex] = (0, prevWallEmptyEnd);
                            }
                        }
                    }
                }

                // Save current wall space (if not already in dictionary)
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
