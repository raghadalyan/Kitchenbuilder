using System;
using System.IO;
using System.Text.Json.Nodes;
using Kitchenbuilder.Models;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public static class SinkCooktopMiddle
    {
        private static readonly string JsonPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Kitchenbuilder", "JSON"
        );

        private static readonly string DebugPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Output", "Sink-Cooktop", "SinkCooktopMiddle.txt"
        );

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}");
        }

        public static Sink? CreateSinkInMiddle(int wall, int baseNum, int optionNum, IModelDoc2 model)
        {
            Log($"🛠 Creating sink on Wall {wall}, Base {baseNum}, Option {optionNum}...");

            string jsonFile = Path.Combine(JsonPath, $"Option{optionNum}SLD.json");
            if (!File.Exists(jsonFile)) { Log("❌ JSON file not found."); return null; }

            JsonObject root = JsonNode.Parse(File.ReadAllText(jsonFile))!.AsObject();

            double floorWidth = root["Floor"]?["Width"]?["Size"]?.GetValue<double>() ?? -1;
            double floorLength = root["Floor"]?["Length"]?["Size"]?.GetValue<double>() ?? -1;
            Log($"📐 Floor: Width={floorWidth}, Length={floorLength}");

            string wallKey = $"Wall{wall}";
            string baseKey = $"Base{baseNum}";

            var baseObj = root[wallKey]?["Bases"]?[baseKey]?.AsObject();
            if (baseObj == null) { Log("❌ Base not found in JSON."); return null; }

            double start = baseObj["Countertop"]?["Start"]?.GetValue<double>() ?? 0;
            double end = baseObj["Countertop"]?["End"]?.GetValue<double>() ?? 0;
            int middle = (int)((end - start) / 2);

            Log($"📏 Sink Countertop: Start={start}, End={end}, Middle={middle}");

            Sink sink = new Sink
            {
                WallNumber = wall,
                BaseNumber = baseNum,
                DistanceFromLeft = middle
            };

            switch (wall)
            {
                case 1:
                    sink.DistanceX_Faucet_On_CT = 5;
                    sink.DistanceY_Faucet_On_CT = (int)(floorWidth - start - middle);
                    sink.Angle_Sketch_Rotate_Faucet = 270;
                    break;
                case 2:
                    sink.DistanceX_Faucet_On_CT = (int)(start + middle);
                    sink.DistanceY_Faucet_On_CT = 5;
                    sink.Angle_Sketch_Rotate_Faucet = 180;
                    break;
                case 3:
                    sink.DistanceX_Faucet_On_CT = (int)(floorLength - 5);
                    sink.DistanceY_Faucet_On_CT = (int)(start + middle);
                    sink.Angle_Sketch_Rotate_Faucet = 90;
                    break;
                case 4:
                    sink.DistanceX_Faucet_On_CT = (int)(floorLength - start - middle);
                    sink.DistanceY_Faucet_On_CT = (int)(floorWidth - 5);
                    sink.Angle_Sketch_Rotate_Faucet = 360;
                    break;
            }

            // ✅ Compute sink cut values based on wall + faucet placement
            sink.ComputeSinkCutDimensions((int)floorWidth, (int)floorLength, wall);

            Log($"✅ Sink created at (X={sink.DistanceX_Faucet_On_CT}, Y={sink.DistanceY_Faucet_On_CT}), Angle={sink.Angle_Sketch_Rotate_Faucet}");
            Log($"✏️ Cut Dimensions: Width={sink.Width_Sink_Cut}, Length={sink.Length_Sink_Cut}, DX={sink.DX_Sink_Cut}, DY={sink.DY_Sink_Cut}");

            ApplySinkCooktopInSLD.ApplySinkAndCooktop(model, sink, null); // ✅ Apply sink + dimensions
            return sink;
        }

        public static Cooktop? CreateCooktopInMiddle(int wall, int baseNum, int optionNum, IModelDoc2 model)
        {
            Log($"🛠 Creating cooktop on Wall {wall}, Base {baseNum}, Option {optionNum}...");

            string jsonFile = Path.Combine(JsonPath, $"Option{optionNum}SLD.json");
            if (!File.Exists(jsonFile)) { Log("❌ JSON file not found."); return null; }

            JsonObject root = JsonNode.Parse(File.ReadAllText(jsonFile))!.AsObject();

            double floorWidth = root["Floor"]?["Width"]?["Size"]?.GetValue<double>() ?? -1;
            double floorLength = root["Floor"]?["Length"]?["Size"]?.GetValue<double>() ?? -1;
            Log($"📐 Floor: Width={floorWidth}, Length={floorLength}");

            string wallKey = $"Wall{wall}";
            string baseKey = $"Base{baseNum}";

            var baseObj = root[wallKey]?["Bases"]?[baseKey]?.AsObject();
            if (baseObj == null) { Log("❌ Base not found in JSON."); return null; }

            double start = baseObj["Start"]?.GetValue<double>() ?? 0;
            double end = baseObj["End"]?.GetValue<double>() ?? 0;
            int middle = (int)((end - start) / 2);

            Log($"📏 Cooktop Base: Start={start}, End={end}, Middle={middle}");

            Cooktop cooktop = new Cooktop
            {
                WallNumber = wall,
                BaseNumber = baseNum,
                DistanceFromLeft = middle
            };

            switch (wall)
            {
                case 1:
                    cooktop.DistanceX_Cooktop_On_CT = 30;
                    cooktop.DistanceY_Cooktop_On_CT = (int)(floorWidth - start - middle);
                    cooktop.Angle_Sketch_Rotate_Cooktop = 360;
                    break;
                case 2:
                    cooktop.DistanceX_Cooktop_On_CT = (int)(start + middle);
                    cooktop.DistanceY_Cooktop_On_CT = 30;
                    cooktop.Angle_Sketch_Rotate_Cooktop = 270;
                    break;
                case 3:
                    cooktop.DistanceX_Cooktop_On_CT = (int)(floorLength - 30);
                    cooktop.DistanceY_Cooktop_On_CT = (int)(start + middle);
                    cooktop.Angle_Sketch_Rotate_Cooktop = 360;
                    break;
                case 4:
                    cooktop.DistanceX_Cooktop_On_CT = (int)(floorLength - start - middle);
                    cooktop.DistanceY_Cooktop_On_CT = (int)(floorWidth - 30);
                    cooktop.Angle_Sketch_Rotate_Cooktop = 270;
                    break;
            }

            Log($"✅ Cooktop created at (X={cooktop.DistanceX_Cooktop_On_CT}, Y={cooktop.DistanceY_Cooktop_On_CT}), Angle={cooktop.Angle_Sketch_Rotate_Cooktop}");

            ApplySinkCooktopInSLD.ApplySinkAndCooktop(model, null, cooktop); // ✅ Apply cooktop
            return cooktop;
        }
    }
}
