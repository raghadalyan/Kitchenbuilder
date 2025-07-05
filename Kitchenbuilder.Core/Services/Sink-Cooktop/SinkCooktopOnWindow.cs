using System;
using System.IO;
using Kitchenbuilder.Models;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public static class SinkCooktopOnWindow
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Sink-Cooktop\SinkCooktopOnWindow.txt";

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}");
        }

        public static object? CreateSinkOrCooktopOnWindow(
            string name,
            int wall,
            int baseNum,
            double windowDistanceX,
            double windowDistanceY,
            double windowWidth,
            double windowHeight,
            double floorWidth,
            double floorLength,
            double startOfCountertop,
            double endOfCountertop,
            IModelDoc2 model) // ✅ Added model
        {
            double center = (windowDistanceX - startOfCountertop) + (windowWidth / 2);
            Log($"🧮 Center = (windowDistanceX - startOfCountertop) + (windowWidth / 2) = {center}");

            Log($"📌 Creating object: name={name}, wall={wall}, base={baseNum}");

            if (name == "sink")
            {
                Sink? sink = wall switch
                {
                    1 => new Sink
                    {
                        WallNumber = wall,
                        BaseNumber = baseNum,
                        DistanceFromLeft = (int)center,
                        DistanceX_Faucet_On_CT = 5,
                        DistanceY_Faucet_On_CT = (int)(floorWidth - startOfCountertop - center),
                        Angle_Sketch_Rotate_Faucet = 270
                    },
                    2 => new Sink
                    {
                        WallNumber = wall,
                        BaseNumber = baseNum,
                        DistanceFromLeft = (int)center,
                        DistanceX_Faucet_On_CT = (int)(startOfCountertop + center),
                        DistanceY_Faucet_On_CT = 5,
                        Angle_Sketch_Rotate_Faucet = 180
                    },
                    3 => new Sink
                    {
                        WallNumber = wall,
                        BaseNumber = baseNum,
                        DistanceFromLeft = (int)center,
                        DistanceX_Faucet_On_CT = (int)(floorLength - 5),
                        DistanceY_Faucet_On_CT = (int)(startOfCountertop + center),
                        Angle_Sketch_Rotate_Faucet = 90
                    },
                    4 => new Sink
                    {
                        WallNumber = wall,
                        BaseNumber = baseNum,
                        DistanceFromLeft = (int)center,
                        DistanceX_Faucet_On_CT = (int)(floorLength - startOfCountertop - center),
                        DistanceY_Faucet_On_CT = (int)(floorWidth - 5),
                        Angle_Sketch_Rotate_Faucet = 360
                    },
                    _ => null
                };

                if (sink != null)
                {
                    Log($"✅ Sink created: X={sink.DistanceX_Faucet_On_CT}, Y={sink.DistanceY_Faucet_On_CT}, Angle={sink.Angle_Sketch_Rotate_Faucet}");
                    ApplySinkCooktopInSLD.ApplySinkAndCooktop(model, sink, null); // ✅ Apply sink only
                }
                else
                {
                    Log("❌ Invalid wall number for sink.");
                }

                return sink;
            }

            if (name == "cooktop")
            {
                Cooktop? cooktop = wall switch
                {
                    1 => new Cooktop
                    {
                        WallNumber = wall,
                        BaseNumber = baseNum,
                        DistanceFromLeft = (int)center,
                        DistanceX_Cooktop_On_CT = 30,
                        DistanceY_Cooktop_On_CT = (int)(floorWidth - startOfCountertop - center),
                        Angle_Sketch_Rotate_Cooktop = 360
                    },
                    2 => new Cooktop
                    {
                        WallNumber = wall,
                        BaseNumber = baseNum,
                        DistanceFromLeft = (int)center,
                        DistanceX_Cooktop_On_CT = (int)(startOfCountertop + center),
                        DistanceY_Cooktop_On_CT = 30,
                        Angle_Sketch_Rotate_Cooktop = 270
                    },
                    3 => new Cooktop
                    {
                        WallNumber = wall,
                        BaseNumber = baseNum,
                        DistanceFromLeft = (int)center,
                        DistanceX_Cooktop_On_CT = (int)(floorLength - 30),
                        DistanceY_Cooktop_On_CT = (int)(startOfCountertop + center),
                        Angle_Sketch_Rotate_Cooktop = 360
                    },
                    4 => new Cooktop
                    {
                        WallNumber = wall,
                        BaseNumber = baseNum,
                        DistanceFromLeft = (int)center,
                        DistanceX_Cooktop_On_CT = (int)(floorLength - startOfCountertop - center),
                        DistanceY_Cooktop_On_CT = (int)(floorWidth - 30),
                        Angle_Sketch_Rotate_Cooktop = 270
                    },
                    _ => null
                };

                if (cooktop != null)
                {
                    Log($"✅ Cooktop created: X={cooktop.DistanceX_Cooktop_On_CT}, Y={cooktop.DistanceY_Cooktop_On_CT}, Angle={cooktop.Angle_Sketch_Rotate_Cooktop}");
                    ApplySinkCooktopInSLD.ApplySinkAndCooktop(model, null, cooktop); // ✅ Apply cooktop only
                }
                else
                {
                    Log("❌ Invalid wall number for cooktop.");
                }

                return cooktop;
            }

            Log("❌ Unknown object name (should be 'sink' or 'cooktop').");
            return null;
        }
    }
}
