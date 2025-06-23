using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core
{
    public static class ApplySmartDims
    {
        private static readonly string LogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\ApplySmartDims.txt";

        public static void ApplyDimensions(string jsonPath, ModelDoc2 model)
        {
            if (!File.Exists(jsonPath))
            {
                Log($"❌ JSON file not found: {jsonPath}");
                return;
            }

            if (model == null)
            {
                Log("❌ Model is null.");
                return;
            }

            string content = File.ReadAllText(jsonPath);
            JsonObject json = JsonNode.Parse(content).AsObject();

            for (int i = 1; i <= 4; i++)
            {
                string wallKey = $"Wall{i}";
                if (!json.ContainsKey(wallKey)) continue;

                JsonObject wall = json[wallKey]?.AsObject();
                if (wall == null || !wall.ContainsKey("Bases")) continue;

                JsonObject bases = wall["Bases"]?.AsObject();
                if (bases == null) continue;

                foreach (var basePair in bases)
                {
                    JsonObject baseObj = basePair.Value?.AsObject();
                    if (baseObj == null) continue;

                    bool isVisible = baseObj["Visible"]?.GetValue<bool>() ?? false;
                    if (!isVisible || !baseObj.ContainsKey("SmartDim")) continue;

                    JsonArray smartDims = baseObj["SmartDim"]?.AsArray();
                    if (smartDims == null) continue;

                    foreach (JsonNode dimNode in smartDims)
                    {
                        JsonObject dimObj = dimNode.AsObject();
                        string dimName = dimObj["Name"]?.GetValue<string>();
                        double size = dimObj["Size"]?.GetValue<double>() ?? 0;

                        if (!string.IsNullOrEmpty(dimName))
                        {
                            Log($"📐 Setting dimension: {dimName} → {size} cm");
                            Edit_Sketch_Dim.SetDimension(model, dimName, size);
                        }
                    }
                }
            }
        }

        private static void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(LogPath, $"[{timestamp}] {message}{System.Environment.NewLine}");
        }
    }
}
