﻿using Kitchenbuilder.Core.Models;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class ImplementInSld
    {
        private static readonly string JsonFolder = Path.Combine(
          KitchenConfig.Get().BasePath,
          "Kitchenbuilder", "Kitchenbuilder", "JSON"
      );

        private static readonly string LogPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Output", "ImplementInSld.txt"
        );

        public static void ApplyBaseDimensions(Kitchen kitchen, Dictionary<int, List<(double start, double end)>> simpleEmptySpaces)
        {
            // Start new log
            File.WriteAllText(LogPath, $"🔧 Starting implementation: {DateTime.Now}\n");

            // Convert the base measurements
            ConvertBaseToNames.Convert(kitchen, simpleEmptySpaces);

            var optionFiles = Directory.GetFiles(JsonFolder, "Option*SLD.json");

            foreach (var file in optionFiles)
            {
                try
                {
                    string fileName = Path.GetFileName(file);
                    File.AppendAllText(LogPath, $"\n🔁 Processing {fileName}\n");

                    // Pass the global kitchen and the specific JSON path to reader
                    JsonReader.ProcessJson(kitchen, file);

                    File.AppendAllText(LogPath, $"✅ Finished processing {fileName}\n");
                }
                catch (Exception ex)
                {
                    File.AppendAllText(LogPath, $"❌ Error reading {file}: {ex.Message}\n");
                }
            }

            File.AppendAllText(LogPath, $"\n✅ Implementation completed at {DateTime.Now}\n");
        }
    }
}
