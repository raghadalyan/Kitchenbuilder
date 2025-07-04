using System;
using System.IO;
using System.Text.Json.Nodes;
using Kitchenbuilder.Models;

namespace Kitchenbuilder.Core
{
    public static class SinkCooktopMiddle
    {
        private static readonly string JsonPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads", "Kitchenbuilder", "Kitchenbuilder", "JSON");

        private static readonly string DebugPath = @"C:\\Users\\chouse\\Downloads\\Kitchenbuilder\\Output\\Sink-Cooktop\\SinkCooktopMiddle.txt";

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        public static Sink? CreateSinkInMiddle(int wall, int baseNum, int optionNum)
        {
            Log($"Creating sink on Wall {wall}, Base {baseNum}, Option {optionNum}...");

            string jsonFile = Path.Combine(JsonPath, $"Option{optionNum}SLD.json");
            if (!File.Exists(jsonFile)) { Log("❌ JSON file not found."); return null; }

            JsonObject root = JsonNode.Parse(File.ReadAllText(jsonFile))!.AsObject();

            double floorWidth = root["Floor"]?["Width"]?["Size"]?.GetValue<double>() ?? -1;
            double floorLength = root["Floor"]?["Length"]?["Size"]?.GetValue<double>() ?? -1;

            string wallKey = $"Wall{wall}";
            string baseKey = $"Base{baseNum}";

            var baseObj = root[wallKey]?["Bases"]?[baseKey]?.AsObject();
            if (baseObj == null) { Log("❌ Base not found in JSON."); return null; }

            double start = baseObj["Start"]?.GetValue<double>() ?? 0;
            double end = baseObj["End"]?.GetValue<double>() ?? 0;
            int middle = (int)((end - start) / 2);

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

            Log("✅ Sink created successfully.");
            return sink;
        }

        public static Cooktop? CreateCooktopInMiddle(int wall, int baseNum, int optionNum)
        {
            Log($"Creating cooktop on Wall {wall}, Base {baseNum}, Option {optionNum}...");

            string jsonFile = Path.Combine(JsonPath, $"Option{optionNum}SLD.json");
            if (!File.Exists(jsonFile)) { Log("❌ JSON file not found."); return null; }

            JsonObject root = JsonNode.Parse(File.ReadAllText(jsonFile))!.AsObject();

            double floorWidth = root["Floor"]?["Width"]?["Size"]?.GetValue<double>() ?? -1;
            double floorLength = root["Floor"]?["Length"]?["Size"]?.GetValue<double>() ?? -1;

            string wallKey = $"Wall{wall}";
            string baseKey = $"Base{baseNum}";

            var baseObj = root[wallKey]?["Bases"]?[baseKey]?.AsObject();
            if (baseObj == null) { Log("❌ Base not found in JSON."); return null; }

            double start = baseObj["Start"]?.GetValue<double>() ?? 0;
            double end = baseObj["End"]?.GetValue<double>() ?? 0;
            int middle = (int)((end - start) / 2);

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
                    cooktop.Angle_Sketch_Rotate_Cooktop = 270;
                    break;
                case 2:
                    cooktop.DistanceX_Cooktop_On_CT = (int)(start + middle);
                    cooktop.DistanceY_Cooktop_On_CT = 30;
                    cooktop.Angle_Sketch_Rotate_Cooktop = 180;
                    break;
                case 3:
                    cooktop.DistanceX_Cooktop_On_CT = (int)(floorLength - 30);
                    cooktop.DistanceY_Cooktop_On_CT = (int)(start + middle);
                    cooktop.Angle_Sketch_Rotate_Cooktop = 90;
                    break;
                case 4:
                    cooktop.DistanceX_Cooktop_On_CT = (int)(floorLength - start - middle);
                    cooktop.DistanceY_Cooktop_On_CT = (int)(floorWidth - 30);
                    cooktop.Angle_Sketch_Rotate_Cooktop = 360;
                    break;
            }

            Log("✅ Cooktop created successfully.");
            return cooktop;
        }
    }
}
