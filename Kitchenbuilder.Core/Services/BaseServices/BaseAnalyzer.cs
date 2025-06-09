using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

                // Update empty spaces based on doors
                UpdateEmptySpacesForDoors(kitchen, i, currentWall, emptySpaces);

                // Save current wall space (if not already in dictionary)
                if (!emptySpaces.ContainsKey(i))
                {
                    emptySpaces[i] = (emptyStart, emptyEnd);
                }
            }

            // Analyze internal empty spaces (doors + windows)
            var wallInternalEmptySpaces = AnalyzeInternalWallEmptySpaces(kitchen);

            // Write to debug.txt
            string debugFilePath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\debug.txt";
            using (StreamWriter writer = new StreamWriter(debugFilePath))
            {
                writer.WriteLine("Empty Spaces Analysis (based on doors at ends):");
                foreach (var kvp in emptySpaces)
                {
                    writer.WriteLine($"Wall {kvp.Key + 1}: from {kvp.Value.Item1} cm to {kvp.Value.Item2} cm");
                }

                writer.WriteLine();
                writer.WriteLine("Internal Wall Empty Spaces (doors + windows):");
                foreach (var kvp in wallInternalEmptySpaces)
                {
                    writer.WriteLine($"Wall {kvp.Key + 1}:");
                    foreach (var space in kvp.Value)
                    {
                        writer.WriteLine($"  From {space.Item1} cm to {space.Item2} cm");
                    }
                }
            }

            return emptySpaces;
        }

        private static void UpdateEmptySpacesForDoors(Kitchen kitchen, int currentIndex, Wall currentWall, Dictionary<int, (double, double)> emptySpaces)
        {
            // Doors at the end
            if (currentWall.HasDoors && currentWall.Doors != null)
            {
                foreach (var door in currentWall.Doors)
                {
                    double spaceAfterDoor = currentWall.Width - door.DistanceX - door.Width;
                    if (spaceAfterDoor < 60)
                    {
                        int nextWallIndex = (currentIndex + 1) % kitchen.Walls.Count;

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

            // Doors at the start
            if (currentWall.HasDoors && currentWall.Doors != null)
            {
                foreach (var door in currentWall.Doors)
                {
                    if (door.DistanceX < 60)
                    {
                        int prevWallIndex = (currentIndex - 1 + kitchen.Walls.Count) % kitchen.Walls.Count;

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
        }

        private static Dictionary<int, List<(double, double)>> AnalyzeInternalWallEmptySpaces(Kitchen kitchen)
        {
            var wallEmptySpaces = new Dictionary<int, List<(double, double)>>();

            for (int i = 0; i < kitchen.Walls.Count; i++)
            {
                Wall currentWall = kitchen.Walls[i];

                List<(double start, double end)> blockedIntervals = new List<(double, double)>();

                // Doors
                if (currentWall.HasDoors && currentWall.Doors != null)
                {
                    foreach (var door in currentWall.Doors)
                    {
                        blockedIntervals.Add((door.DistanceX, door.DistanceX + door.Width));
                    }
                }

                // Windows with distanceY < 90
                if (currentWall.HasWindows && currentWall.Windows != null)
                {
                    foreach (var window in currentWall.Windows)
                    {
                        if (window.DistanceY < 90)
                        {
                            blockedIntervals.Add((window.DistanceX, window.DistanceX + window.Width));
                        }
                    }
                }

                // Sort blocked intervals
                blockedIntervals.Sort((a, b) => a.start.CompareTo(b.start));

                // Find empty spaces
                List<(double, double)> emptySpaces = new List<(double, double)>();
                double prevEnd = 0;

                foreach (var interval in blockedIntervals)
                {
                    if (interval.start > prevEnd)
                    {
                        emptySpaces.Add((prevEnd, interval.start));
                    }
                    prevEnd = Math.Max(prevEnd, interval.end);
                }

                if (prevEnd < currentWall.Width)
                {
                    emptySpaces.Add((prevEnd, currentWall.Width));
                }

                wallEmptySpaces[i] = emptySpaces;
            }

            return wallEmptySpaces;
        }
    }
}
