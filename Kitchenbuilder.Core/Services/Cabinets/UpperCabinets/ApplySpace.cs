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
                // Simple dimension validation
                if (newSpace.Width < 60 || newSpace.Height < 60)
                    return LogAndReturn("❌ Width and Height must be ≥ 60 cm");

                if (newSpace.DistanceX < 0 || newSpace.DistanceY < 0)
                    return LogAndReturn("❌ DistanceX and DistanceY cannot be negative");

                // ✅ Check legality using validator
                string validationMessage = SpacePositionValidator.CheckDownPosition(optionNum, station.WallNumber, newSpace);
                if (!validationMessage.StartsWith("✅"))
                    return LogAndReturn(validationMessage);

                string wallKey = $"Wall{station.WallNumber}";
                Dictionary<string, WallCabinetWrapper> upperData;

                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        upperData = new(); // Empty file – treat as new
                    }
                    else
                    {
                        upperData = JsonSerializer.Deserialize<Dictionary<string, WallCabinetWrapper>>(json)
                                     ?? new();
                    }
                }
                else
                {
                    upperData = new();
                }

                if (!upperData.ContainsKey(wallKey))
                    upperData[wallKey] = new WallCabinetWrapper();

                upperData[wallKey].Spaces.Add(newSpace);

                File.WriteAllText(SavePath, JsonSerializer.Serialize(upperData, new JsonSerializerOptions { WriteIndented = true }));
                double floorLength = GetFloorLength(optionNum);
                double floorWidth = GetFloorWidth(optionNum);
                ApplySpaceDim.Apply(model, newSpace, station.WallNumber, floorLength, floorWidth);

                return LogAndReturn($"✅ Space saved to wall {wallKey}.");
            }
            catch (Exception ex)
            {
                return LogAndReturn($"❌ Exception in ApplySpace: {ex.Message}");
            }

        }
        private static double GetFloorWidth(int optionNum)
        {
            string jsonPath = $@"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\Option{optionNum}SLD.json";

            if (!File.Exists(jsonPath))
                return 0;

            string json = File.ReadAllText(jsonPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Floor", out var floorElement) &&
                floorElement.TryGetProperty("Width", out var widthElement) &&
                widthElement.TryGetProperty("Size", out var sizeElement))
            {
                return sizeElement.GetDouble();
            }

            return 0;
        }

        private static double GetFloorLength(int optionNum)
        {
            string jsonPath = $@"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\Option{optionNum}SLD.json";

            if (!File.Exists(jsonPath))
                return 0;

            string json = File.ReadAllText(jsonPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Floor", out var floorElement) &&
                floorElement.TryGetProperty("Length", out var lengthElement) &&
                lengthElement.TryGetProperty("Size", out var sizeElement))
            {
                return sizeElement.GetDouble();
            }

            return 0;
        }

        private static string LogAndReturn(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}");
            return message;
        }
    }
}
