using Kitchenbuilder.Core.Models;
using SolidWorks.Interop.sldworks;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core.Services.BaseServices.SLD_IMPLEMENTAION
{
    public static class CreateFloor_Base
    {
        private static readonly string LogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\CreateFloorBase.txt";

        public static void Create(Kitchen kitchen, ModelDoc2 model, string jsonPath)
        {
            try
            {
                Log("************** CreateFloor_Base **************");
                Log($"📂 JSON Path: {jsonPath}");

                string json = File.ReadAllText(jsonPath);
                var node = JsonNode.Parse(json);

                if (node == null)
                {
                    Log("❌ Failed to parse JSON file.");
                    return;
                }

                var floorNode = node["Floor"];
                if (floorNode == null)
                {
                    Log("❌ Floor section not found in JSON.");
                    return;
                }

                string widthName = floorNode["Width"]?["Name"]?.ToString();
                double widthSize = floorNode["Width"]?["Size"]?.GetValue<double>() ?? -1;

                string lengthName = floorNode["Length"]?["Name"]?.ToString();
                double lengthSize = floorNode["Length"]?["Size"]?.GetValue<double>() ?? -1;

                if (string.IsNullOrEmpty(widthName) || string.IsNullOrEmpty(lengthName) || widthSize <= 0 || lengthSize <= 0)
                {
                    Log("❌ Invalid floor dimensions in JSON.");
                    return;
                }

                Log($"📏 Editing Width: {widthName} = {widthSize} mm");
                Log($"📏 Editing Length: {lengthName} = {lengthSize} mm");

                Edit_Sketch_Dim.SetDimension(model, widthName, widthSize);
                Edit_Sketch_Dim.SetDimension(model, lengthName, lengthSize);

                Log("✅ Floor dimensions applied successfully.");
            }
            catch (Exception ex)
            {
                Log($"❌ Error in CreateFloor_Base.Create: {ex.Message}");
            }
        }

        private static void Log(string message)
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
    }
}
