using System;
using System.IO;
using Kitchenbuilder.Models;
using Kitchenbuilder.Core.Models;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public static class SinkCooktopOnIsland
    {
        private static readonly string DebugPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Output", "Sink-Cooktop", "SinkCooktopOnIsland_Debug.txt"
        );

        private static void Log(string msg)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {msg}{System.Environment.NewLine}");
        }
        public static Sink CreateSinkOnIsland(int wallNumber, Island island, IModelDoc2 model)
        {
            Log($"🔧 Creating Sink on island. Wall={wallNumber}, Dir={island.Direction}, DX={island.DistanceX}, DY={island.DistanceY}, Depth={island.Depth}");

            var sink = new Sink
            {
                WallNumber = wallNumber
            };

            if (wallNumber == 1 || wallNumber == 2)
            {
                if (island.Direction == 90)
                {
                    sink.DistanceX_Faucet_On_CT = (int)(island.DistanceX - island.Depth / 2 + 55);
                    sink.DistanceY_Faucet_On_CT = (int)island.DistanceY;
                    sink.Angle_Sketch_Rotate_Faucet = 90;

                    sink.Width_Sink_Cut = 40;
                    sink.Length_Sink_Cut = 60;
                    sink.DX_Sink_Cut = (int)(island.DistanceX - island.Depth / 2 + 10);
                    sink.DY_Sink_Cut = (int)(island.DistanceY - 30);
                }
                else
                {
                    sink.DistanceX_Faucet_On_CT = (int)island.DistanceX;
                    sink.DistanceY_Faucet_On_CT = (int)(island.DistanceY - island.Depth / 2 + 55);
                    sink.Angle_Sketch_Rotate_Faucet = 359.99;

                    sink.Width_Sink_Cut = 60;
                    sink.Length_Sink_Cut = 40;
                    sink.DX_Sink_Cut = (int)(island.DistanceX - 30);
                    sink.DY_Sink_Cut = (int)(island.DistanceY - island.Depth / 2 + 10);
                }
            }
            else
            {
                if (island.Direction == 90)
                {
                    sink.DistanceX_Faucet_On_CT = (int)(island.DistanceX + island.Depth / 2 - 55);
                    sink.DistanceY_Faucet_On_CT = (int)island.DistanceY;
                    sink.Angle_Sketch_Rotate_Faucet = 269.99;

                    sink.Width_Sink_Cut = 40;
                    sink.Length_Sink_Cut = 60;
                    sink.DX_Sink_Cut = (int)(island.DistanceX + island.Depth / 2 - 50);
                    sink.DY_Sink_Cut = (int)(island.DistanceY - 30);
                }
                else
                {
                    sink.DistanceX_Faucet_On_CT = (int)island.DistanceX;
                    sink.DistanceY_Faucet_On_CT = (int)(island.DistanceY + island.Depth / 2 - 55);
                    sink.Angle_Sketch_Rotate_Faucet = 179.99;

                    sink.Width_Sink_Cut = 60;
                    sink.Length_Sink_Cut = 40;
                    sink.DX_Sink_Cut = (int)(island.DistanceX - 30);
                    sink.DY_Sink_Cut = (int)(island.DistanceY + island.Depth / 2 - 50);
                }
            }

            Log($"✅ Sink Result: X={sink.DistanceX_Faucet_On_CT}, Y={sink.DistanceY_Faucet_On_CT}, Angle={sink.Angle_Sketch_Rotate_Faucet}");
            Log($"🪚 Cut: Width={sink.Width_Sink_Cut}, Length={sink.Length_Sink_Cut}, DX={sink.DX_Sink_Cut}, DY={sink.DY_Sink_Cut}");

            ApplySinkCooktopInSLD.ApplySinkAndCooktop(model, sink, null);
            return sink;
        }

        public static Cooktop CreateCooktopOnIsland(int wallNumber, Island island, IModelDoc2 model)
        {
            Log($"🔧 Creating Cooktop on island. Wall={wallNumber}, Dir={island.Direction}, DX={island.DistanceX}, DY={island.DistanceY}, Depth={island.Depth}");

            var cooktop = new Cooktop
            {
                WallNumber = wallNumber
            };

            if (wallNumber == 1 || wallNumber == 2)
            {
                if (island.Direction == 90)
                {
                    cooktop.DistanceX_Cooktop_On_CT = (int)(island.DistanceX - island.Depth / 2 + 30);
                    cooktop.DistanceY_Cooktop_On_CT = (int)island.DistanceY;
                    cooktop.Angle_Sketch_Rotate_Cooktop = 179.99;
                }
                else
                {
                    cooktop.DistanceX_Cooktop_On_CT = (int)island.DistanceX;
                    cooktop.DistanceY_Cooktop_On_CT = (int)(island.DistanceY - island.Depth / 2 + 30);
                    cooktop.Angle_Sketch_Rotate_Cooktop = 90;
                }
            }
            else
            {
                if (island.Direction == 90)
                {
                    cooktop.DistanceX_Cooktop_On_CT = (int)(island.DistanceX + island.Depth / 2 - 30);
                    cooktop.DistanceY_Cooktop_On_CT = (int)island.DistanceY;
                    cooktop.Angle_Sketch_Rotate_Cooktop = 179.99;
                }
                else
                {
                    cooktop.DistanceX_Cooktop_On_CT = (int)island.DistanceX;
                    cooktop.DistanceY_Cooktop_On_CT = (int)(island.DistanceY + island.Depth / 2 - 30);
                    cooktop.Angle_Sketch_Rotate_Cooktop = 90;
                }
            }

            Log($"✅ Cooktop Result: X={cooktop.DistanceX_Cooktop_On_CT}, Y={cooktop.DistanceY_Cooktop_On_CT}, Angle={cooktop.Angle_Sketch_Rotate_Cooktop}");
            ApplySinkCooktopInSLD.ApplySinkAndCooktop(model, null, cooktop);
            return cooktop;
        }
    }
}
