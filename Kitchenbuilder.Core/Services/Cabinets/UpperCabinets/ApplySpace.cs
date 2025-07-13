using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Kitchenbuilder.Core.Models;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public static class ApplySpace
    {
        private static readonly string SavePath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\UpperCabinets.json";
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\upper\apply_space_debug.txt";

        public static string Apply(IModelDoc2 model, int optionNum, Space newSpace, UpperCabinetStation station)
        {
            try
            {
                // Validation
                if (newSpace.Width < 60 || newSpace.Height < 60)
                    return LogAndReturn("❌ Width and Height must be ≥ 60 cm");

                if (newSpace.DistanceX < 0 || newSpace.DistanceY < 0)
                    return LogAndReturn("❌ DistanceX and DistanceY cannot be negative");

                string wallKey = $"Wall{station.WallNumber}";
                Dictionary<string, WallCabinetWrapper> upperData;

                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    upperData = JsonSerializer.Deserialize<Dictionary<string, WallCabinetWrapper>>(json)
                                 ?? new();
                }
                else
                {
                    upperData = new();
                }

                if (!upperData.ContainsKey(wallKey))
                    upperData[wallKey] = new WallCabinetWrapper();

                upperData[wallKey].Spaces.Add(newSpace);

                File.WriteAllText(SavePath, JsonSerializer.Serialize(upperData, new JsonSerializerOptions { WriteIndented = true }));

                return LogAndReturn($"✅ Space saved to wall {wallKey}.");
            }
            catch (Exception ex)
            {
                return LogAndReturn($"❌ Exception in ApplySpace: {ex.Message}");
            }
        }

        private static string LogAndReturn(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}");
            return message;
        }
    }
}
