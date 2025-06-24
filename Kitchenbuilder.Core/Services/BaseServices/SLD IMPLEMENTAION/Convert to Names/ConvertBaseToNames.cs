using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core
{
    public static class ConvertBaseToNames
    {
        private static readonly string JsonFolder = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\";
        private static readonly string LogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\ConvertBaseToNames.txt";

        public static void Convert(Kitchen kitchen, Dictionary<int, List<(double start, double end)>> simpleEmptySpaces)
        {
            var files = Directory.GetFiles(JsonFolder, "Option*.json");

            Log("************** ConvertBaseToNames **************");

            foreach (var file in files)
            {
                try
                {
                    string fileName = Path.GetFileName(file);
                    Log($"🔍 Reading {fileName}");

                    string outputFileName = Path.GetFileNameWithoutExtension(file) + "SLD.json";
                    string outputPath = Path.Combine(JsonFolder, outputFileName);

                    var outputJson = new
                    {
                        Floor = new
                        {
                            Width = new
                            {
                                Name = "width@master_wall1",
                                Size = kitchen.Floor.Width
                            },
                            Length = new
                            {
                                Name = "length@Floor",
                                Size = kitchen.Floor.Length
                            }
                        }
                    };

                    File.WriteAllText(outputPath, JsonSerializer.Serialize(outputJson, new JsonSerializerOptions { WriteIndented = true }));
                    Log($"✅ Saved floor data to {outputFileName}");
                    //HandleUsableWalls 
                    HandleUsableWalls.Process(file, outputPath);

                    // ➕ Call Identify_Hidden_Walls here
                    Identify_Hidden_Walls.Process(file, outputPath, simpleEmptySpaces);
                }
                catch (Exception ex)
                {
                    Log($"❌ Error processing {file}: {ex.Message}");
                }
            }
        }

        private static void Log(string message)
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
    }
}