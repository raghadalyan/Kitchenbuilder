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

                // Update empty spaces based on doors at ends
                UpdateEmptySpacesForDoors(kitchen, i, currentWall, emptySpaces);

                // Save current wall space (if not already in dictionary)
                if (!emptySpaces.ContainsKey(i))
                {
                    emptySpaces[i] = (emptyStart, emptyEnd);
                }
            }

            // Analyze internal empty spaces (doors + windows)
            var wallInternalEmptySpaces = AnalyzeInternalWallEmptySpaces(kitchen);

            // Merge both dictionaries
            var mergedEmptySpaces = MergeEmptySpaces(emptySpaces, wallInternalEmptySpaces, kitchen);

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

                writer.WriteLine();
                writer.WriteLine("Merged Empty Spaces (final result):");
                foreach (var kvp in mergedEmptySpaces)
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
                        int nextWallIndex = currentIndex + 1;

                        if (nextWallIndex >= kitchen.Walls.Count)
                        {
                            // Only wrap around if there are exactly 4 walls
                            if (kitchen.Walls.Count == 4)
                            {
                                nextWallIndex = 0;
                            }
                            else
                            {
                                continue; // skip, no wrap-around
                            }
                        }

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
                        int prevWallIndex = currentIndex - 1;

                        if (prevWallIndex < 0)
                        {
                            // Only wrap around if there are exactly 4 walls
                            if (kitchen.Walls.Count == 4)
                            {
                                prevWallIndex = kitchen.Walls.Count - 1;
                            }
                            else
                            {
                                continue;
                            }
                        }

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

        private static Dictionary<int, List<(double, double)>> MergeEmptySpaces(
            Dictionary<int, (double, double)> endDoorSpaces,
            Dictionary<int, List<(double, double)>> internalSpaces,
            Kitchen kitchen)
        {
            var mergedSpaces = new Dictionary<int, List<(double, double)>>();

            for (int i = 0; i < kitchen.Walls.Count; i++)
            {
                var mergedList = new List<(double, double)>();

                // Get global door-based space
                var (doorStart, doorEnd) = endDoorSpaces.ContainsKey(i)
                    ? endDoorSpaces[i]
                    : (0, kitchen.Walls[i].Width);

                // Get internal wall spaces
                if (internalSpaces.ContainsKey(i))
                {
                    foreach (var space in internalSpaces[i])
                    {
                        double mergedStart = Math.Max(space.Item1, doorStart);
                        double mergedEnd = Math.Min(space.Item2, doorEnd);

                        if (mergedStart < mergedEnd)
                        {
                            mergedList.Add((mergedStart, mergedEnd));
                        }
                    }
                }
                else
                {
                    // No internal blocks — use the door-based space directly
                    mergedList.Add((doorStart, doorEnd));
                }

                mergedSpaces[i] = mergedList;
            }

            return mergedSpaces;
        }
    }
}
