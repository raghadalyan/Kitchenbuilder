using System;
using System.IO;
using Kitchenbuilder.Models;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public static class SinkCooktopSameCountertop
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Sink-Cooktop\SinkCooktopSameCountertop.txt";

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}");
        }

        public static (Sink?, Cooktop?) Create(
            int wall,
            int baseNum,
            double startOfCountertop,
            double endOfCountertop,
            double floorWidth,
            double floorLength,
            IModelDoc2 model)
        {
            double width = endOfCountertop - startOfCountertop;
            Log($"📏 Countertop width = {width}");

            if (width < 190)
            {
                Log("❌ Countertop width is less than 190 cm. Cannot place both sink and cooktop.");
                return (null, null);
            }

            double usable = width - 10;
            double third = usable / 3;
            double middleOfThird = third / 2;

            bool sinkOnLeft = new Random().Next(2) == 0;

            double sinkDistance = sinkOnLeft
                ? 5 + middleOfThird
                : 5 + (2 * third) + middleOfThird;

            double cooktopDistance = sinkOnLeft
                ? 5 + (2 * third) + middleOfThird
                : 5 + middleOfThird;

            Log($"🔀 Random order: Sink on {(sinkOnLeft ? "left" : "right")}, Cooktop on {(sinkOnLeft ? "right" : "left")}");

            Sink sink = new Sink
            {
                WallNumber = wall,
                BaseNumber = baseNum,
                DistanceFromLeft = (int)sinkDistance
            };

            Cooktop cooktop = new Cooktop
            {
                WallNumber = wall,
                BaseNumber = baseNum,
                DistanceFromLeft = (int)cooktopDistance
            };

            switch (wall)
            {
                case 1:
                    sink.DistanceX_Faucet_On_CT = 5;
                    sink.DistanceY_Faucet_On_CT = (int)(floorWidth - startOfCountertop - sinkDistance);
                    sink.Angle_Sketch_Rotate_Faucet = 270;

                    cooktop.DistanceX_Cooktop_On_CT = 30;
                    cooktop.DistanceY_Cooktop_On_CT = (int)(floorWidth - startOfCountertop - cooktopDistance);
                    cooktop.Angle_Sketch_Rotate_Cooktop = 360;
                    break;

                case 2:
                    sink.DistanceX_Faucet_On_CT = (int)(startOfCountertop + sinkDistance);
                    sink.DistanceY_Faucet_On_CT = 5;
                    sink.Angle_Sketch_Rotate_Faucet = 180;

                    cooktop.DistanceX_Cooktop_On_CT = (int)(startOfCountertop + cooktopDistance);
                    cooktop.DistanceY_Cooktop_On_CT = 30;
                    cooktop.Angle_Sketch_Rotate_Cooktop = 270;
                    break;

                case 3:
                    sink.DistanceX_Faucet_On_CT = (int)(floorLength - 5);
                    sink.DistanceY_Faucet_On_CT = (int)(startOfCountertop + sinkDistance);
                    sink.Angle_Sketch_Rotate_Faucet = 90;

                    cooktop.DistanceX_Cooktop_On_CT = (int)(floorLength - 30);
                    cooktop.DistanceY_Cooktop_On_CT = (int)(startOfCountertop + cooktopDistance);
                    cooktop.Angle_Sketch_Rotate_Cooktop = 360;
                    break;

                case 4:
                    sink.DistanceX_Faucet_On_CT = (int)(floorLength - startOfCountertop - sinkDistance);
                    sink.DistanceY_Faucet_On_CT = (int)(floorWidth - 5);
                    sink.Angle_Sketch_Rotate_Faucet = 360;

                    cooktop.DistanceX_Cooktop_On_CT = (int)(floorLength - startOfCountertop - cooktopDistance);
                    cooktop.DistanceY_Cooktop_On_CT = (int)(floorWidth - 30);
                    cooktop.Angle_Sketch_Rotate_Cooktop = 270;
                    break;

                default:
                    Log("❌ Invalid wall number.");
                    return (null, null);
            }

            Log($"✅ Sink placed at DistanceFromLeft={sink.DistanceFromLeft}, X={sink.DistanceX_Faucet_On_CT}, Y={sink.DistanceY_Faucet_On_CT}");
            Log($"✅ Cooktop placed at DistanceFromLeft={cooktop.DistanceFromLeft}, X={cooktop.DistanceX_Cooktop_On_CT}, Y={cooktop.DistanceY_Cooktop_On_CT}");
            sink.ComputeSinkCutDimensions((int)floorWidth, (int)floorLength, wall);

            ApplySinkCooktopInSLD.ApplySinkAndCooktop(model, sink, cooktop);

            return (sink, cooktop);
        }
    }
}
